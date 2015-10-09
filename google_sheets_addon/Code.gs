/**
 * @OnlyCurrentDoc  Limits the script to only accessing the current spreadsheet.
 */

/**
 * Adds a custom menu with items to show the sidebar and dialog.
 *
 * @param {Object} e The event parameter for a simple onOpen trigger.
 */
function onOpen(e) {
  SpreadsheetApp.getUi().createAddonMenu().addItem('Import...', 'importDialog').addItem('Export current sheet...', 'exportDialog').addToUi();
}

/**
 * Runs when the add-on is installed; calls onOpen() to ensure menu creation and
 * any other initializion work is done immediately.
 *
 * @param {Object} e The event parameter for a simple onInstall trigger.
 */
function onInstall(e) {
  onOpen(e);
}

/**
 * The import dialog.
 */
function importDialog() {
  var ui = HtmlService.createTemplateFromFile('ImportDialog').evaluate().setWidth(600).setHeight(300).setSandboxMode(HtmlService.SandboxMode.IFRAME);
  SpreadsheetApp.getUi().showModalDialog(ui, 'Import');
}

/**
 * Perform the actual import.
 */
function jb64Import(data) {
  var currsheet = SpreadsheetApp.getActiveSpreadsheet();
  var newsheet = currsheet.insertSheet();
  var range = newsheet.getRange(1, 1, data.length, data[0].length);
  range.setValues(data);
}

/**
 * The export dialog.
 */
function exportDialog() {
// Select mapping here...then do the export and display the save as dialog.
  var ui = HtmlService.createTemplateFromFile('ExportDialog').evaluate().setWidth(600).setHeight(300).setSandboxMode(HtmlService.SandboxMode.IFRAME);
  SpreadsheetApp.getUi().showModalDialog(ui, 'Export Current Sheet');
}

/**
 * Exports the current sheet's data.
 */
function jb64Export() {
  var currsheet = SpreadsheetApp.getActiveSheet();
  var range = currsheet.getRange(1, 1, currsheet.getMaxRows(), currsheet.getMaxColumns());
  var data = range.getValues();

  // Generate a cleaned up data array.
  var data2 = [];
  if (data.length >= 2)
  {
    var row = [];
    for (var x = 0; x < data[0].length; x++)
    {
      if (typeof data[0][x] === 'string' && data[0][x] !== '')  row.push(data[0][x]);
    }
    data2.push(row);

    for (var y = 1; y < data.length; y++)
    {
      var found = false;
      var row = [];
      for (var x = 0; x < data[y].length; x++)
      {
        if (typeof data[0][x] === 'string' && data[0][x] !== '')
        {
          // Fix dates by converting them to Unix timestamps.
          if (data[y][x] instanceof Date)
          {
            row.push(data[y][x].getTime());
            found = true;
          }
          else
          {
            row.push(data[y][x]);
            if (typeof data[y][x] !== 'string' || data[y][x] !== '')  found = true;
          }
        }
      }
      if (found)  data2.push(row);
    }
  }

  return {sheetname: currsheet.getName(), data: data2};
}
