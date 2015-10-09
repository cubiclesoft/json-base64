<?php
	// JSON-Base64 reference implementation.  (http://jb64.org/)
	// Public Domain.

	class JB64_Base
	{
		protected static function FixDate($ts)
		{
			$ts = array_pad(explode(" ", trim(preg_replace('/\s+/', " ", preg_replace('/[^0-9]/', " ", (string)$ts)))), 6, 0);

			$year = str_pad((string)(int)$ts[0], 4, "0", STR_PAD_LEFT);
			$month = str_pad((string)(int)$ts[1], 2, "0", STR_PAD_LEFT);
			$day = str_pad((string)(int)$ts[2], 2, "0", STR_PAD_LEFT);
			$hour = str_pad((string)(int)$ts[3], 2, "0", STR_PAD_LEFT);
			$min = str_pad((string)(int)$ts[4], 2, "0", STR_PAD_LEFT);
			$sec = str_pad((string)(int)$ts[5], 2, "0", STR_PAD_LEFT);

			return $year . "-" . $month . "-" . $day . " " . $hour . ":" . $min . ":" . $sec;
		}

		protected static function FixTime($ts)
		{
			$ts = array_pad(explode(" ", trim(preg_replace('/\s+/', " ", preg_replace('/[^0-9]/', " ", (string)$ts)))), 3, 0);

			$x = (count($ts) == 6 ? 3 : 0);
			$hour = str_pad((string)(int)$ts[$x], 2, "0", STR_PAD_LEFT);
			$min = str_pad((string)(int)$ts[$x + 1], 2, "0", STR_PAD_LEFT);
			$sec = str_pad((string)(int)$ts[$x + 2], 2, "0", STR_PAD_LEFT);

			return $hour . ":" . $min . ":" . $sec;
		}

		protected static function JB64_Translate()
		{
			$args = func_get_args();
			if (!count($args))  return "";

			return call_user_func_array((defined("CS_TRANSLATE_FUNC") && function_exists(CS_TRANSLATE_FUNC) ? CS_TRANSLATE_FUNC : "sprintf"), $args);
		}
	}

	class JB64Encode extends JB64_Base
	{
		private $headers;

		public function __construct()
		{
			$this->headers = false;
		}

		public function EncodeHeaders($headers)
		{
			$this->headers = false;

			if (!is_array($headers) || !count($headers))  return array("success" => false, "error" => JB64Encode::JB64_Translate("JSON-Base64 headers must be an array with values."), "errorcode" => "not_array");

			$types = array(
				"boolean" => true,
				"integer" => true,
				"number" => true,
				"date" => true,
				"time" => true,
				"string" => true,
				"binary" => true,
			);

			foreach ($headers as $num => $col)
			{
				if (!is_array($col) || count($col) != 2 || !is_string($col[0]) || !is_string($col[1]))  return array("success" => false, "error" => JB64Encode::JB64_Translate("JSON-Base64 header %d must be an array with two string entries.", $num), "errorcode" => "invalid_header");

				if (substr($col[1], 0, 7) != "custom:" && !isset($types[$col[1]]))  return array("success" => false, "error" => JB64Encode::JB64_Translate("JSON-Base64 header %d has an invalid type of '%s'.", $num, $col[1]), "errorcode" => "invalid_header_type");
			}

			$this->headers = array_values($headers);

			return array("success" => true, "line" => $this->EncodeLine($this->headers));
		}

		public function EncodeRecord($data)
		{
			if ($this->headers === false)  return array("success" => false, "error" => JB64Encode::JB64_Translate("JSON-Base64 headers must be encoded before record data can be encoded."), "errorcode" => "headers_not_encoded");
			if (is_object($data))  $data = get_object_vars($data);
			if (!is_array($data) || !count($data))  return array("success" => false, "error" => JB64Encode::JB64_Translate("Record data must be an array with values."), "errorcode" => "not_array");
			if (count($data) !== count($this->headers))  return array("success" => false, "error" => JB64Encode::JB64_Translate("Record data must be an array with the same number of columns as the headers."), "errorcode" => "mismatched_record_size");

			// Normalize the input.
			$data = array_values($data);
			foreach ($this->headers as $num => $col)
			{
				if ($data[$num] !== null)
				{
					switch ($col[1])
					{
						case "boolean":  $data[$num] = (bool)(int)$data[$num];  break;
						case "integer":  $data[$num] = (int)$data[$num];  break;
						case "number":  $data[$num] = (double)$data[$num];  break;
						case "date":  $data[$num] = JB64Encode::FixDate($data[$num]);  break;
						case "time":  $data[$num] = JB64Encode::FixTime($data[$num]);  break;
						case "string":  $data[$num] = (string)$data[$num];  break;
						case "binary":
						default:  $data[$num] = str_replace(array("+", "/", "="), array("-", "_", ""), base64_encode((string)$data[$num]));  break;
					}
				}
			}

			return array("success" => true, "line" => $this->EncodeLine($data));
		}

		protected function EncodeLine($data)
		{
			$data = str_replace(array("+", "/", "="), array("-", "_", ""), base64_encode(json_encode($data)));
			$data .= "." . md5($data) . "\r\n";

			return $data;
		}
	}

	class JB64Decode extends JB64_Base
	{
		private $headers, $map;

		public function __construct()
		{
			$this->headers = false;
			$this->map = false;
		}

		public function DecodeHeaders($line)
		{
			$this->headers = false;
			$this->map = false;

			$headers = $this->DecodeLine($line);
			if ($headers === false)  return array("success" => false, "error" => JB64Decode::JB64_Translate("Unable to decode JSON-Base64 headers."), "errorcode" => "headers_decode_failed");

			return $this->SetHeaders($headers);
		}

		public function SetHeaders($headers)
		{
			$this->headers = false;
			$this->map = false;

			if (!is_array($headers) || !count($headers))  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 headers is not an array with values."), "errorcode" => "not_array");

			$types = array(
				"boolean" => true,
				"integer" => true,
				"number" => true,
				"date" => true,
				"time" => true,
				"string" => true,
				"binary" => true,
			);

			foreach ($headers as $num => $col)
			{
				if (!is_array($col) || count($col) != 2 || !is_string($col[0]) || !is_string($col[1]))  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 header %d is not an array with two string entries.", $num), "errorcode" => "invalid_header");

				if (substr($col[1], 0, 7) != "custom:" && !isset($types[$col[1]]))  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 header %d has an invalid type of '%s'.", $num, $col[1]), "errorcode" => "invalid_header_type");
			}

			$this->headers = array_values($headers);

			return array("success" => true, "headers" => $this->headers);
		}

		public function SetHeaderMap($map)
		{
			if ($this->headers === false)  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 headers must be decoded before header mapping can be applied."), "errorcode" => "headers_not_decoded");

			if (!is_array($map) || !count($map))  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 header map is not an array with values."), "errorcode" => "not_array");
			if (count($map) !== count($this->headers))  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 map is not an array with the same number of columns as the headers."), "errorcode" => "mismatched_header_map_size");

			$types = array(
				"boolean" => true,
				"integer" => true,
				"number" => true,
				"date" => true,
				"time" => true,
				"string" => true,
				"binary" => true,
			);

			foreach ($map as $num => $col)
			{
				if (!is_array($col) || count($col) != 3 || !is_string($col[0]) || !is_string($col[1]) || !is_bool($col[2]))  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 map column %d is not an array with two string entries and one boolean entry.", $num), "errorcode" => "invalid_header_map");

				if (substr($col[1], 0, 7) != "custom:" && !isset($types[$col[1]]))  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 map column %d has an invalid type of '%s'.", $num, $col[1]), "errorcode" => "invalid_header_map_type");

				if (substr($col[1], 0, 7) == "custom:" && $this->headers[$num][1] != $col[1])  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 map column %d has an invalid conversion type of '%s' for header type '%s'.", $num, $col[1], $this->headers[$num][1]), "errorcode" => "invalid_header_map_conversion");
			}

			$this->map = array_values($map);

			return array("success" => true, "map" => $this->map);
		}

		public function DecodeRecord($line)
		{
			if ($this->headers === false)  return array("success" => false, "error" => JB64Decode::JB64_Translate("JSON-Base64 headers must be decoded before record data can be decoded."), "errorcode" => "headers_not_decoded");

			$data = $this->DecodeLine($line);
			if ($data === false)  return array("success" => false, "error" => JB64Decode::JB64_Translate("Unable to decode JSON-Base64 record data."), "errorcode" => "record_decode_failed");

			if (is_object($data))  $data = get_object_vars($data);
			if (!is_array($data) || !count($data))  return array("success" => false, "error" => JB64Decode::JB64_Translate("Record data is not an array with values."), "errorcode" => "not_array");
			if (count($data) !== count($this->headers))  return array("success" => false, "error" => JB64Decode::JB64_Translate("Record data is not an array with the same number of columns as the headers."), "errorcode" => "mismatched_record_size");

			// Normalize the input.
			$data = array_values($data);
			foreach ($this->headers as $num => $col)
			{
				if ($data[$num] !== null)
				{
					switch ($col[1])
					{
						case "boolean":  $data[$num] = (bool)(int)$data[$num];  break;
						case "integer":  $data[$num] = (int)$data[$num];  break;
						case "number":  $data[$num] = (double)$data[$num];  break;
						case "date":  $data[$num] = JB64Decode::FixDate($data[$num]);  break;
						case "time":  $data[$num] = JB64Decode::FixTime($data[$num]);  break;
						case "string":  $data[$num] = (string)$data[$num];  break;
						case "binary":
						default:  $data[$num] = base64_decode(str_replace(array("-", "_"), array("+", "/"), (string)$data[$num]));  break;
					}
				}
			}

			// Map the input.
			$data2 = array();
			if ($this->map === false)
			{
				foreach ($this->headers as $num => $col)  $data2[$col[0]] = $data[$num];
			}
			else
			{
				foreach ($this->map as $num => $col)
				{
					if ($data[$num] === null && !$col[2])
					{
						switch ($col[1])
						{
							case "boolean":  $data[$num] = false;  break;
							case "integer":  $data[$num] = 0;  break;
							case "number":  $data[$num] = (double)0;  break;
							case "date":  $data[$num] = "0000-00-00 00:00:00";  break;
							case "time":  $data[$num] = "00:00:00";  break;
							default:  $data[$num] = "";  break;
						}
					}

					if ($data[$num] !== null)
					{
						if (is_bool($data[$num]) && $col[1] != "boolean")  $data[$num] = (int)$data[$num];

						switch ($col[1])
						{
							case "boolean":  $data[$num] = (bool)(int)$data[$num];  break;
							case "integer":  $data[$num] = (int)$data[$num];  break;
							case "number":  $data[$num] = (double)$data[$num];  break;
							case "date":  $data[$num] = JB64Decode::FixDate($data[$num]);  break;
							case "time":  $data[$num] = JB64Decode::FixTime($data[$num]);  break;
							default:  $data[$num] = (string)$data[$num];  break;
						}
					}

					$data2[$col[0]] = $data[$num];
				}
			}

			return array("success" => true, "ord_data" => $data, "assoc_data" => $data2);
		}

		protected function DecodeLine($line)
		{
			$line = trim($line);

			if (strlen($line) > 33 && $line{strlen($line) - 33} == ".")
			{
				$md5 = substr($line, -32);
				$line = substr($line, 0, -33);
				if ($md5 !== md5($line))  return false;
			}

			$data = @json_decode(base64_decode(str_replace(array("-", "_"), array("+", "/"), $line)), true);

			return $data;
		}
	}
?>