// JSON-Base64 task pane app for Microsoft Excel Online and Microsoft Excel 2013 and later.  (http://jb64.org/)
// (C) 2014 CubicleSoft.

var jb64_app_filesaver = false;

Office.initialize = function (reason) {
	$(function () {
		$('#import_file').change(function(e) {
			var filename = $(this).val();
			var y = filename.match(/([^\/\\]+)$/);
			if (y)  filename = y[1];

			if (filename !== "")
			{
				y = filename.match(/([^\.]+)$/);
				var fileext = (y && y[1] !== filename ? y[1] : '');

				if (fileext !== "jb64")  $('#contentwrap').html('<p><font style="color: #880000;">[ERROR]</font> Import failed.  The file "' + filename + '" does not have a .jb64 file extension.</p>');
				else
				{
					var read = new FileReader();

					read.onload = function(e) {
						ProcessImport(read.result);
					}

					read.readAsText(this.files[0]);
				}
			}
		});

		// Find a viable export mechanism.
		try
		{
			jb64_app_filesaver = !!new Blob();
		}
		catch (e)
		{
		}

		$('#menuitems').show();
		$('#contentwrap').html('<p>Ready.</p>' + (jb64_app_filesaver ? '' : '<p>Due to insufficient web browser support for HTML 5, you will need to manually load and save data.</p><p><a href="http://jb64.org/excel_app/" target="_blank">Learn more about HTML 5 requirements for this app</a></p>'));
	});
}

var jb64_app_importheaders = null;

function ProcessImport(data) {
	var lines = data.split('\n');

	if (!lines.length)
	{
		$('#contentwrap').html('<p><font style="color: #880000;">[ERROR]</font> Import failed.  The file does not contain any data or the file contents failed to be read into memory.</p>');

		return;
	}

	// Process the headers.
	var line = lines.shift();
	var jb64 = new JB64Decode();
	var result = jb64.DecodeHeaders(line);
	if (!result["success"])
	{
		$('#contentwrap').html('<p><font style="color: #880000;">[ERROR]</font> Unable to decode JSON-Base64 headers.</p><p>' + result.error + '</p>');

		return false;
	}

	jb64_app_importheaders = result.headers;

	data = [[]];
	var x = jb64_app_importheaders.length;
	for (var x2 = 0; x2 < x; x2++)  data[0][x2] = jb64_app_importheaders[x2][0];

	// Process the records.
	var html = '';
	while (lines.length)
	{
		line = lines.shift().trim();
		if (line !== "")
		{
			result = jb64.DecodeRecord(line);
			if (!result["success"])  html += '<p><font style="color: #880000;">[ERROR]</font> Unable to decode JSON-Base64 record.</p><p>' + result.error + '</p>';
			else
			{
				for (var x2 = 0; x2 < x; x2++)
				{
					if (jb64_app_importheaders[x2][1] == "date")  result.ord_data[x2] = UTCToLocalDate(result.ord_data[x2]);
					else if (jb64_app_importheaders[x2][1] == "time")  result.ord_data[x2] = UTCToLocalTime(result.ord_data[x2]);
				}

				data[data.length] = result.ord_data;
			}
		}
	}

	// Load the data into Excel.
	Office.context.document.setSelectedDataAsync(data, { coercionType: 'matrix' }, function(result) {
		if (result.status === "succeeded")  html += '<p><font style="color: #008800;">[SUCCESS]</font> Successfully imported data.</p>';
		else  html += '<p><font style="color: #880000;">[ERROR]</font> Unable to write information.  Make sure cells are empty and exactly one cell is selected where the imported data will be inserted.<br /><br />[' + result.error.code + '] ' + result.error.name + ' - ' + result.error.message + '</p>';

		$('#contentwrap').html(html);
	});
}

function BeginImport() {
	if (jb64_app_filesaver)
	{
		// Reset the selected file so the onchange fires.
		$('#import_file').val(null);

		// Now set the directions.
		$('#contentwrap').html("<p>Select a JSON-Base64 (*.jb64) file to import.</p>");

		// Click the hidden file selection button to open the file dialog.
		$('#import_file').click();
	}
	else
	{
		// Add the textarea and buttons.
		var html = '<p><b>Paste the contents of a .jb64 file:</b></p>';
		html += '<p><textarea id="import_data"></textarea></p>';
		html += '<p class="buttonwrap"><input type="submit" value="Import" onclick="return RunManualImport();" /> <input type="submit" value="Cancel" onclick="return CancelImport();" /></p>';

		$('#contentwrap').html(html);
	}

	return false;
}

function RunManualImport() {
	ProcessImport($('#import_data').val());

	return false;
}

function CancelImport() {
	$('#contentwrap').html('<p>Ready.</p>');

	return false;
}

