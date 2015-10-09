<?php
	// Excel to JSON-Base64 command-line conversion tool.
	// Public Domain.  This code only.

	if (!isset($_SERVER["argc"]) || !$_SERVER["argc"])
	{
		echo "This file is intended to be run from the command-line.";

		exit();
	}

	// Temporary root.
	$rootpath = str_replace("\\", "/", dirname(__FILE__));

	require_once $rootpath . "/support/cli.php";
	require_once $rootpath . "/support/utf8.php";
	require_once $rootpath . "/support/jb64.php";

	$filename = $rootpath . "/support/PHPExcel/Classes/PHPExcel.php";
	if (!file_exists($filename))
	{
		echo "Using the Excel to JSON-Base64 conversion tool requires '" . $filename . "' to exist.\n\n";
		echo "PHPExcel is missing.\n\n";
		echo "Please install PHPExcel:\n\n";
		echo "https://github.com/PHPOffice/PHPExcel/releases\n";

		exit();
	}

	require_once $filename;

	// Process the command-line options.
	$options = array(
		"shortmap" => array(
			"c" => "columns",
			"k" => "keep",
			"s" => "skipfirst",
			"t" => "types",
			"?" => "help"
		),
		"rules" => array(
			"columns" => array("arg" => true),
			"keep" => array("arg" => false),
			"limit" => array("arg" => true),
			"sheet" => array("arg" => true),
			"skipfirst" => array("arg" => false),
			"types" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = ParseCommandLine($options);

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]))
	{
		echo "Excel to JSON-Base64 command-line conversion tool\n";
		echo "Purpose:  Converts Excel (.xlsx, .xls) files to JSON-Base64.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] SrcFilename [DestFilename]\n";
		echo "Options:\n";
		echo "\t-c=columns     Comma-separated list of column names.  Forced header row.\n";
		echo "\t-t=types       Comma-separated list of column data types.\n";
		echo "\t-limit=num     The number of records to read to determine column types.\n";
		echo "\t-sheet=num     The Excel sheet number to export.  Default is 0.\n";
		echo "\t-k             Keep outer whitespace on each column of data.\n";
		echo "\t-s             Skip first row of input.  Only valid when -c is specified.\n";
		echo "\t-?             This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " test.xlsx test.jb64\n";
		echo "\tphp " . $args["file"] . " -c=id,name -t=integer,string -sheet=0 test.xlsx\n";

		exit();
	}

	function DisplayError($msg, $result = false, $exit = true)
	{
		ob_start();

		echo $msg . "\n";

		if ($result !== false)
		{
			if (isset($result["error"]))  echo $result["error"] . " (" . $result["errorcode"] . ")\n";
			if (isset($result["info"]))  var_dump($result["info"]);
		}

		fwrite(STDERR, ob_get_contents());
		ob_end_clean();

		if ($exit)  exit();
	}

	// Input file.
	$stdin = false;
	try
	{
		$excel = PHPExcel_IOFactory::load($args["params"][0]);
	}
	catch (Exception $e)
	{
		DisplayError("Error loading file '" . $args["params"][0] . "'.  " . $e->getMessage());
	}

	// Select the sheet.
	try
	{
		$sheet = $excel->getSheet(isset($args["opts"]["sheet"]) ? (int)$args["opts"]["sheet"] : 0);
		$maxrow = $sheet->getHighestRow();
		$maxcol = $sheet->getHighestColumn();
	}
	catch (Exception $e)
	{
		DisplayError("Error retrieving worksheet " . (int)$args["opts"]["sheet"] . ".  " . $e->getMessage());
	}

	// Column names.
	if (isset($args["opts"]["columns"]))
	{
		$columns2 = explode(",", $args["opts"]["columns"]);

		$skipfirst = (isset($args["opts"]["skipfirst"]) && $args["opts"]["skipfirst"]);
	}
	else
	{
		$columns2 = $sheet->rangeToArray("A1:" . $maxcol . "1", null, true, false);
		$columns2 = $columns2[0];

		$skipfirst = true;
	}

	$columns = array();
	foreach ($columns2 as $col)
	{
		$col = trim(UTF8::MakeValid($col));
		if ($col !== "")  $columns[] = $col;
	}

	// Column types.
	if (isset($args["opts"]["types"]))
	{
		$types2 = explode(",", $args["opts"]["types"]);
	}
	else
	{
		$types2 = array();
	}

	$types = array();
	foreach ($types2 as $type)
	{
		$type = trim(UTF8::MakeValid($type));
		if ($type !== "")  $types[] = $type;
	}

	$autotype = false;
	while (count($types) < count($columns))
	{
		if ($stdin || (isset($args["opts"]["defaulttypes"]) && $args["opts"]["defaulttypes"]))  $types[] = "string";
		else
		{
			if ($autotype === false)  $autotype = count($types);

			$types[] = "boolean";
		}
	}

	$types = array_slice($types, 0, count($columns));

	// Handle automatic type calculations by processing the data.
	if ($autotype !== false)
	{
		for ($y = ($skipfirst ? 2 : 1); $y <= $maxrow; $y++)
		{
			$line = $sheet->rangeToArray("A" . $y . ":" . $maxcol . $y, null, true, false);
			$line = $line[0];

			while (count($line) < count($columns))  $line[] = "";

			$line = array_slice($line, 0, count($columns));

			$found = false;
			for ($x = 0; $x < count($types); $x++)
			{
				$col = UTF8::MakeValid((string)$line[$x]);

				if (!isset($args["opts"]["keep"]) || !$args["opts"]["keep"])  $col = trim($col);

				$line[$x] = $col;

				if ($col !== "")  $found = true;
			}

			if (!$found)  continue;

			for ($x = $autotype; $x < count($types); $x++)
			{
				$col = $line[$x];

				$type = $types[$x];

				$isdate = PHPExcel_Shared_Date::isDateTime($sheet->getCell(PHPExcel_Cell::stringFromColumnIndex($x) . $y));

				if ($type === "boolean")
				{
					if ($isdate)  $type = "time";
					else if (!is_numeric($col))  $type = "string";
					else if (!is_int($col + 0))  $type = "number";
					else if ((int)$col !== 0 && (int)$col !== 1)  $type = "integer";
				}

				if ($type === "integer")
				{
					if ($isdate)  $type = "time";
					else if (!is_numeric($col))  $type = "string";
					else if (!is_int($col + 0))  $type = "number";
				}

				if ($type === "number")
				{
					if ($isdate)  $type = "time";
					else if (!is_numeric($col))  $type = "string";
				}

				if ($type === "time")
				{
					if (!$isdate || !is_numeric($col))  $type = "string";

					if ($col >= 1.0)  $type = "date";
				}

				if ($type === "date")
				{
					if (!$isdate || !is_numeric($col))  $type = "string";
				}

				$types[$x] = $type;
			}
		}
	}

	// Output file/pipe.
	if (count($args["params"]) > 1)
	{
		$fp2 = @fopen($args["params"][1], "wb");
		if ($fp2 === false)  DisplayError("Unable to open '" . $args["params"][1] . "' for writing.");
	}
	else
	{
		$fp2 = STDOUT;
	}

	// Start JSON-Base64 output.
	$jb64 = new JB64Encode();

	$headers = array();
	foreach ($columns as $num => $col)
	{
		$headers[] = array($col, $types[$num]);
	}

	$result = $jb64->EncodeHeaders($headers);
	if (!$result["success"])  DisplayError("Unable to generate JSON-Base64 headers due to bad input.", $result);

	fwrite($fp2, $result["line"]);

	// Transform the rest of the Excel data.
	for ($y = ($skipfirst ? 2 : 1); $y <= $maxrow; $y++)
	{
		$line = $sheet->rangeToArray("A" . $y . ":" . $maxcol . $y, null, true, false);
		$line = $line[0];

		while (count($line) < count($columns))  $line[] = "";

		$line = array_slice($line, 0, count($columns));

		$found = false;
		for ($x = 0; $x < count($types); $x++)
		{
			$col = UTF8::MakeValid((string)$line[$x]);

			if (!isset($args["opts"]["keep"]) || !$args["opts"]["keep"])  $col = trim($col);

			$line[$x] = $col;

			if ($col !== "")  $found = true;
		}

		if (!$found)  continue;

		for ($x = 0; $x < count($types); $x++)
		{
			if ($types[$x] === "date" || $types[$x] === "time")
			{
				$col = $line[$x];

				if ($types[$x] === "date")  $col = ($col !== "" ? gmdate("Y-m-d H:i:s", PHPExcel_Shared_Date::ExcelToPHP((double)$col)) : "0000-00-00 00:00:00");
				else if ($types[$x] === "time")  $col = ($col !== "" ? gmdate("H:i:s", PHPExcel_Shared_Date::ExcelToPHP((double)$col + 2.0)) : "00:00:00");

				$line[$x] = $col;
			}
		}

		if (!$found)  continue;

		$result = $jb64->EncodeRecord($line);
		if (!$result["success"])  DisplayError("Unable to output JSON-Base64 data due to bad input.", $result, false);
		else  fwrite($fp2, $result["line"]);
	}
?>