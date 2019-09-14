<?php
	// JSON-Base64 command-line head/sample tool.
	// (C) 2014 CubicleSoft.

	if (!isset($_SERVER["argc"]) || !$_SERVER["argc"])
	{
		echo "This file is intended to be run from the command-line.";

		exit();
	}

	// Temporary root.
	$rootpath = str_replace("\\", "/", dirname(__FILE__));

	require_once $rootpath . "/support/cli.php";
	require_once $rootpath . "/support/jb64.php";

	// Process the command-line options.
	$options = array(
		"shortmap" => array(
			"r" => "rawdecode",
			"n" => "numrecords",
			"?" => "help"
		),
		"rules" => array(
			"rawdecode" => array("arg" => false),
			"numrecords" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = CLI::ParseCommandLine($options);

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]))
	{
		echo "JSON-Base64 command-line header/sample tool\n";
		echo "Purpose:  Shows header information and sample data from a JSON-Base64 file.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] SrcFilename\n";
		echo "Options:\n";
		echo "\t-n=records   Number of records to display.  Default is 10.\n";
		echo "\t-r           Dumps the raw Base64 decoded JSON objects.\n";
		echo "\t-?           This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " test.jb64\n";

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

	function DecodeLineAndAnalyze($line)
	{
		$line = trim($line);

		$md5status = "missing";
		if (strlen($line) > 33 && $line{strlen($line) - 33} == ".")
		{
			$md5 = substr($line, -32);
			$line = substr($line, 0, -33);

			if ($md5 !== md5($line))  $md5status = "invalid";
			else  $md5status = "valid";
		}

		$data = @base64_decode(str_replace(array("-", "_"), array("+", "/"), $line));
		$data2 = @json_decode($data, true);
		$jsonstatus = (is_array($data2) ? "valid" : "invalid");

		return array("success" => true, "base64_decoded" => $data, "json_decoded" => $data2, "md5" => $md5status, "json" => $jsonstatus);
	}

	// Input file.
	$stdin = ($args["params"][0] === "STDIN" && !file_exists($args["params"][0]));
	$fp = ($stdin ? STDIN : @fopen($args["params"][0], "rb"));
	if ($fp === false)  DisplayError("Unable to open '" . $args["params"][0] . "' for reading.");

	// Read headers.
	$jb64 = new JB64Decode();

	$line = fgets($fp);
	$result = $jb64->DecodeHeaders($line);
	$result2 = DecodeLineAndAnalyze($line);
	if (!$result["success"])  DisplayError("Unable to decode the first line of the input as a valid JSON-Base64 header.", $result + array("info" => $result2));

	$headers = $result["headers"];
	$records = array();

	// Handle output differently depending on input parameters.
	if (isset($args["opts"]["rawdecode"]) && $args["opts"]["rawdecode"])
	{
		echo "Headers (MD5 " . $result2["md5"] . ", JSON " . $result2["json"] . "):\n";
		echo "  " . $result2["base64_decoded"] . "\n\n";
	}
	else
	{
		$widths = array();
		foreach ($headers as $info)  $widths[] = max(strlen($info[0]), strlen($info[1]));
	}

	// Read specified number of records.
	$numrecords = (isset($args["opts"]["numrecords"]) && (int)$args["opts"]["numrecords"] >= 0 ? (int)$args["opts"]["numrecords"] : 10);

	$linenum = 1;
	while (count($records) < $numrecords && ($line = fgets($fp)) !== false)
	{
		$linenum++;

		$line = trim($line);
		if ($line !== "")
		{
			$result = $jb64->DecodeRecord($line);
			$result2 = DecodeLineAndAnalyze($line);
			if (!$result["success"])  DisplayError("Unable to decode line " . $linenum . " of the input as a valid JSON-Base64 record.", $result + array("info" => $result2), false);
			else
			{
				$record = $result["ord_data"];
				$records[] = $record;

				if (isset($args["opts"]["rawdecode"]) && $args["opts"]["rawdecode"])
				{
					echo "Record #" . count($records) . " (MD5 " . $result2["md5"] . ", JSON " . $result2["json"] . "):\n";
					echo "  " . $result2["base64_decoded"] . "\n\n";
				}
				else
				{
					foreach ($record as $num => $info)  $widths[$num] = max($widths[$num], strlen($info));
				}
			}
		}
	}

	if (!isset($args["opts"]["rawdecode"]) || !$args["opts"]["rawdecode"])
	{
		foreach ($widths as $size)  echo "+-" . str_repeat("-", $size) . "-";
		echo "+\n";
		foreach ($headers as $num => $info)  echo "| " . str_pad($info[0], $widths[$num]) . " ";
		echo "|\n";
		foreach ($headers as $num => $info)  echo "| " . str_pad($info[1], $widths[$num]) . " ";
		echo "|\n";
		foreach ($widths as $size)  echo "+-" . str_repeat("-", $size) . "-";
		echo "+\n";

		foreach ($records as $record)
		{
			foreach ($record as $num => $info)  echo "| " . str_pad($info, $widths[$num]) . " ";
			echo "|\n";
		}

		foreach ($widths as $size)  echo "+-" . str_repeat("-", $size) . "-";
		echo "+\n";
	}
?>