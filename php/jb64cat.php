<?php
	// JSON-Base64 command-line concatenation tool.
	// Public Domain.  This code only.

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
			"r" => "rawcopy",
			"?" => "help"
		),
		"rules" => array(
			"rawcopy" => array("arg" => false),
			"help" => array("arg" => false)
		)
	);
	$args = ParseCommandLine($options);

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]))
	{
		echo "JSON-Base64 command-line concatenation tool\n";
		echo "Purpose:  Concatenates multiple JSON-Base64 files with identical headers (including column order) into a single output stream.  Writes to standard output.  Tip:  Use 'jb64map' to adjust input files prior to using this tool.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] Filename [MoreFilenames]\n";
		echo "Options:\n";
		echo "\t-r   Raw copy the data instead of reading each record and performing normalization.\n";
		echo "\t-?   This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " test_cleaned.jb64 test2_cleaned.jb64 > final.jb64\n";

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

	// Open each file and read the headers.
	$headers = false;
	foreach ($args["params"] as $filename)
	{
		$fp = @fopen($filename, "rb");
		if ($fp === false)  DisplayError("Unable to open '" . $filename . "' for reading.");

		// Read headers.
		$jb64decode = new JB64Decode();

		$line = fgets($fp);
		$result = $jb64decode->DecodeHeaders($line);
		if (!$result["success"])  DisplayError("Unable to decode the first line of the input in '" . $filename . "' as a valid JSON-Base64 header.", $result);

		if ($headers === false)  $headers = $result["headers"];
		else if (json_encode($headers) !== json_encode($result["headers"]))  DisplayError("The headers in '" . $filename . "' do not exactly match the headers in '" . $args["params"][0] . "'.\n\n" . $args["params"][0] . ":\n" . json_encode($headers) . "\n\n" . $filename . ":\n" . json_encode($result["headers"]));

		fclose($fp);
	}

	// Write out the headers.
	$jb64encode = new JB64Encode();
	$result = $jb64encode->EncodeHeaders($headers);
	if (!$result["success"])  DisplayError("Unable to generate JSON-Base64 headers due to bad input.", $result);

	echo $result["line"];

	foreach ($args["params"] as $filename)
	{
		$fp = @fopen($filename, "rb");
		if ($fp === false)  DisplayError("Unable to open '" . $filename . "' for reading.");

		// Read and mostly ignore the headers.
		$jb64decode = new JB64Decode();

		$line = fgets($fp);
		$result = $jb64decode->DecodeHeaders($line);
		if (!$result["success"])  DisplayError("Unable to decode the first line of the input in '" . $filename . "' as a valid JSON-Base64 header.", $result);

		// Sanity check.
		if (json_encode($headers) !== json_encode($result["headers"]))  DisplayError("The headers in '" . $filename . "' do not exactly match the headers in '" . $args["params"][0] . "'.\n\n" . $args["params"][0] . ":\n" . json_encode($headers) . "\n\n" . $filename . ":\n" . json_encode($result["headers"]));

		// Process the records.
		if (isset($args["opts"]["rawcopy"]) && $args["opts"]["rawcopy"])
		{
			while (($data = fread($fp, 1048576)) !== false && !feof($fp))
			{
				echo $data;
			}

			if ($data !== false)  echo $data;
		}
		else
		{
			// In most cases, this code will do nothing and a bulk copy (via rawcopy above) makes more sense for performance reasons.
			// However, this will normalize output data from non-conformant JSON-Base64 libraries as well as not copy invalid data.
			$linenum = 1;
			while (($line = fgets($fp)) !== false)
			{
				$linenum++;

				$line = trim($line);
				if ($line !== "")
				{
					$result = $jb64decode->DecodeRecord($line);
					if (!$result["success"])  DisplayError("Unable to decode line " . $linenum . " of the input in '" . $filename . "' as a valid JSON-Base64 record.", $result, false);
					else
					{
						$data = $result["ord_data"];

						$result = $jb64encode->EncodeRecord($data);
						if (!$result["success"])  DisplayError("Unable to output JSON-Base64 data due to bad input in '" . $filename . "'.", $result, false);
						else  echo $result["line"];
					}
				}
			}
		}

		fclose($fp);
	}
?>