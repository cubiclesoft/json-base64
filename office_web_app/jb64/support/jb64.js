// JSON-Base64 reference implementation.  (http://jb64.org/)
// (C) 2014 CubicleSoft.

// One dumb function for old IE.
if (!String.prototype.trim) {
	String.prototype.trim = function() { return this.replace(/^\s+|\s+$/g, ''); };
}

// Some basic functions.
var JB64_Base = {
	IsArray: Array.isArray || function(obj) {
		return (obj instanceof Array);
	},

	Base64Encode: window.btoa ? window.btoa.bind(window) : function(data) {
		data = JB64_Base.ToHex(data);
		data = CryptoJS.enc.Hex.parse(data);
		data = CryptoJS.enc.Base64.stringify(data);

		return data;
	},

	Base64Decode: window.atob ? window.atob.bind(window) : function(data) {
		data = CryptoJS.enc.Base64.parse(data);
		data = CryptoJS.enc.Hex.stringify(data);
		data = JB64_Base.FromHex(data);

		return data;
	},

	ToHex : function(data) {
		data = data.toString();

		var result = '';
		for (var x = 0; x < data.length; x++)  result += data.charCodeAt(x).toString(16);

		return result;
	},

	FromHex : function(data) {
		data = data.toString();

		var result = '';
		for (var x = 0; x < data.length; x += 2)  result += String.fromCharCode(parseInt(data.slice(x, x + 2), 16));

		return result;
	},

	ToBoolean : function(data) {
		if (data === null)  data = false;
		else if (typeof data === 'number')  data = (!isNaN(data) && data != 0);
		else if (typeof data === 'string')
		{
			data = parseInt(data, 10);
			data = (!isNaN(data) && data != 0);
		}

		if (typeof data !== 'boolean')   data = false;

		return data;
	},

	ToInteger : function(data) {
		if (data === null)  data = 0;
		else if (typeof data === 'boolean')  data = (data ? 1 : 0);
		else if (typeof data === 'number')  data = (isNaN(data) ? 0 : data >> 0);
		else if (typeof data === 'string')
		{
			data = parseInt(data, 10);
			if (isNaN(data))  data = 0;
		}

		if (typeof data !== 'number')   data = 0;

		return data;
	},

	ToNumber : function(data) {
		if (data === null)  data = 0;
		else if (typeof data === 'boolean')  data = (data ? 1 : 0);
		else if (typeof data === 'string')
		{
			data = parseFloat(data);
			if (isNaN(data))  data = 0;
		}

		if (typeof data !== 'number')   data = 0;

		return data;
	},

	ToString : function(data) {
		if (data === null)  data = "";
		if (typeof data === 'boolean')  data = (data ? 1 : 0);

		return data.toString();
	},

	IntZeroPad : function(str, num) {
		str = parseInt(str, 10);
		if (isNaN(str))  str = 0;
		str = str.toString();
		while (str.length < num)  str = "0" + str;

		return str;
	},

	FixDate : function(ts) {
		ts = JB64_Base.ToString(ts).replace(/[^0-9]/g, " ").replace(/\s+/g, " ").split(" ");
		while (ts.length < 6)  ts[ts.length] = "0";

		var year = JB64_Base.IntZeroPad(ts[0], 4);
		var month = JB64_Base.IntZeroPad(ts[1], 2);
		var day = JB64_Base.IntZeroPad(ts[2], 2);
		var hour = JB64_Base.IntZeroPad(ts[3], 2);
		var minute = JB64_Base.IntZeroPad(ts[4], 2);
		var sec = JB64_Base.IntZeroPad(ts[5], 2);

		return year + "-" + month + "-" + day + " " + hour + ":" + minute + ":" + sec;
	},

	FixTime : function(ts) {
		ts = JB64_Base.ToString(ts).replace(/[^0-9]/g, " ").replace(/\s+/g, " ").split(" ");
		while (ts.length < 3)  ts[ts.length] = "0";

		var x = (ts.length == 6 ? 3 : 0);
		var hour = JB64_Base.IntZeroPad(ts[x], 2);
		var minute = JB64_Base.IntZeroPad(ts[x + 1], 2);
		var sec = JB64_Base.IntZeroPad(ts[x + 2], 2);

		return hour + ":" + minute + ":" + sec;
	}
};

