<?php
	// JSON-Base64 to database command-line converter.
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
	require_once $rootpath . "/support/utf8.php";

	$filename = $rootpath . "/support/csdb/db.php";
	if (!file_exists($filename))
	{
		echo "The JSON-Base64 to database import tool requires '" . $filename . "' to exist.\n\n";
		echo "CSDB is missing.\n\n";
		echo "Please install CSDB:\n\n";
		echo "https://github.com/cubiclesoft/csdb/\n";

		exit();
	}

	require_once $filename;

	// Process the command-line options.
	$options = array(
		"shortmap" => array(
			"d" => "dsn",
			"u" => "username",
			"p" => "password",
			"s" => "useschema",
			"q" => "query",
			"t" => "table",
			"?" => "help"
		),
		"rules" => array(
			"dsn" => array("arg" => true),
			"username" => array("arg" => true),
			"password" => array("arg" => true),
			"useschema" => array("arg" => true),
			"table" => array("arg" => true),
			"query" => array("arg" => true),
			"limit" => array("arg" => true),
			"dateformat" => array("arg" => true, "multiple" => true),
			"timezone" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = ParseCommandLine($options);

	if (!isset($args["opts"]["dsn"]))  echo "Missing -d (-dsn) option.\n\n";
	if (!isset($args["opts"]["table"]) && !isset($args["opts"]["query"]))  echo "Missing -t (-table) OR -q (-query) option.\n\n";

	if (isset($args["opts"]["help"]) || !isset($args["opts"]["dsn"]) || (!isset($args["opts"]["table"]) && !isset($args["opts"]["query"])))
	{
		echo "JSON-Base64 to database import command-line tool\n";
		echo "Purpose:  Imports a JSON-Base64 file into a database table.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " -d=DSN [options] [DestFilename]\n";
		echo "Options:\n";
		echo "\t-d=DSN         The PDO DSN string used to connect to the database.\n";
		echo "\t-u=User        The username to connect to the database with.\n";
		echo "\t-p=****        The password to connect to the database with.\n";
		echo "\t-s=schema      The database/schema name to USE after connecting.\n";
		echo "\t-t=table       The database table to dump.  Overrides the -q option.\n";
		echo "\t-q=query       The SQL query to run.  Use the -limit option to increase speed.\n";
		echo "\t-limit=num     The number of records to read to determine column types when -q is used.\n";
		echo "\t-dateformat=format   The string format of dates in the results.\n";
		echo "\t-timezone=tz   The timezone to use for input dates and times.  Default is 'UTC'.\n";
		echo "\t-?             This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " -d=sqlite:final.db -q=\"SELECT * FROM users ORDER BY name\"\n";

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

	// Dissect the database option.
	$pos = strpos($args["opts"]["dsn"], ":");
	if ($pos === false)  DisplayError("Unable to locate the type of database.  '" . $args["opts"]["dsn"] . "' is an invalid DSN string.");
	$dbtype = substr($args["opts"]["dsn"], 0, $pos);
	$dbtype = preg_replace('/[^a-z_]/', "", $dbtype);

	// Initialize and connect to the database.
	$filename = $rootpath . "/support/csdb/db_" . $dbtype . ".php";
	if (!file_exists($filename))  DisplayError("The database type '" . $dbtype . "' was unable to be located since '" . $filename . "' does not exist.");

	require_once $filename;

	$dbclassname = "CSDB_" . $dbtype;

	try
	{
		$db = new $dbclassname();
		$db->Connect($args["opts"]["dsn"], (isset($args["opts"]["username"]) ? $args["opts"]["username"] : ""), (isset($args["opts"]["password"]) ? $args["opts"]["password"] : ""));
	}
	catch (Exception $e)
	{
		DisplayError("Unable to connect to the database.\n\n" . $e->getMessage());
	}

	try
	{
		if (isset($args["opts"]["useschema"]))  $db->Query("USE", $args["opts"]["useschema"]);
	}
	catch (Exception $e)
	{
		DisplayError("Unable to switch databases/schemas to '" . $args["opts"]["useschema"] . "'.\n\n" . $e->getMessage());
	}

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
	else  $tz = new DateTimeZone("UTC");

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

	// No complaining about this function.
	function ProcessCurrentRow()
	{
		global $headers, $row, $dateformats, $tz;

		$data = array();
		foreach ($headers as $info)
		{
			$col = $row->$info[0];

			$type = $info[1];

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

			$data[] = $col;
		}

		return $data;
	}

	if (isset($args["opts"]["table"]))
	{
		// Extract the table definition.
		$tablename = $args["opts"]["table"];
		try
		{
			$row = $db->GetRow("SHOW CREATE TABLE", array($tablename));
		}
		catch (Exception $e)
		{
			DisplayError("Unable to retrieve database table definition for '" . $tablename . "'.\n\n" . $e->getMessage());
		}

		$headers = array();
		foreach ($row->opts[1] as $key => $info)
		{
			switch ($info[0])
			{
				case "INTEGER":
				{
					if ($info[1] > 3 && $db->GetOne("SELECT", array("MAX(" . $db->QuoteIdentifier($key) . ") > " . PHP_INT_MAX, "FROM" => "?"), $tablename))  $type = "string";
					else  $type = "integer";

					break;
				}
				case "FLOAT":
				case "NUMERIC":  $type = "number";  break;
				case "DATE":
				case "DATETIME":  $type = "date";  break;
				case "TIME":  $type = "time";  break;
				case "BINARY":  $type = "binary";  break;
				case "STRING":
				default:  $type = "string";  break;
			}

			$headers[] = array($key, $type);
		}

		// Output file/pipe.
		if (count($args["params"]) > 0)
		{
			$fp2 = @fopen($args["params"][0], "wb");
			if ($fp2 === false)  DisplayError("Unable to open '" . $args["params"][0] . "' for writing.");
		}
		else
		{
			$fp2 = STDOUT;
		}

		// Write out the headers.
		$jb64 = new JB64Encode();
		$result = $jb64->EncodeHeaders($headers);
		if (!$result["success"])  DisplayError("Unable to generate JSON-Base64 headers due to bad input.", $result);

		fprintf($fp2, $result["line"]);

		// Enable large results mode.
		$db->LargeResults(true);

		// Run the query and dump the results.
		try
		{
			$result = $db->Query("SELECT", array(
				"*",
				"FROM" => "?",
			), $tablename);

			while ($row = $result->NextRow())
			{
				$data = ProcessCurrentRow();

				$result2 = $jb64->EncodeRecord($data);
				if (!$result2["success"])  DisplayError("Unable to output JSON-Base64 data due to bad input in '" . $tablename . "'.", $result2, false);
				else  fprintf($fp2, $result2["line"]);
			}
		}
		catch (Exception $e)
		{
			DisplayError("Unable to run SQL query to dump '" . $tablename . "'.\n\n" . $e->getMessage());
		}
	}
	else
	{
		// Enable large results mode.
		$db->LargeResults(true);

		// Automatically calculate compatible JSON-Base64 header types from the data.
		$headers = false;
		try
		{
			// Raw SQL query from the command-line.  Nothing could possibly go wrong with this.
			$result = $db->Query(false, $args["opts"]["query"]);

			$limit = (isset($args["opts"]["limit"]) ? (int)$args["opts"]["limit"] : false);

			while (($limit === false || $limit > 0) && ($row = $result->NextRow()))
			{
				if ($headers === false)
				{
					$headers = array();
					foreach ($row as $key => $val)  $headers[] = array($key, "boolean");
				}

				$data = array();
				foreach ($row as $key => $val)
				{
					$data[] = $val;
				}

				for ($x = 0; $x < count($headers); $x++)
				{
					$col = $data[$x];
					$type = $headers[$x][1];

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

					if ($type === "string")
					{
						if (!UTF8::IsValid($col))  $type = "binary";
					}

					$headers[$x][1] = $type;
				}

				if ($limit !== false)  $limit--;
			}
		}
		catch (Exception $e)
		{
			DisplayError("An error occurred while running the SQL query.\n\n" . $e->getMessage());
		}

		// No results available.
		if ($headers === false)  DisplayError("The SQL query had zero results.");

		// Output file/pipe.
		if (count($args["params"]) > 0)
		{
			$fp2 = @fopen($args["params"][0], "wb");
			if ($fp2 === false)  DisplayError("Unable to open '" . $args["params"][0] . "' for writing.");
		}
		else
		{
			$fp2 = STDOUT;
		}

		// Write out the headers.
		$jb64 = new JB64Encode();
		$result = $jb64->EncodeHeaders($headers);
		if (!$result["success"])  DisplayError("Unable to generate JSON-Base64 headers due to bad input.", $result);

		fprintf($fp2, $result["line"]);

		// Reconnect to the database.
		// Gets around issues with enabling large results and only pulling partial results.
		try
		{
			$db = new $dbclassname();
			$db->Connect($args["opts"]["dsn"], (isset($args["opts"]["username"]) ? $args["opts"]["username"] : ""), (isset($args["opts"]["password"]) ? $args["opts"]["password"] : ""));
		}
		catch (Exception $e)
		{
			DisplayError("Unable to reconnect to the database.\n\n" . $e->getMessage());
		}

		try
		{
			if (isset($args["opts"]["useschema"]))  $db->Query("USE", $args["opts"]["useschema"]);
		}
		catch (Exception $e)
		{
			DisplayError("Unable to switch databases/schemas to '" . $args["opts"]["useschema"] . "'.\n\n" . $e->getMessage());
		}

		// Enable large results mode again.
		$db->LargeResults(true);

		// Run the query and dump the results.
		try
		{
			// Raw SQL query from the command-line.  Nothing could possibly go wrong with this.
			$result = $db->Query(false, $args["opts"]["query"]);

			while ($row = $result->NextRow())
			{
				$data = ProcessCurrentRow();

				$result2 = $jb64->EncodeRecord($data);
				if (!$result2["success"])  DisplayError("Unable to output JSON-Base64 data due to bad input in query results.", $result2, false);
				else  fprintf($fp2, $result2["line"]);
			}
		}
		catch (Exception $e)
		{
			DisplayError("Unable to run SQL query.\n\n" . $e->getMessage());
		}
	}
?>