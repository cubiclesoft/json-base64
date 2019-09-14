<?php
	// JSON-Base64 to CSV command-line converter.
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
			"d" => "delimiter",
			"q" => "quotes",
			"n" => "noheaders",
			"t" => "types",
			"?" => "help"
		),
		"rules" => array(
			"delimiter" => array("arg" => true),
			"quotes" => array("arg" => true),
			"noheaders" => array("arg" => false),
			"types" => array("arg" => false),
			"timezone" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = CLI::ParseCommandLine($options);

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]))
	{
		echo "JSON-Base64 to CSV command-line conversion tool\n";
		echo "Purpose:  Converts JSON-Base64 files to CSV.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] SrcFilename [DestFilename]\n";
		echo "Options:\n";
		echo "\t-d=,           The character used as a column delimiter in the source file.\n";
		echo "\t-q=\"           The character used to quote strings in the source file.\n";
		echo "\t-n             Don't output the header row.\n";
		echo "\t-t             Outputs the data types as well.  Not really a CSV file.\n";
		echo "\t-timezone=tz   The timezone to use for output dates and times.  Default is 'UTC'.\n";
		echo "\t-?             This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " final.jb64 final.csv\n";

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

	// Start CSV output.
	$jb64 = new JB64Decode();

	$line = fgets($fp);
	$result = $jb64->DecodeHeaders($line);
	if (!$result["success"])  DisplayError("Unable to decode the first line of the input as a valid JSON-Base64 header.", $result);

	$headers = $result["headers"];

	if (!isset($args["opts"]["noheader"]) || !$args["opts"]["noheader"])
	{
		$line = array();
		foreach ($headers as $info)  $line[] = $info[0];

		fputcsv($fp2, $line, $delimiter, $quotes, "\x00");
	}

	if (isset($args["opts"]["types"]) && $args["opts"]["types"])
	{
		$line = array();
		foreach ($headers as $info)  $line[] = $info[1];

		fputcsv($fp2, $line, $delimiter, $quotes, "\x00");
	}

	// Set the target timezone.
	if (isset($args["opts"]["timezone"]) && strtoupper($args["opts"]["timezone"]) === "UTC")  unset($args["opts"]["timezone"]);
	if (isset($args["opts"]["timezone"]))
	{
		$tz = new DateTimeZone($args["opts"]["timezone"]);
		$utc = new DateTimeZone("UTC");
	}

	// Map all headers to strings.
	$headers2 = $headers;
	foreach ($headers2 as $num => $info)
	{
		$headers2[$num][1] = "string";
		$headers2[$num][2] = false;
	}
	$result = $jb64->SetHeaderMap($headers2);
	if (!$result["success"])  DisplayError("Unable to set the JSON-Base64 header map.", $result);

	// Process the records.
	$linenum = 1;
	while (($line = fgets($fp)) !== false)
	{
		$linenum++;

		$line = trim($line);
		if ($line !== false)
		{
			$result = $jb64->DecodeRecord($line);
			if (!$result["success"])  DisplayError("Unable to decode line " . $linenum . " of the input as a valid JSON-Base64 record.", $result, false);
			else
			{
				$line = $result["ord_data"];

				if (isset($args["opts"]["timezone"]))
				{
					foreach ($headers as $num => $info)
					{
						$type = $info[1];

						if ($type === "date")
						{
							$col = $line[$num];

							$dt = @DateTime::createFromFormat("Y-m-d H:i:s", $col, $utc);
							if ($dt === false)  $col = "0000-00-00 00:00:00";
							else
							{
								$dt->setTimezone($tz);
								$col = $dt->format("Y-m-d H:i:s");
							}

							$line[$num] = $col;
						}
						else if ($type === "time")
						{
							$col = $line[$num];

							$dt = @DateTime::createFromFormat("Y-m-d H:i:s", "1970-01-02 " . $col, $utc);
							if ($dt === false)  $col = "00:00:00";
							else
							{
								$dt->setTimezone($tz);
								$col = $dt->format("H:i:s");
							}

							$line[$num] = $col;
						}
					}
				}

				fputcsv($fp2, $line, $delimiter, $quotes, "\x00");
			}
		}
	}
?>