var jb64_app_exportdata = null;

function HTMLEncode(html) {
	return document.createElement('a').appendChild(document.createTextNode(html)).parentNode.innerHTML.replace(/"/g, '&quot;');
}

function UTCToLocalDate(ts) {
	if (ts === '0000-00-00 00:00:00')  return ts;

	ts = new Date(Date.parse(ts.replace(/-/g, '/') + ' UTC'));

	return JB64_Base.FixDate(ts.getFullYear() + '-' + (ts.getMonth() + 1) + '-' + ts.getDate() + ' ' + ts.getHours() + ':' + ts.getMinutes() + ':' + ts.getSeconds());
}

function UTCToLocalTime(ts) {
	return UTCToLocalDate('1970-01-02 ' + ts).slice(-8);
}

function ExcelDateToUTC(ts) {
	// Local time might be ahead of UTC, so add one extra day to times.
	if (ts < 1.0)  ts += 25570.0;

	// Cut off dates older than 1970-01-01.  Will need to be rewritten if someone needs older dates.
	if (ts <= 25569.0)  return "0000-00-00 00:00:00";

	ts = new Date(((ts - 25569) * 86400 + ((new Date().getTimezoneOffset()) * 60)) * 1000);

	return JB64_Base.FixDate(ts.getUTCFullYear() + '-' + (ts.getUTCMonth() + 1) + '-' + ts.getUTCDate() + ' ' + ts.getUTCHours() + ':' + ts.getUTCMinutes() + ':' + ts.getUTCSeconds());
}

function ExcelTimeToUTC(ts) {
	return ExcelDateToUTC(ts).slice(-8);
}

function ExcelDateToLocal(ts) {
	if (ts < 1.0)  ts += 25569.0;

	// Cut off dates older than 1970-01-01.  Will need to be rewritten if someone needs older dates.
	if (ts <= 25569.0)  return "0000-00-00 00:00:00";

	ts = new Date(((ts - 25569) * 86400) * 1000);

	return JB64_Base.FixDate(ts.getUTCFullYear() + '-' + (ts.getUTCMonth() + 1) + '-' + ts.getUTCDate() + ' ' + ts.getUTCHours() + ':' + ts.getUTCMinutes() + ':' + ts.getUTCSeconds());
}

function ExcelTimeToLocal(ts) {
	return ExcelDateToLocal(ts).slice(-8);
}

function UpdateExportNotes() {
	$('#contentwrap p.datetimenote').hide();

	$('#contentwrap select.export_cols').each(function() {
		var tempval = $(this).val();
		if (tempval === "date" || tempval === "date-alt" || tempval === "time" || tempval === "time-alt")  $('#contentwrap p.datetimenote').show();
	});
}

function BeginExport() {
	$('#contentwrap').html('<p>Reading data, please wait...</p>');

	jb64_app_exportdata = null;

	Office.context.document.getSelectedDataAsync("matrix", {
		valueFormat: Office.ValueFormat.Unformatted,
		filterType: Office.FilterType.All
	}, function (result) {
		if (result.status === "succeeded")
		{
			if (result.value.length < 2)  $('#contentwrap').html('<p><font style="color: #880000;">[ERROR]</font> At least two rows must be selected to export to JSON-Base64.  Select an export region and try again.</p>');
			else
			{
				// Analyze the matrix for consistency.
				var x = result.value[0].length;
				var y = 0;
				for (y = 0; y < result.value.length && result.value[y].length === x; y++)
				{
				}

				if (y < result.value.length)  $('#contentwrap').html('<p><font style="color: #880000;">[ERROR]</font> Unable to process information.  Select an export region with the same number of columns for each row and try again.</p>');
				else
				{
					jb64_app_exportdata = result.value;

					var types = [
						"boolean",
						"integer",
						"number",
						"date",
						"date-alt",
						"time",
						"time-alt",
						"string",
					];

					// Generate the header array based on the first two rows of data.
					var html = '<p><b>Set column information:</b></p>';
					for (var x2 = 0; x2 < x; x2++)
					{
						html += '<p>' + HTMLEncode(jb64_app_exportdata[0][x2]) + "<br />\n";
						html += '<select id="export_col_' + x2 + '" name="export_col_' + x2 + '" class="export_cols">\n';
						var temptype = typeof jb64_app_exportdata[1][x2];
						var data = jb64_app_exportdata[1][x2];
						for (var x3 = 0; x3 < types.length; x3++)
						{
							var currtype = types[x3];
							var tempval = '';

							html += '<option value="' + currtype + '"' + (currtype === temptype ? ' selected' : '') + '>' + currtype + ' (';
							switch (currtype)
							{
								case "boolean":  tempval = JB64_Base.ToBoolean(data).toString();  break;
								case "integer":  tempval = JB64_Base.ToInteger(data).toString();  break;
								case "number":  tempval = JB64_Base.ToNumber(data).toString();  break;
								case "date":  tempval = ExcelDateToUTC(data);  break;
								case "date-alt":  tempval = ExcelDateToLocal(data);  break;
								case "time":  tempval = ExcelTimeToUTC(data);  break;
								case "time-alt":  tempval = ExcelTimeToLocal(data);  break;
								case "string":  tempval = JB64_Base.ToString(data);  break;
							}
							html += HTMLEncode(tempval.slice(0, 50) + (tempval.length < 50 ? '' : '...'));
							html += ')</option>\n';
						}
						html += '</select>\n';
						html += '</p>\n';
					}

					html += '<p class="note datetimenote">Applications that import JSON-Base64 expect exported \'date\' and \'time\' columns to be in UTC.</p>';

					html += '<p class="buttonwrap"><input type="submit" value="Export" onclick="return RunExport();" /> <input type="submit" value="Cancel" onclick="return CancelExport();" /></p>';

					html += '<p id="manualexportwrap"></p>';

					html += '<p id="exportmessage"></p>';

					$('#contentwrap').html(html);

					$('#contentwrap select.export_cols').change(UpdateExportNotes).keyup(UpdateExportNotes);
				}
			}
		}
		else
		{
			$('#contentwrap').html('<p><font style="color: #880000;">[ERROR]</font> Unable to retrieve information.  Select an export region and try again.</p><p>[' + result.error.code + '] ' + result.error.name + ' - ' + result.error.message + '</p>');
		}
	});

	return false;
}

function RunExport() {
	// Generate the headers.
	var headers = [], map = [];
	var x = jb64_app_exportdata[0].length;
	var jb64 = new JB64Encode();
	var output = '';
	for (var x2 = 0; x2 < x; x2++)
	{
		var tempval = $('#export_col_' + x2).val();
		map[x2] = tempval;
		tempval = tempval.replace("-alt", "");
		headers[x2] = [jb64_app_exportdata[0][x2], tempval];
	}

	var result = jb64.EncodeHeaders(headers);
	if (!result["success"])
	{
		$('#exportmessage').html('<font style="color: #880000;">[ERROR]</font> Unable to encode JSON-Base64 headers.<br /><br />' + result.error);

		return false;
	}

	output += result.line;

	// Generate the records.
	var success = true;
	var record = [];
	for (var y = 1; y < jb64_app_exportdata.length; y++)
	{
		for (var x2 = 0; x2 < x; x2++)
		{
			var tempval = jb64_app_exportdata[y][x2];

			if (map[x2] === "date")  tempval = ExcelDateToUTC(tempval);
			else if (map[x2] === "date-alt")  tempval = ExcelDateToLocal(tempval);
			else if (map[x2] === "time")  tempval = ExcelTimeToUTC(tempval);
			else if (map[x2] === "time-alt")  tempval = ExcelTimeToLocal(tempval);

			record[x2] = tempval;
		}

		result = jb64.EncodeRecord(record);
		if (result["success"])  output += result.line;
		else
		{
			$('#exportmessage').html('<font style="color: #880000;">[ERROR]</font> Unable to encode JSON-Base64 record.<br /><br />' + result.error);
			success = false;
		}
	}

	// Calculate the filename.
	var filename = Office.context.document.url;
	if (filename === null)  filename = "Untitled";

	var y = filename.match(/([^\/\\]+)$/);
	if (y)  filename = y[1];

	if (filename === "")  filename = "Untitled";

	y = filename.match(/([^\.]+)$/);
	var fileext = (y && y[1] !== filename ? y[1] : '');

	filename = (fileext === '' ? filename + '.jb64' : filename.slice(0, -(fileext.length)) + 'jb64');

	// Initiate download.
	if (jb64_app_filesaver)
	{
		var blob = new Blob([output], {type: "application/jb64; charset=utf-8"});
		saveAs(blob, filename);
	}
	else
	{
		var html = '<p><b>Copy the contents to a .jb64 file:</b></p>';
		html += '<p><textarea id="export_data"></textarea></p>';
		$('#manualexportwrap').html(html);
		$('#export_data').val(output).focus(function() {
			var $this = $(this);

			$this.select();

			window.setTimeout(function() {
				$this.select();
			}, 1);

			function mouseUpHandler() {
				$this.off("mouseup", mouseUpHandler);
				return false;
			}

			$this.mouseup(mouseUpHandler);
		});
	}

	if (success)  $('#exportmessage').html('<font style="color: #008800;">[SUCCESS]</font> The records were exported successfully.');

	return false;
}

function CancelExport() {
	$('#contentwrap').html('<p>Ready.</p>');

	return false;
}
