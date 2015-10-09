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
			"t" => "table",
			"n" => "notnull",
			"?" => "help"
		),
		"rules" => array(
			"dsn" => array("arg" => true),
			"username" => array("arg" => true),
			"password" => array("arg" => true),
			"useschema" => array("arg" => true),
			"table" => array("arg" => true),
			"nodrop" => array("arg" => false),
			"notnull" => array("arg" => false),
			"timezone" => array("arg" => true),
			"help" => array("arg" => false)
		)
	);
	$args = ParseCommandLine($options);

	if (!isset($args["opts"]["dsn"]))  echo "Missing -d (-dsn) option.\n\n";

	if (count($args["params"]) < 1 || isset($args["opts"]["help"]) || !isset($args["opts"]["dsn"]))
	{
		echo "JSON-Base64 to database import command-line tool\n";
		echo "Purpose:  Imports a JSON-Base64 file into a database table.\n";
		echo "\n";
		echo "Syntax:  " . $args["file"] . " -d=DSN [options] SrcFilename\n";
		echo "Options:\n";
		echo "\t-d=DSN         The PDO DSN string used to connect to the database.\n";
		echo "\t-u=User        The username to connect to the database with.\n";
		echo "\t-p=****        The password to connect to the database with.\n";
		echo "\t-s=schema      The database/schema name to USE after connecting.\n";
		echo "\t-t=table       The database table to drop/create and use.\n";
		echo "\t-nodrop        Don't do DROP TABLE/CREATE TABLE.\n";
		echo "\t-timezone=tz   The timezone to use for output dates and times.  Default is 'UTC'.\n";
		echo "\t-n             Don't allow nulls.  Also sets fields to NOT NULL.\n";
		echo "\t-?             This help documentation.\n";
		echo "\n";
		echo "Examples:\n";
		echo "\tphp " . $args["file"] . " -d=sqlite:final.db -t=users -n final.jb64\n";

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

	// Read in the JSON-Base64 headers.
	$jb64 = new JB64Decode();

	$line = fgets($fp);
	$result = $jb64->DecodeHeaders($line);
	if (!$result["success"])  DisplayError("Unable to decode the first line of the input as a valid JSON-Base64 header.", $result);

	$headers = $result["headers"];

	// Dissect the database option.
	$pos = strpos($args["opts"]["dsn"], ":");
	if ($pos === false)  DisplayError("Unable to locate the type of database.  '" . $args["opts"]["dsn"] . "' is an invalid DSN string.");
	$dbtype = substr($args["opts"]["dsn"], 0, $pos);
	$dbtype = preg_replace('/[^a-z_]/', "", $dbtype);

	// Initialize and connect to the database.
	$filename = $rootpath . "/support/csdb/db_" . $dbtype . ".php";
	if (!file_exists($filename))  DisplayError("The database type '" . $dbtype . "' was unable to be located since '" . $filename . "' does not exist.");

	require_once $filename;

	try
	{
		echo "Connecting to '" . $args["opts"]["dsn"] . "'...\n";
		$dbclassname = "CSDB_" . $dbtype;

		$db = new $dbclassname();
		$db->Connect($args["opts"]["dsn"], (isset($args["opts"]["username"]) ? $args["opts"]["username"] : ""), (isset($args["opts"]["password"]) ? $args["opts"]["password"] : ""));
		echo "Connected to '" . $args["opts"]["dsn"] . "'.\n";
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

	if (isset($args["opts"]["table"]))  $tablename = $args["opts"]["table"];
	else if (!$stdin)  $tablename = str_ireplace(".jb64", "", $args["params"][0]);
	else  $tablename = "main";

	$tablename = str_replace(" ", "_", trim(preg_replace('/\s+/', " ", preg_replace('/[^a-z0-9]/', " ", strtolower($tablename)))));
	if ($tablename === "")  $tablename = "main";

	// Process the headers to determine the fields and types for the new table.
	$fields = array();
	$fieldmap = array();
	$notnull = (isset($args["opts"]["notnull"]) && $args["opts"]["notnull"]);
	foreach ($headers as $info)
	{
		$colname = preg_replace('/_+/', "_", preg_replace('/[^0-9a-z]/', "_", strtolower($info[0])));
		switch ($info[1])
		{
			case "boolean":  $fields[$colname] = array("BOOLEAN", "NOT NULL" => $notnull);  break;
			case "integer":  $fields[$colname] = array("INTEGER", 4, "NOT NULL" => $notnull);  break;
			case "number":  $fields[$colname] = array("FLOAT", 4, "NOT NULL" => $notnull);  break;
			case "date":  $fields[$colname] = array("DATETIME", "NOT NULL" => $notnull);  break;
			case "time":  $fields[$colname] = array("TIME", "NOT NULL" => $notnull);  break;
			case "string":  $fields[$colname] = array("STRING", 4, "NOT NULL" => $notnull);  break;
			case "binary":
			default:
			{
				$fields[$colname] = array("BINARY", 4, "NOT NULL" => $notnull);
				break;
			}
		}

		$fieldmap[] = array($colname, $info[1], (!isset($args["opts"]["notnull"]) || !$args["opts"]["notnull"]));
	}

	// Apply the field map.
	$result = $jb64->SetHeaderMap($fieldmap);
	if (!$result["success"])  DisplayError("Unable to set the JSON-Base64 header map.", $result);

	// Drop and recreate the table.
	if (!isset($args["opts"]["nodrop"]) || !$args["opts"]["nodrop"])
	{
		// Only attempt to drop if it exists.
		if ($db->TableExists($tablename))
		{
			try
			{
				echo "Dropping table '" . $tablename . "'...\n";
				$db->Query("DROP TABLE", array($tablename));
				echo "Dropped table '" . $tablename . "'.\n";
			}
			catch (Exception $e)
			{
				DisplayError("Unable to drop table '" . $tablename . "'.  " . $e->getMessage() . "\nIgnoring the exception and attempting to create the database table.", false, false);
			}
		}

		// Create the table.
		try
		{
			echo "Creating table '" . $tablename . "'...\n";
			$db->Query("CREATE TABLE", array($tablename, $fields, array()));
			echo "Created table '" . $tablename . "'.\n";
		}
		catch (Exception $e)
		{
			DisplayError("Unable to create table '" . $tablename . "'.  " . $e->getMessage());
		}
	}

	// Enable bulk import.
	try
	{
		echo "Enabling bulk import mode...\n";
		$db->Query("BULK IMPORT MODE", true);
		echo "Bulk import mode enabled.\n";
	}
	catch (Exception $e)
	{
		DisplayError("Unable to enable bulk import mode.  " . $e->getMessage() . "\nIgnoring the exception and inserting rows.", false, false);
	}

	// Set the target timezone.
	if (isset($args["opts"]["timezone"]) && strtoupper($args["opts"]["timezone"]) === "UTC")  unset($args["opts"]["timezone"]);
	if (isset($args["opts"]["timezone"]))
	{
		$tz = new DateTimeZone($args["opts"]["timezone"]);
		$utc = new DateTimeZone("UTC");
	}

	$numrecords = 0;
	$errors = 0;
	$linenum = 1;
	try
	{
		do
		{
			// Start a database transaction.
			$db->BeginTransaction();

			$rows = array($tablename);
			$size = 0;

			// Process the records.
			while (($line = fgets($fp)) !== false)
			{
				$linenum++;

				$line = trim($line);
				if ($line !== "")
				{
					$result = $jb64->DecodeRecord($line);
					if (!$result["success"])
					{
						DisplayError("Unable to decode line " . $linenum . " of the input as a valid JSON-Base64 record.", $result, false);

						$errors++;
					}
					else
					{
						$data = $result["assoc_data"];

						if (isset($args["opts"]["timezone"]))
						{
							foreach ($headers as $num => $info)
							{
								$type = $info[1];

								if ($type === "date")
								{
									$col = $data[$num];

									$dt = @DateTime::createFromFormat("Y-m-d H:i:s", $col, $utc);
									if ($dt === false)  $col = "0000-00-00 00:00:00";
									else
									{
										$dt->setTimezone($tz);
										$col = $dt->format("Y-m-d H:i:s");
									}

									$data[$num] = $col;
								}
								else if ($type === "time")
								{
									$col = $data[$num];

									$dt = @DateTime::createFromFormat("Y-m-d H:i:s", "1970-01-02 " . $col, $utc);
									if ($dt === false)  $col = "00:00:00";
									else
									{
										$dt->setTimezone($tz);
										$col = $dt->format("H:i:s");
									}

									$data[$num] = $col;
								}
							}
						}

						$fields2 = array();
						foreach ($fields as $colname => $info)
						{
							if (isset($data[$colname]))
							{
								$fields2[$colname] = $data[$colname];

								$size += strlen($data[$colname]);
							}
						}

						if (count($fields2) !== count($fields))
						{
							DisplayError("A mismatch error occurred while mapping a JSON-Base64 record to a database row insert.  Are your header record names \"clean\"?  That is, are you using database-agnostic field names?", array("success" => false, "error" => "More details available below.", "errorcode" => "see_info", "info" => array("db_columns" => $fields, "fields_read" => $fields2)));
						}

						$rows[] = $fields2;
						$rows[] = array();

						// Some benchmarks floating around show 100 rows per insert is optimal.
						// Also do a bulk insert if the total data size is > 10MB.
						if (count($rows) > 100 || $size > 10485760)
						{
							$db->Query("INSERT", $rows);

							$rows = array($tablename);
							$size = 0;
						}

						$numrecords++;
					}
				}

				if ($linenum % 10000 == 0)
				{
					echo $linenum . " rows processed.\n";

					// Some benchmarks floating around show 100 bulk inserts per transaction is optimal (100 * 100 = 10,000).
					break;
				}
			}

			// Perform the last bulk insert for this cycle.
			if (count($rows) > 1)  $db->Query("INSERT", $rows);

			// Commit the transaction.
			$db->Commit();

		} while ($line !== false);
	}
	catch (Exception $e)
	{
		$db->Rollback();

		DisplayError("An error occurred while inserting a row.  " . $e->getMessage());
	}

	echo $linenum . " rows processed.  Headers are valid.  " . $numrecords . " out of " . ($numrecords + $errors) . " records are valid.\n";

	// Disable bulk import.
	try
	{
		echo "Disabling bulk import mode...\n";
		$db->Query("BULK IMPORT MODE", false);
		echo "Bulk import mode disabled.\n";
	}
	catch (Exception $e)
	{
		DisplayError("Unable to disable bulk import mode.  " . $e->getMessage());
	}

	echo "Done.\n";
?>