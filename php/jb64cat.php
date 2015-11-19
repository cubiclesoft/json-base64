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
			"m" => "merge",
			"n" => "notnull",
			"r" => "rawcopy",
			"s" => "sourcetrack",
			"?" => "help"
		),
		"rules" => array(
			"merge" => array("arg" => false),
			"notnull" => array("arg" => false),
			"rawcopy" => array("arg" => false),
			"sourcetrack" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = ParseCommandLine($options);

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]))
	{
		echo "JSON-Base64 command-line concatenation tool\n";
		echo "Purpose:  Concatenates multiple JSON-Base64 files into a single output stream.  Without the -m option, files must have identical headers (including column order).  Writes to standard output.  Tip:  Use 'jb64map' to adjust input files prior to using this tool.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] Filename [MoreFilenames]\n";
		echo "Options:\n";
		echo "\t-m       Merges the files together.  Adds missing columns and removes duplicate columns.  Overrides -r.\n";
		echo "\t-n       Coerces null values to their defaults.  Only works with -m.\n";
		echo "\t-s=col   Adds a column that tracks the source filename.  Only works with -m.\n";
		echo "\t-r       Raw copy the data instead of reading each record and performing normalization.\n";
		echo "\t-?       This help documentation.\n";
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
	$mergemap = array();
	$merge = (isset($args["opts"]["merge"]) && $args["opts"]["merge"]);
	$null = !(isset($args["opts"]["notnull"]) && $args["opts"]["notnull"]);
	$sourcetrack = (isset($args["opts"]["sourcetrack"]) ? $args["opts"]["sourcetrack"] : false);
	$headers = ($merge ? array() : false);
	foreach ($args["params"] as $filename)
	{
		$fp = @fopen($filename, "rb");
		if ($fp === false)  DisplayError("Unable to open '" . $filename . "' for reading.");

		// Read headers.
		$jb64decode = new JB64Decode();

		$line = fgets($fp);
		$result = $jb64decode->DecodeHeaders($line);
		if (!$result["success"])  DisplayError("Unable to decode the first line of the input in '" . $filename . "' as a valid JSON-Base64 header.", $result);

		if ($merge)
		{
			foreach ($result["headers"] as $info)
			{
				if (!isset($mergemap[$info[0]]))
				{
					$mergemap[$info[0]] = count($headers);
					$headers[] = $info;
				}
			}
		}
		else if ($headers === false)
		{
			$headers = $result["headers"];
		}
		else if (json_encode($headers) !== json_encode($result["headers"]))
		{
			DisplayError("The headers in '" . $filename . "' do not exactly match the headers in '" . $args["params"][0] . "'.\n\n" . $args["params"][0] . ":\n" . json_encode($headers) . "\n\n" . $filename . ":\n" . json_encode($result["headers"]));
		}

		fclose($fp);
	}

	if ($merge && $sourcetrack !== false && !isset($mergemap[$sourcetrack]))
	{
		$mergemap[$sourcetrack] = count($headers);
		$headers[] = array($sourcetrack, "string");
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

		if ($merge)
		{
			// Build a map for the data.
			$mergemap2 = array();
			foreach ($result["headers"] as $info)
			{
				if (!isset($mergemap[$info[0]]))  DisplayError("The headers in '" . $filename . "' does not have a merge mapping for '" . $info[0] . "'.\n");

				$mergemap2[] = array(
					$info[0],
					$headers[$mergemap[$info[0]]][1],
					$null
				);
			}

			$result = $jb64decode->SetHeaderMap($mergemap2);
			if (!$result["success"])  DisplayError("Unable to set the JSON-Base64 header map for '" . $filename . "'.", $result);

			// Process the records.
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
						$data = array();
						foreach ($headers as $info)
						{
							if ($null)  $data[] = null;
							else
							{
								switch ($info[1])
								{
									case "boolean":  $data[] = false;  break;
									case "integer":  $data[] = 0;  break;
									case "number":  $data[] = (double)0;  break;
									case "date":  $data[] = "0000-00-00 00:00:00";  break;
									case "time":  $data[] = "00:00:00";  break;
									default:  $data[] = "";  break;
								}
							}
						}

						foreach ($result["assoc_data"] as $key => $val)
						{
							if (isset($mergemap[$key]))  $data[$mergemap[$key]] = $val;
						}

						if ($sourcetrack !== false)  $data[$mergemap[$sourcetrack]] = $filename;

						$result = $jb64encode->EncodeRecord($data);
						if (!$result["success"])  DisplayError("Unable to output JSON-Base64 data due to bad input in '" . $filename . "'.", $result, false);
						else  echo $result["line"];
					}
				}
			}
		}
		else
		{
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
		}

		fclose($fp);
	}
?>