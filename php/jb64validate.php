<?php
	// JSON-Base64 command-line validation tool.
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
			"?" => "help"
		),
		"rules" => array(
			"help" => array("arg" => false)
		)
	);
	$args = CLI::ParseCommandLine($options);

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]))
	{
		echo "JSON-Base64 command-line validation tool\n";
		echo "Purpose:  Validates a JSON-Base64 file.  Looks for decoding and data corruption issues.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] SrcFilename\n";
		echo "Options:\n";
		echo "\t-?   This help documentation.\n";
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

	$numrecords = 0;
	$errors = 0;
	$linenum = 1;
	while (($line = fgets($fp)) !== false)
	{
		$linenum++;

		$line = trim($line);
		if ($line !== "")
		{
			$result = $jb64->DecodeRecord($line);
			$result2 = DecodeLineAndAnalyze($line);
			if ($result["success"])  $numrecords++;
			else
			{
				DisplayError("Unable to decode line " . $linenum . " of the input as a valid JSON-Base64 record.", $result + array("info" => $result2), false);

				$errors++;
			}
		}

		if ($linenum % 10000 == 0)  echo $linenum . " rows processed.\n";
	}

	echo $linenum . " rows processed.  Headers are valid.  " . $numrecords . " out of " . ($numrecords + $errors) . " records are valid.\n";
?>