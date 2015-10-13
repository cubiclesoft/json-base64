<?php
	// JSON-Base64 command-line data mapping tool.
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
			"a" => "addcol",
			"c" => "changetype",
			"d" => "dropcol",
			"i" => "interactive",
			"l" => "limit",
			"n" => "notnull",
			"o" => "reorder",
			"p" => "parser",
			"r" => "renamecol",
			"?" => "help"
		),
		"rules" => array(
			"addcol" => array("arg" => true, "multiple" => true),
			"changetype" => array("arg" => true, "multiple" => true),
			"dbnames" => array("arg" => false),
			"delimiter" => array("arg" => true),
			"dropcol" => array("arg" => true, "multiple" => true),
			"interactive" => array("arg" => false),
			"limit" => array("arg" => true),
			"notnull" => array("arg" => false),
			"parser" => array("arg" => true),
			"renamecol" => array("arg" => true, "multiple" => true),
			"reorder" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = ParseCommandLine($options);

	if (isset($args["opts"]["interactive"]) && count($args["params"]) < 2)  echo "DestFilename is required when using the -interactive option.\n\n";

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]) || (isset($args["opts"]["interactive"]) && count($args["params"]) < 2))
	{
		echo "JSON-Base64 command-line data mapping tool\n";
		echo "Purpose:  Cleanup of JSON-Base64 headers and data.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " [options] SrcFilename [DestFilename]\n";
		echo "Options:\n";
		echo "\t-r=col,newcol       Renames a column.\n";
		echo "\t-c=col,newtype      Changes a column's type.\n";
		echo "\t-a=col,type[,val]   Adds a column, type, and default value.\n";
		echo "\t-d=col              Drops a column.\n";
		echo "\t-o=col[,col,col]    Reorder output columns as specified.  Unspecified columns are left in their original order.\n";
		echo "\t-dbnames            Renames all columns to database-friendly names.\n";
		echo "\t-i                  Interactive mode.  Speeds up bulk column changes.  DestFilename is required.\n";
		echo "\t-p=phpfile          Includes the specified PHP file and calls your custom AlterRecord() function for each record.\n";
		echo "\t-l=num              The number of records to process.  Default is all records.\n";
		echo "\t-delimiter=,        Sets the delimiter for the other options.\n";
		echo "\t-n                  Coerces null values to their defaults.\n";
		echo "\t-?                  This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " -r=ID,id -a=created,date -n test.jb64\n";
		echo "\tphp " . $args["file"] . " \"-r=User ID,id\" -r=Name,name \"-r=Created On,created\" -o=id,name -n test2.jb64\n";

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

	function GetUserInput($question, $default)
	{
		echo $question . " [" . $default . "]:  ";

		$line = fgets(STDIN);
		$line = trim($line);
		if ($line === "")  $line = $default;

		return $line;
	}

	// Input file.
	$interactive = (isset($args["opts"]["interactive"]) && $args["opts"]["interactive"]);
	$stdin = (!$interactive && $args["params"][0] === "STDIN" && !file_exists($args["params"][0]));
	$fp = ($stdin ? STDIN : @fopen($args["params"][0], "rb"));
	if ($fp === false)  DisplayError("Unable to open '" . $args["params"][0] . "' for reading." . ($interactive ? "  Interactive mode requires a physical input file." : ""));

	// Read headers.
	$jb64decode = new JB64Decode();

	$line = fgets($fp);
	$result = $jb64decode->DecodeHeaders($line);
	if (!$result["success"])  DisplayError("Unable to decode the first line of the input as a valid JSON-Base64 header.", $result);

	$headers = $result["headers"];

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

	$delimiter = (isset($args["opts"]["delimiter"]) ? $args["opts"]["delimiter"] : ",");
	$null = !(isset($args["opts"]["notnull"]) && $args["opts"]["notnull"]);

	// Let the library handle mapping existing columns and types.
	$headers2 = $headers;

	if (isset($args["opts"]["renamecol"]))
	{
		foreach ($args["opts"]["renamecol"] as $info)
		{
			$info = explode($delimiter, $info);
			if (count($info) >= 2)
			{
				foreach ($headers2 as $num => $info2)
				{
					if ($info2[0] === $info[0])  $headers2[$num][0] = $info[1];
				}
			}
		}
	}

	if (isset($args["opts"]["dbnames"]) && $args["opts"]["dbnames"])
	{
		foreach ($headers2 as $num => $info2)
		{
			$info2[0] = preg_replace('/\s+/', "_", trim(preg_replace('/[^a-z0-9]/', " ", strtolower(trim($info2[0])))));

			$headers2[$num] = $info2;
		}
	}

	if (isset($args["opts"]["changetype"]))
	{
		foreach ($args["opts"]["changetype"] as $info)
		{
			$info = explode($delimiter, $info);
			if (count($info) >= 2)
			{
				foreach ($headers2 as $num => $info2)
				{
					if ($info2[0] === $info[0])  $headers2[$num][1] = $info[1];
				}
			}
		}
	}

	if ($interactive)
	{
		foreach ($headers2 as $num => $info2)
		{
			$info2[0] = GetUserInput("Column " . ($num + 1) . " name", $info2[0]);
			$info2[1] = GetUserInput("Column " . ($num + 1) . " type", $info2[1]);

			if ($info2[1] !== "-")  $headers2[$num] = $info2;
			else
			{
				if (!isset($args["opts"]["dropcol"]))  $args["opts"]["dropcol"] = array();
				$args["opts"]["dropcol"][] = $info2[0];
			}
		}
	}

	$finalheaders = $headers2;

	foreach ($headers2 as $num => $info)
	{
		$headers2[$num][2] = $null;
	}

	$result = $jb64decode->SetHeaderMap($headers2);
	if (!$result["success"])  DisplayError("Unable to set the JSON-Base64 header map.", $result);

	// Preprocess added and dropped columns.
	$addcols = array();
	$dropcols = array();
	if (isset($args["opts"]["addcol"]))
	{
		foreach ($args["opts"]["addcol"] as $num => $info)
		{
			$info = explode($delimiter, $info);
			if (count($info) >= 2)
			{
				if (count($info) < 3)
				{
					if ($null)  $info[] = null;
					else
					{
						switch ($info[1])
						{
							case "boolean":  $info[] = false;  break;
							case "integer":  $info[] = 0;  break;
							case "number":  $info[] = (double)0;  break;
							case "date":  $info[] = "0000-00-00 00:00:00";  break;
							case "time":  $info[] = "00:00:00";  break;
							default:  $info[] = "";  break;
						}
					}
				}

				$finalheaders[] = array($info[0], $info[1]);

				$addcols[] = $info;
			}
		}
	}

	if (isset($args["opts"]["dropcol"]))
	{
		foreach ($args["opts"]["dropcol"] as $info)
		{
			foreach ($finalheaders as $num => $info2)
			{
				if ($info2[0] === $info)
				{
					$dropcols[] = $num;

					unset($finalheaders[$num]);
				}
			}
		}

		$finalheaders = array_values($finalheaders);
	}

	$headerpos = array();
	foreach ($finalheaders as $num => $info)
	{
		$headerpos[$info[0]] = $num;
	}

	// Reorder the header columns.
	$reordermap = array();
	if ($interactive)  $args["opts"]["reorder"] = GetUserInput("Column order", implode($delimiter, array_keys($headerpos)));
	if (isset($args["opts"]["reorder"]))
	{
		$headers4 = array();

		$names = explode($delimiter, $args["opts"]["reorder"]);
		foreach ($names as $num => $name)
		{
			if (isset($headerpos[$name]) && isset($finalheaders[$headerpos[$name]]))
			{
				$reordermap[] = $headerpos[$name];

				$headers4[] = $finalheaders[$headerpos[$name]];
				unset($finalheaders[$headerpos[$name]]);
			}
		}

		foreach ($finalheaders as $info)  $headers4[] = $info;

		$finalheaders = $headers4;

		$headerpos = array();
		foreach ($finalheaders as $num => $info)
		{
			$headerpos[$info[0]] = $num;
		}
	}

	// Load the data parser (if any).  The parser can further modify the $finalheaders array at this point.
	define("JB64_MAP", 1);
	if (isset($args["opts"]["parser"]))  require_once $args["opts"]["parser"];

	// Write headers to new file.
	$jb64encode = new JB64Encode();
	$result = $jb64encode->EncodeHeaders($finalheaders);
	if (!$result["success"])  DisplayError("Unable to generate JSON-Base64 headers due to bad input.", $result);

	fwrite($fp2, $result["line"]);

	$limit = (isset($args["opts"]["limit"]) ? (int)$args["opts"]["limit"] : false);

	// Process the records.
	$linenum = 1;
	while (($limit === false || $limit > 0) && ($line = fgets($fp)) !== false)
	{
		$linenum++;

		$line = trim($line);
		if ($line !== "")
		{
			$result = $jb64decode->DecodeRecord($line);
			if (!$result["success"])  DisplayError("Unable to decode line " . $linenum . " of the input as a valid JSON-Base64 record.", $result, false);
			else
			{
				$data = $result["ord_data"];

				// Add, drop, and reorder columns.
				foreach ($addcols as $info)
				{
					$data[] = $info[2];
				}

				foreach ($dropcols as $pos)
				{
					unset($data[$pos]);
				}

				$data2 = array_values($data);

				$data = array();
				foreach ($reordermap as $pos)
				{
					$data[] = $data2[$pos];
					unset($data2[$pos]);
				}

				foreach ($data2 as $info)  $data[] = $info;

				// Let a custom AlterRecord() function modify the data.
				$result = (is_callable("AlterRecord") ? call_user_func_array("AlterRecord", array($finalheaders, $headerpos, &$data)) : true);

				if ($result)
				{
					// Encode the final record.
					$result = $jb64encode->EncodeRecord($data);
					if (!$result["success"])  DisplayError("Unable to output JSON-Base64 data due to bad input.", $result, false);
					else  fwrite($fp2, $result["line"]);
				}

				if ($limit !== false)  $limit--;
			}
		}
	}
?>