function JB64Encode() {
	var that = this;
	var headers = null;

	function EncodeLine(data) {
		data = JSON.stringify(data);
		data = JB64_Base.Base64Encode(data).replace("+", "-").replace("/", "_").replace("=", "");
		if (typeof CryptoJS.MD5 === 'function')  data += "." + CryptoJS.MD5(data);
		data += "\r\n";

		return data;
	}

	that.EncodeHeaders = function(newheaders) {
		headers = null;

		if (!JB64_Base.IsArray(newheaders) || !newheaders.length)  return { success: false, error: "JSON-Base64 headers must be an array with values." };

		var types = {
			'boolean' : true,
			'integer' : true,
			'number' : true,
			'date' : true,
			'time' : true,
			'string' : true,
			'binary' : true
		};

		for (var x = 0; x < newheaders.length; x++)
		{
			var col = newheaders[x];

			if (!JB64_Base.IsArray(col) || col.length != 2 || typeof col[0] !== 'string' || typeof col[1] !== 'string')  return { success: false, error: "JSON-Base64 header " + x + " must be an array with two string entries." };

			if (col[1].slice(0, 7) != "custom:" && typeof types[col[1]] === 'undefined')  return { success: false, error: "JSON-Base64 header " + x + " has an invalid type of '" + col[1] + "'." };
		}

		headers = newheaders;

		return { success: true, line: EncodeLine(headers) };
	}

	that.EncodeRecord = function(data) {
		if (headers === null)  return { success: false, error: "JSON-Base64 headers must be encoded before record data can be encoded." };
		if (data === null)  return { success: false, error: "Record data must be an array with values." };
		if (typeof data === 'object' && !JB64_Base.IsArray(data))
		{
			var data2 = [];
			for (var x in data)  data2[data2.length] = data[x];
			data = data2;
		}
		if (!JB64_Base.IsArray(data) || !data.length)  return { success: false, error: "Record data must be an array with values." };
		if (data.length !== headers.length)  return { success: false, error: "Record data must be an array with the same number of columns as the headers." };

		// Normalize the input.
		for (var x = 0; x < headers.length; x++)
		{
			if (data[x] !== null)
			{
				switch (headers[x][1])
				{
					case "boolean":  data[x] = JB64_Base.ToBoolean(data[x]);  break;
					case "integer":  data[x] = JB64_Base.ToInteger(data[x]);  break;
					case "number":  data[x] = JB64_Base.ToNumber(data[x]);  break;
					case "date":  data[x] = JB64_Base.FixDate(data[x]);  break;
					case "time":  data[x] = JB64_Base.FixTime(data[x]);  break;
					case "string":  data[x] = JB64_Base.ToString(data[x]);  break;
					case "binary":
					default:  data[x] = JB64_Base.Base64Encode(JB64_Base.ToString(data[x])).replace("+", "-").replace("/", "_").replace("=", "");  break;
				}
			}
		}

		return { success: true, line: EncodeLine(data) };
	}
}

