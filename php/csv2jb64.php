<?php
	// CSV to JSON-Base64 command-line converter.
	// (C) 2014 CubicleSoft.

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

	// Process the command-line options.
	$options = array(
		"shortmap" => array(
			"c" => "columns",
			"d" => "defaulttypes",
			"k" => "keep",
			"s" => "skipfirst",
			"t" => "types",
			"?" => "help"
		),
		"rules" => array(
			"columns" => array("arg" => true),
			"dateformat" => array("arg" => true, "multiple" => true),
			"defaulttypes" => array("arg" => false),
			"delimiter" => array("arg" => true),
			"keep" => array("arg" => false),
			"limit" => array("arg" => true),
			"quotes" => array("arg" => true),
			"skipfirst" => array("arg" => false),
			"types" => array("arg" => true),
			"timezone" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = ParseCommandLine($options);

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]))
	{
		echo "CSV to JSON-Base64 command-line conversion tool\n";
		echo "Purpose:  Converts CSV files to JSON-Base64.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] SrcFilename [DestFilename]\n";
		echo "Options:\n";
		echo "\t-c=columns     Comma-separated list of column names.  Forced header row.\n";
		echo "\t-t=types       Comma-separated list of column data types.\n";
		echo "\t-dateformat=format   The string format of dates in the source file.\n";
		echo "\t-timezone=tz   The timezone to use for input dates and times.  Default is '" . date_default_timezone_get() . "'.\n";
		echo "\t-delimiter=,   The character used as a column delimiter in the source file.\n";
		echo "\t-quotes=\"      The character used to quote strings in the source file.\n";
		echo "\t-limit=num     The number of records to read to determine column types.\n";
		echo "\t-d             Assumes a default type of 'string' for all columns.\n";
		echo "\t-k             Keep outer whitespace on each column of data.\n";
		echo "\t-s             Skip first row of input.  Only valid when -c is specified.\n";
		echo "\t-?             This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " test.csv test.jb64\n";
		echo "\tphp " . $args["file"] . " -c=id,name -t=integer,string -timezone=UTC test.csv\n";

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
	$stdin = ($args["params"][0] === "STDIN" && !file_exists($args["params"][0]));
	$fp = ($stdin ? STDIN : @fopen($args["params"][0], "rb"));
	if ($fp === false)  DisplayError("Unable to open '" . $args["params"][0] . "' for reading.");

	$delimiter = (isset($args["opts"]["delimiter"]) ? $args["opts"]["delimiter"] : ",");
	$quotes = (isset($args["opts"]["quotes"]) ? $args["opts"]["quotes"] : "\"");

	// Column names.
	if (isset($args["opts"]["columns"]))
	{
		$columns2 = explode(",", $args["opts"]["columns"]);

		if (isset($args["opts"]["skipfirst"]) && $args["opts"]["skipfirst"])  fgetcsv($fp, 0, $delimiter, $quotes, "\x00");
	}
	else
	{
		$columns2 = fgetcsv($fp, 0, $delimiter, $quotes, "\x00");
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

	// Initialize date formats.
	$dateformats = array(
		"Y-m-d H:i:s",
		"Y-m-d|",
	);

	if (isset($args["opts"]["dateformat"]))
	{
		foreach ($args["opts"]["dateformat"] as $format)  $dateformats[] = $format;
	}

	// Set the source timezone.
	if (isset($args["opts"]["timezone"]))  $tz = new DateTimeZone($args["opts"]["timezone"]);
	else  $tz = new DateTimeZone(date_default_timezone_get());

	// Handle automatic type calculations by processing the data.
	if ($autotype !== false)
	{
		$limit = (isset($args["opts"]["limit"]) ? (int)$args["opts"]["limit"] : false);

		while (($limit === false || $limit > 0) && ($line = fgetcsv($fp, 0, $delimiter, $quotes, "\x00")) !== false)
		{
			if (count($line))
			{
				while (count($line) < count($columns))  $line[] = "";

				$line = array_slice($line, 0, count($columns));

				for ($x = $autotype; $x < count($types); $x++)
				{
					$col = UTF8::MakeValid($line[$x]);

					if (!isset($args["opts"]["keep"]) || !$args["opts"]["keep"])  $col = trim($col);

					$type = $types[$x];

					if ($type === "boolean")
					{
						if (!is_numeric($col))  $type = "date";
						else if (!is_int($col + 0))  $type = "number";
						else if ((int)$col !== 0 && (int)$col !== 1)  $type = "integer";
					}

					if ($type === "integer")
					{
						if (!is_numeric($col))  $type = "date";
						else if (!is_int($col + 0))  $type = "number";
					}

					if ($type === "number")
					{
						if (!is_numeric($col))  $type = "date";
					}

					// Technically this is a violation of JSON-Base64, but autodetection is weird.
					if ($type === "date")
					{
						$found = false;
						foreach ($dateformats as $format)
						{
							if (@DateTime::createFromFormat($format, $col, $tz) !== false)
							{
								$found = true;

								break;
							}
						}

						if (!$found)  $type = "time";
					}

					if ($type === "time")
					{
						if (!preg_match('/^\d{1,2}:\d{1,2}:\d{1,2}$/', $col))  $type = "string";
					}

					$types[$x] = $type;
				}

				if ($limit !== false)  $limit--;
			}
		}

		// Reopen the input file.
		fclose($fp);
		$fp = @fopen($args["params"][0], "rb");
		if ($fp === false)  DisplayError("Unable to open '" . $args["params"][0] . "' for reading.");

		// Skip first row.
		if (!isset($args["opts"]["columns"]) || (isset($args["opts"]["skipfirst"]) && $args["opts"]["skipfirst"]))  fgetcsv($fp);
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

	// Swiped from the JSON-Base64 PHP library.
	function FixTime($ts)
	{
		$ts = array_pad(explode(" ", trim(preg_replace('/\s+/', " ", preg_replace('/[^0-9]/', " ", (string)$ts)))), 3, 0);

		$x = (count($ts) == 6 ? 3 : 0);
		$hour = str_pad((string)(int)$ts[$x], 2, "0", STR_PAD_LEFT);
		$min = str_pad((string)(int)$ts[$x + 1], 2, "0", STR_PAD_LEFT);
		$sec = str_pad((string)(int)$ts[$x + 2], 2, "0", STR_PAD_LEFT);

		return $hour . ":" . $min . ":" . $sec;
	}

	// Transform the rest of the CSV file.
	while (($line = fgetcsv($fp, 0, $delimiter, $quotes, "\x00")) !== false)
	{
		while (count($line) < count($columns))  $line[] = "";

		$line = array_slice($line, 0, count($columns));

		foreach ($line as $num => $col)
		{
			$col = UTF8::MakeValid($col);

			if (!isset($args["opts"]["keep"]) || !$args["opts"]["keep"])  $col = trim($col);

			$type = $headers[$num][1];

			if ($type === "date")
			{
				$found = false;
				foreach ($dateformats as $format)
				{
					$dt = @DateTime::createFromFormat($format, $col, $tz);

					if ($dt !== false)
					{
						$col = gmdate("Y-m-d H:i:s", $dt->getTimestamp());

						$found = true;

						break;
					}
				}

				if (!$found)  $col = "0000-00-00 00:00:00";
			}
			else if ($type === "time")
			{
				if (!preg_match('/^\d{1,2}:\d{1,2}:\d{1,2}$/', $col))  $col = "00:00:00";
				else
				{
					$dt = @DateTime::createFromFormat("Y-m-d H:i:s", "1970-01-02 " . FixTime($col), $tz);

					$col = gmdate("H:i:s", $dt->getTimestamp());
				}
			}

			$line[$num] = $col;
		}

		$result = $jb64->EncodeRecord($line);
		if (!$result["success"])  DisplayError("Unable to output JSON-Base64 data due to bad input.", $result, false);
		else  fwrite($fp2, $result["line"]);
	}
?>