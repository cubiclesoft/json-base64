<?php
	// JSON-Base64 test suite.  (http://jb64.org/)
	// Public Domain.

	if (!isset($_SERVER["argc"]) || !$_SERVER["argc"])
	{
		echo "This file is intended to be run from the command-line.";

		exit();
	}

	// Temporary root.
	$rootpath = str_replace("\\", "/", dirname(__FILE__));
	require_once $rootpath . "/support/jb64.php";

	$tests = json_decode(file_get_contents($rootpath . "/tests.txt"), true);

	foreach ($tests as $num => $test)
	{
		$x = $num + 1;

		echo "Test #" . $x . ":  " . $test["comment"] . "\n";

		if (isset($test["check_records"]))  $fp = fopen($rootpath . "/out.jb64", "wb");

		// Test the encoder.
		$jb64 = new JB64Encode();
		if (isset($test["headers"]))
		{
			$result = $jb64->EncodeHeaders($test["headers"]);
			if ($result["success"] === $test["header_result"])
			{
				echo "[PASS] EncodeHeaders() returned expected result.\n";
			}
			else
			{
				echo "[FAIL] EncodeHeaders() returned unexpected result.\n";
			}

			if ($result["success"] && isset($test["check_records"]))  fwrite($fp, $result["line"]);
		}
		if (isset($test["data"]))
		{
			foreach ($test["data"] as $num2 => $data)
			{
				$x2 = $num2 + 1;

				$result = $jb64->EncodeRecord($data);
				if ($result["success"] === $test["data_results"][$num2])
				{
					echo "[PASS] EncodeRecord() for record #" . $x2 . " returned expected result.\n";
				}
				else
				{
					echo "[FAIL] EncodeRecord() for record #" . $x2 . " returned unexpected result.\n";
				}

				if ($result["success"] && isset($test["check_records"]))  fwrite($fp, $result["line"]);
			}
		}

		// Test the decoder.
		if (isset($test["check_records"]))
		{
			fclose($fp);

			$jb64 = new JB64Decode();
			$fp = fopen($rootpath . "/out.jb64", "rb");
			$line = fgets($fp);
			$result = $jb64->DecodeHeaders($line);
			if ($result["success"])
			{
				echo "[PASS] DecodeHeaders() succeeded.\n";
				if (serialize($result["headers"]) === serialize($test["headers"]))
				{
					echo "[PASS] DecodeHeaders() returned expected result.\n";

					if (isset($test["header_map"]))
					{
						$result = $jb64->SetHeaderMap($test["header_map"]);
						if ($result["success"])
						{
							echo "[PASS] SetHeaderMap() succeeded.\n";
						}
						else
						{
							echo "[FAIL] SetHeaderMap() failed.\n";
						}
					}

					foreach ($test["check_records"] as $record)
					{
						$line = fgets($fp);
						$result = $jb64->DecodeRecord($line);
						if ($result["success"])
						{
							echo "[PASS] DecodeRecord() succeeded.\n";

							if (serialize($result["ord_data"]) === serialize($record))
							{
								echo "[PASS] DecodeRecord() returned matching record data.\n";
							}
							else
							{
								echo "[FAIL] DecodeRecord() returned record data that does not match.\n";
							}
						}
						else
						{
							echo "[PASS] DecodeRecord() failed.\n";
						}
					}

					$line = fgets($fp);
					if ($line == "")
					{
						echo "[PASS] File contained the correct amount of data.\n";
					}
					else
					{
						echo "[FAIL] File contained more data than expected.\n";
					}
				}
				else
				{
					echo "[FAIL] DecodeHeaders() returned unexpected result.\n";
				}
			}
			else
			{
				echo "[FAIL] DecodeHeaders() failed.\n";
			}
			fclose($fp);

			@unlink($rootpath . "/out.jb64");
		}

		echo "\n";
	}
?>