function JB64Decode() {
	var that = this;
	var headers = null;
	var map = null;

	function DecodeLine(data) {
		data = data.trim();

		if (data.length > 33 && data.charAt(data.length - 33) == '.')
		{
			var tempmd5 = data.slice(-32);
			data = data.slice(0, -33);

			if (typeof CryptoJS.MD5 === 'function' && tempmd5 !== CryptoJS.MD5(data).toString())  return false;
		}

		while (data.length % 4 != 0)  data += '=';
		data = JB64_Base.Base64Decode(data.replace("-", "+").replace("_", "/"));
		data = JSON.parse(data);

		return data;
	}

	that.DecodeHeaders = function(line) {
		headers = null;
		map = null;

		var newheaders = DecodeLine(line);
		if (newheaders === false)  return { success: false, error: "Unable to decode JSON-Base64 headers." };

		return that.SetHeaders(newheaders);
	}

	that.SetHeaders = function(newheaders) {
		headers = null;
		map = null;

		if (!JB64_Base.IsArray(newheaders) || !newheaders.length)  return { success: false, error: "JSON-Base64 headers must be an array with values." };

		var types = {
			'boolean' : true,
			'integer' : true,
			'number' : true,
			'date' : true,
			'time' : true,
			'string' : true,
			'binary' : true
		};

		for (var x = 0; x < newheaders.length; x++)
		{
			var col = newheaders[x];

			if (!JB64_Base.IsArray(col) || col.length != 2 || typeof col[0] !== 'string' || typeof col[1] !== 'string')  return { success: false, error: "JSON-Base64 header " + x + " must be an array with two string entries." };

			if (col[1].slice(0, 7) != "custom:" && typeof types[col[1]] === 'undefined')  return { success: false, error: "JSON-Base64 header " + x + " has an invalid type of '" + col[1] + "'." };
		}

		headers = newheaders;

		return { success: true, headers: headers };
	}

	that.SetHeaderMap = function(newmap) {
		if (headers === null)  return { success: false, error: "JSON-Base64 headers must be decoded before header mapping can be applied." };
		if (newmap === null)  return { success: false, error: "JSON-Base64 header map must be an array with values." };
		if (!JB64_Base.IsArray(newmap) || !newmap.length)  return { success: false, error: "JSON-Base64 header map is not an array with values." };
		if (newmap.length !== headers.length)  return { success: false, error: "JSON-Base64 map is not an array with the same number of columns as the headers." };

		var types = {
			'boolean' : true,
			'integer' : true,
			'number' : true,
			'date' : true,
			'time' : true,
			'string' : true,
			'binary' : true
		};

		for (var x = 0; x < newmap.length; x++)
		{
			var col = newmap[x];

			if (!JB64_Base.IsArray(col) || col.length != 3 || typeof col[0] !== 'string' || typeof col[1] !== 'string' || typeof col[2] !== 'boolean')  return { success: false, error: "JSON-Base64 map column " + x + " must be an array with two string entries." };

			if (col[1].slice(0, 7) != "custom:" && typeof types[col[1]] === 'undefined')  return { success: false, error: "JSON-Base64 map column " + x + " has an invalid type of '" + col[1] + "'." };

			if (col[1].slice(0, 7) == "custom:" && headers[x][1] !== col[1])  return { success: false, error: "JSON-Base64 map column " + x + " has an invalid conversion type of '" + col[1] + "' for header type '" + headers[x][1] + "'." };
		}

		map = newmap;

		return { success: true, map: map };
	}

	that.DecodeRecord = function(line) {
		if (headers === null)  return { success: false, error: "JSON-Base64 headers must be decoded before record data can be decoded." };

		var data  = DecodeLine(line);
		if (data === false)  return { success: false, error: "Unable to decode JSON-Base64 record data." };
		if (typeof data === 'object' && !JB64_Base.IsArray(data))
		{
			var data2 = [];
			for (var x in data)  data2[data2.length] = data[x];
			data = data2;
		}
		if (!JB64_Base.IsArray(data) || !data.length)  return { success: false, error: "Record data must be an array with values." };
		if (data.length !== headers.length)  return { success: false, error: "Record data must be an array with the same number of columns as the headers." };

		// Normalize the input.
		for (var x = 0; x < headers.length; x++)
		{
			if (data[x] !== null)
			{
				switch (headers[x][1])
				{
					case "boolean":  data[x] = JB64_Base.ToBoolean(data[x]);  break;
					case "integer":  data[x] = JB64_Base.ToInteger(data[x]);  break;
					case "number":  data[x] = JB64_Base.ToNumber(data[x]);  break;
					case "date":  data[x] = JB64_Base.FixDate(data[x]);  break;
					case "time":  data[x] = JB64_Base.FixTime(data[x]);  break;
					case "string":  data[x] = JB64_Base.ToString(data[x]);  break;
					case "binary":
					default:
						data[x] = JB64_Base.ToString(data[x]).replace("-", "+").replace("_", "/");
						while (data[x].length % 4 != 0)  data[x] += '=';
						data[x] = JB64_Base.Base64Decode(data[x]);

						break;
				}
			}
		}

		// Map the input.
		var data2 = {};
		if (map === null)
		{
			for (var x = 0; x < headers.length; x++)  data2[headers[x][0]] = data[x];
		}
		else
		{
			for (var x = 0; x < map.length; x++)
			{
				if (data[x] === null && !map[x][2])
				{
					switch (map[x][1])
					{
						case "boolean":  data[x] = false;  break;
						case "integer":  data[x] = 0;  break;
						case "number":  data[x] = 0.0;  break;
						case "date":  data[x] = "0000-00-00 00:00:00";  break;
						case "time":  data[x] = "00:00:00";  break;
						default:  data[x] = "";  break;
					}
				}

				if (data[x] !== null)
				{
					if (typeof data[x] === 'boolean' && map[x][1] != "boolean")  data[x] = (data[x] ? 1 : 0);

					switch (map[x][1])
					{
						case "boolean":  data[x] = JB64_Base.ToBoolean(data[x]);  break;
						case "integer":  data[x] = JB64_Base.ToInteger(data[x]);  break;
						case "number":  data[x] = JB64_Base.ToNumber(data[x]);  break;
						case "date":  data[x] = JB64_Base.FixDate(data[x]);  break;
						case "time":  data[x] = JB64_Base.FixTime(data[x]);  break;
						default:  data[x] = JB64_Base.ToString(data[x]);  break;
					}
				}

				data2[map[x][0]] = data[x];
			}
		}

		return { success: true, ord_data: data, assoc_data: data2 };
	}
}
