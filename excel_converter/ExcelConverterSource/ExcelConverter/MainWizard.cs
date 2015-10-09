using JB64;
using Microsoft.Win32;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ExcelConverter
{
    public partial class MainWizard : Form
    {
        private bool AllowNext, UpdatingOptions;
        private List<SetOptionsEntry[]> SetOptionsEntries;
        private int SetOptionsActiveEntry;
        private ReadInterface ReadConvert;
        private WriteInterface WriteConvert;
        private string ConversionResults;

        public MainWizard()
        {
            this.UpdatingOptions = false;

            InitializeComponent();

            // Check for a filename specified on the command-line.
            string[] Args = Environment.GetCommandLineArgs();
            if (Args.Length > 1)
            {
                string SrcFilename = Args[1];
                string SrcFileExt = GetFileExt(SrcFilename);
                if (SrcFileExt == ".jb64" || SrcFileExt == ".xls" || SrcFileExt == ".xlsx")
                {
                    select_source_file.Text = SrcFilename;
                    SelectFiles_UpdateDestFile();
                }
            }
        }

        public static string GetFileExt(string Filename)
        {
            return (Filename.LastIndexOf('.') > -1 ? Filename.Substring(Filename.LastIndexOf('.')) : "").ToLower();
        }

        private void OnClickCancel(object sender, CancelEventArgs e)
        {
            Close();
        }

        private void SelectFiles_OnSourceFileBrowse(object sender, EventArgs e)
        {
            OpenFileDialog Dlg = new OpenFileDialog();
            Dlg.Filter = "Supported file types (*.jb64, *.xls, *.xlsx)|*.jb64;*.xls;*.xlsx";
            Dlg.Title = "Select a file to convert...";
            Dlg.FileName = select_source_file.Text;
            Dlg.FileOk += delegate(object s, CancelEventArgs ev)
            {
                string SrcFilename = Dlg.FileName;
                string SrcFileExt = GetFileExt(SrcFilename);
                if (SrcFileExt != ".jb64" && SrcFileExt != ".xls" && SrcFileExt != ".xlsx")
                {
                    MessageBox.Show("File extension must be .jb64, .xls, or .xslx.", "Error");

                    ev.Cancel = true;
                }
            };
            DialogResult DlgResult = Dlg.ShowDialog();
            if (DlgResult == DialogResult.OK)
            {
                select_source_file.Text = Dlg.FileName;
                SelectFiles_UpdateDestFile();

                return;
            }
        }

        private void SelectFiles_OnSourceFileDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))  e.Effect = DragDropEffects.Copy;
            else  e.Effect = DragDropEffects.None;
        }

        private void SelectFiles_OnSourceFileDragDrop(object sender, DragEventArgs e)
        {
            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (FileList.Length > 0)
            {
                select_source_file.Text = FileList[0];
                SelectFiles_UpdateDestFile();
            }
        }

        private void SelectFiles_OnDestFileBrowse(object sender, EventArgs e)
        {
            string SrcFilename = select_source_file.Text;
            string SrcFileExt = GetFileExt(SrcFilename);
            if (SrcFileExt == ".jb64")
            {
                SaveFileDialog Dlg = new SaveFileDialog();
                Dlg.Title = "Select destination file...";
                string DestFilename = SrcFilename;
                if (DestFilename.LastIndexOf('\\') > -1)
                {
                    Dlg.InitialDirectory = DestFilename.Substring(0, DestFilename.LastIndexOf('\\'));
                    DestFilename = DestFilename.Substring(DestFilename.LastIndexOf('\\') + 1);
                }
                Dlg.Filter = "Excel 2007 and later (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls|JSON-Base64 (*.jb64)|*.jb64";
                Dlg.FileName = DestFilename.Substring(0, DestFilename.Length - 5) + ".xlsx";
                Dlg.FileOk += delegate(object s, CancelEventArgs ev)
                {
                    DestFilename = Dlg.FileName;
                    string DestFileExt = GetFileExt(DestFilename);
                    if (DestFileExt != ".xls" && DestFileExt != ".xlsx" && DestFileExt != ".jb64")
                    {
                        MessageBox.Show("File extension must be .xls, .xslx, or .jb64.", "Error");

                        ev.Cancel = true;
                    }
                };
                DialogResult DlgResult = Dlg.ShowDialog();
                if (DlgResult == DialogResult.OK)
                {
                    select_dest_file.Text = Dlg.FileName;

                    return;
                }
            }
            else if (SrcFileExt == ".xls" || SrcFileExt == ".xlsx")
            {
                SaveFileDialog Dlg = new SaveFileDialog();
                Dlg.Title = "Select destination file...";
                string DestFilename = SrcFilename;
                if (DestFilename.LastIndexOf('\\') > -1)
                {
                    Dlg.InitialDirectory = DestFilename.Substring(0, DestFilename.LastIndexOf('\\'));
                    DestFilename = DestFilename.Substring(DestFilename.LastIndexOf('\\') + 1);
                }
                Dlg.Filter = "JSON-Base64 (*.jb64)|*.jb64";
                Dlg.FileName = DestFilename.Substring(0, DestFilename.Length - SrcFileExt.Length) + ".jb64";
                Dlg.FileOk += delegate(object s, CancelEventArgs ev)
                {
                    DestFilename = Dlg.FileName;
                    string DestFileExt = GetFileExt(DestFilename);
                    if (DestFileExt != ".jb64")
                    {
                        MessageBox.Show("File extension must be .jb64.", "Error");

                        ev.Cancel = true;
                    }
                };
                DialogResult DlgResult = Dlg.ShowDialog();
                if (DlgResult == DialogResult.OK)
                {
                    select_dest_file.Text = Dlg.FileName;

                    return;
                }
            }
            else
            {
                MessageBox.Show("Please enter or select a valid source file first.\n\nSource file must have a .jb64, .xls, or .xlsx extension.", "Error");
            }
        }

        private void SelectFiles_OnDestFileDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))  e.Effect = DragDropEffects.Copy;
            else  e.Effect = DragDropEffects.None;
        }

        private void SelectFiles_OnDestFileDragDrop(object sender, DragEventArgs e)
        {
            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (FileList.Length > 0)  select_dest_file.Text = FileList[0];
        }

        private void SelectFiles_UpdateDestFile()
        {
            string DestFilename = select_source_file.Text;
            string DestFileExt = GetFileExt(DestFilename);

            if (DestFileExt == ".xls")  DestFilename = DestFilename.Substring(0, DestFilename.Length - 4) + ".jb64";
            else if (DestFileExt == ".xlsx")  DestFilename = DestFilename.Substring(0, DestFilename.Length - 5) + ".jb64";
            else if (DestFileExt == ".jb64")  DestFilename = DestFilename.Substring(0, DestFilename.Length - 5) + ".xlsx";
            else  DestFilename = "";

            select_dest_file.Text = DestFilename;
        }

        private void SelectFiles_OnCommit(object sender, AeroWizard.WizardPageConfirmEventArgs e)
        {
            string SrcFilename = select_source_file.Text;
            string SrcFileExt = GetFileExt(SrcFilename);
            string DestFilename = select_dest_file.Text;
            string DestFileExt = GetFileExt(DestFilename);

            if (SrcFileExt == ".jb64")
            {
                if (!File.Exists(SrcFilename))
                {
                    MessageBox.Show("Please enter or select a valid source file.\n\nSource file must exist and be accessible to this application.", "Error");

                    e.Cancel = true;
                }
                else if (DestFileExt != ".xls" && DestFileExt != ".xlsx" && DestFileExt != ".jb64")
                {
                    MessageBox.Show("Please enter or select a valid destination file.\n\nDestination file must have a .xls, .xlsx, or .jb64 extension.", "Error");

                    e.Cancel = true;
                }
            }
            else if (SrcFileExt == ".xls" || SrcFileExt == ".xlsx")
            {
                if (!File.Exists(SrcFilename))
                {
                    MessageBox.Show("Please enter or select a valid source file.\n\nSource file must exist and be accessible to this application.", "Error");

                    e.Cancel = true;
                }
                else if (DestFileExt != ".jb64")
                {
                    MessageBox.Show("Please enter or select a valid destination file.\n\nDestination file must have a .jb64 extension.", "Error");

                    e.Cancel = true;
                }
            }
            else
            {
                MessageBox.Show("Please enter or select a valid source file.\n\nSource file must have a .jb64, .xls, or .xlsx extension.", "Error");

                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                if (SrcFilename == DestFilename)
                {
                    MessageBox.Show("Please enter or select a valid destination file.\n\nDestination file must be different than source file.", "Error");

                    e.Cancel = true;
                }
                else if (File.Exists(DestFilename) && MessageBox.Show("Destination file exists.  Overwrite?", "Warning", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    try
                    {
                        if (SrcFileExt == ".jb64")  this.ReadConvert = new Reader_JB64();
                        else  this.ReadConvert = new Reader_Excel();

                        if (DestFileExt == ".jb64")  this.WriteConvert = new Writer_JB64();
                        else  this.WriteConvert = new Writer_Excel();

                        this.ReadConvert.Init(SrcFilename, "");

                        this.SetOptionsEntries = this.ReadConvert.GetSetOptionsEntryList(this.WriteConvert.IsBinaryAllowed());

                        options_sheet_select_box.DropDownStyle = this.ReadConvert.GetSheetNameBoxStyle();
                        options_sheet_select_box.DataSource = this.ReadConvert.GetSheetNames();

                        if (this.ReadConvert.GetSheetNameBoxStyle() == ComboBoxStyle.DropDownList)  options_sheet_select_box.SelectedIndex = 0;

                        if (this.SetOptionsEntries.Count > 0)
                        {
                            SetOptions_SwitchActiveEntry(0);
                        }
                        else
                        {
                            MessageBox.Show("Unable to locate a header or record data in the source file.\n\nTip:  Header and record column counts must be identical.", "Error");

                            e.Cancel = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An exception occurred while attempting to read the source file.\n\n" + ex.Message + "\n\n" + ex.StackTrace, "Error");

                        e.Cancel = true;
                    }
                }
            }
        }

        private static string FixDate(string Date)
        {
            if (Date == null)  return null;

            Regex TempReg = new Regex("[^0-9]");
            Date = TempReg.Replace(Date, " ");
            TempReg = new Regex("\\s+");
            Date = TempReg.Replace(Date, " ");
            string[] Items = Date.Trim().Split(' ');

            Date = "";
            Date += (Items.Length > 0 ? Items[0] : "0000").PadLeft(4, '0');
            Date += "-";
            Date += (Items.Length > 1 ? Items[1] : "00").PadLeft(2, '0');
            Date += "-";
            Date += (Items.Length > 2 ? Items[2] : "00").PadLeft(2, '0');
            Date += " ";
            Date += (Items.Length > 3 ? Items[3] : "00").PadLeft(2, '0');
            Date += ":";
            Date += (Items.Length > 4 ? Items[4] : "00").PadLeft(2, '0');
            Date += ":";
            Date += (Items.Length > 5 ? Items[5] : "00").PadLeft(2, '0');

            return Date;
        }

        public static string DateTimeToString(DateTime TempDateTime)
        {
            return FixDate(TempDateTime.Year.ToString() + '-' + TempDateTime.Month.ToString() + '-' + TempDateTime.Day.ToString() + ' ' + TempDateTime.Hour.ToString() + ':' + TempDateTime.Minute.ToString() + ':' + TempDateTime.Second.ToString());
        }

        public static string UTCToLocalDate(string Date)
        {
            Date = FixDate(Date);

            if (Date == "0000-00-00 00:00:00")  return Date;

            Regex TempReg = new Regex("[^0-9]");
            Date = TempReg.Replace(Date, " ");
            string[] Items = Date.Trim().Split(' ');
            DateTime TempDateTime = new DateTime(Convert.ToInt32(Items[0]), Convert.ToInt32(Items[1]), Convert.ToInt32(Items[2]), Convert.ToInt32(Items[3]), Convert.ToInt32(Items[4]), Convert.ToInt32(Items[5]), DateTimeKind.Utc);
            TempDateTime = TempDateTime.ToLocalTime();

            return DateTimeToString(TempDateTime);
        }

        public static string UTCToLocalTime(string Time)
        {
            string Result = UTCToLocalDate("1970-01-02 " + Time);

            return Result.Substring(Result.Length - 8);
        }

        public static string ExcelDateToUTC(double Date)
        {
           	// Local time might be ahead of UTC, so add one extra day to times.
            if (Date < 1.0)  Date += 1.0;

            DateTime TempDateTime = DateTime.FromOADate(Date).ToUniversalTime();

            return DateTimeToString(TempDateTime);
        }

        public static string ExcelTimeToUTC(double Time)
        {
            string Result = ExcelDateToUTC(Time);

            return Result.Substring(Result.Length - 8);
        }

        public static string ExcelDateToLocal(double Date)
        {
           	// Local time might be ahead of UTC, so add one extra day to times.
            if (Date < 1.0)  Date += 1.0;

            DateTime TempDateTime = DateTime.FromOADate(Date);

            return DateTimeToString(TempDateTime);
        }

        public static string ExcelTimeToLocal(double Time)
        {
            string Result = ExcelDateToLocal(Time);

            return Result.Substring(Result.Length - 8);
        }

        private string SetOptions_GetDisplayValue(JB64Value Val, string Type)
        {
            if (Val.Type == "number")
            {
                if (Type == "date")  Val = new JB64Value(ExcelDateToUTC((double)Val.Val));
                else if (Type == "date-alt")  Val = new JB64Value(ExcelDateToLocal((double)Val.Val));
                else if (Type == "time")  Val = new JB64Value(ExcelTimeToUTC((double)Val.Val));
                else if (Type == "time-alt")  Val = new JB64Value(ExcelTimeToLocal((double)Val.Val));
                else  Val = new JB64Value(Val);
            }
            else
            {
                Val = new JB64Value(Val);
            }
            Val.ConvertTo(Type.Replace("-alt", ""));
            Val.ConvertToString();
            string Val2 = (string)Val.Val;
            if (Type == "boolean")
            {
                Val2 = (Val2 == "0" ? "false" : "true");
            }

            if (Val2.Length > 50)
            {
                Val2 = Val2.Substring(0, 50) + "...";
            }

            return Val2;
        }

        private void SetOptions_SwitchActiveEntry(int NewActiveEntry)
        {
            this.SetOptionsActiveEntry = NewActiveEntry;

            options_col_list_box.Items.Clear();

            for (int x = 0; x < this.SetOptionsEntries[this.SetOptionsActiveEntry].Length; x++)
            {
                SetOptionsEntry TempEntry = this.SetOptionsEntries[this.SetOptionsActiveEntry][x];
                string Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, TempEntry.Type);

                options_col_list_box.Items.Add(TempEntry.Name + " (" + TempEntry.Type + " - " + Val + ")" + (TempEntry.Type != TempEntry.OrigType ? " [Was " + TempEntry.OrigType + "]" : ""), TempEntry.Enabled);
            }

            options_col_list_box.SelectedIndex = 0;
        }

        private void SetOptions_OnCheckAllClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int y = options_col_list_box.Items.Count;

            for (int x = 0; x < y; x++)
            {
                options_col_list_box.SetItemChecked(x, true);
            }
        }

        private void SetOptions_OnUncheckAllClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int y = options_col_list_box.CheckedItems.Count;

            for (int x = 0; x < y; x++)
            {
                options_col_list_box.SetItemChecked(x, false);
            }
        }

        private void SetOptions_OnSelectedSheetChanged(object sender, EventArgs e)
        {
            string SrcFilename = select_source_file.Text;
            string SrcFileExt = GetFileExt(SrcFilename);

            if (this.ReadConvert.GetSheetNameBoxStyle() == ComboBoxStyle.DropDownList && options_sheet_select_box.SelectedIndex > -1)
            {
                SetOptions_SwitchActiveEntry(options_sheet_select_box.SelectedIndex);
            }
        }

        private void SetOptions_UpdateSelectedColumnInfo()
        {
            if (this.UpdatingOptions)  return;

            this.UpdatingOptions = true;
            SetOptionsEntry TempEntry = this.SetOptionsEntries[this.SetOptionsActiveEntry][options_col_list_box.SelectedIndex];

            options_name.Text = TempEntry.Name;

            string Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "boolean");
            options_type_boolean.Text = "boolean (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "integer");
            options_type_integer.Text = "integer (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "number");
            options_type_number.Text = "number (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "date");
            options_type_date.Text = "date (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "date-alt");
            options_type_date_alt.Text = "date-alt (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "time");
            options_type_time.Text = "time (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "time-alt");
            options_type_time_alt.Text = "time-alt (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "string");
            options_type_string.Text = "string (" + Val + ")";

            Val = SetOptions_GetDisplayValue(TempEntry.OrigVal, "binary");
            options_type_binary.Text = "binary (" + Val + ")";

            if (TempEntry.Type == "boolean")  options_type_boolean.Checked = true;
            else if (TempEntry.Type == "integer")  options_type_integer.Checked = true;
            else if (TempEntry.Type == "number")  options_type_number.Checked = true;
            else if (TempEntry.Type == "date")  options_type_date.Checked = true;
            else if (TempEntry.Type == "date-alt")  options_type_date_alt.Checked = true;
            else if (TempEntry.Type == "time")  options_type_time.Checked = true;
            else if (TempEntry.Type == "time-alt")  options_type_time_alt.Checked = true;
            else if (TempEntry.Type == "string")  options_type_string.Checked = true;
            else if (TempEntry.Type == "binary")  options_type_binary.Checked = true;

            options_name_info_label.Enabled = TempEntry.Enabled;
            options_name.Enabled = TempEntry.Enabled;
            options_type_info_label.Enabled = TempEntry.Enabled;
            options_type_boolean.Enabled = TempEntry.Enabled;
            options_type_integer.Enabled = TempEntry.Enabled;
            options_type_number.Enabled = TempEntry.Enabled;
            options_type_date.Enabled = TempEntry.Enabled;
            options_type_date_alt.Enabled = (this.ReadConvert.IsDateAltAllowed(TempEntry.OrigType) && TempEntry.Enabled);
            options_type_time.Enabled = TempEntry.Enabled;
            options_type_time_alt.Enabled = (this.ReadConvert.IsTimeAltAllowed(TempEntry.OrigType) && TempEntry.Enabled);
            options_type_string.Enabled = TempEntry.Enabled;
            options_type_binary.Enabled = (this.WriteConvert.IsBinaryAllowed() && TempEntry.Enabled);

            this.UpdatingOptions = false;
        }

        private void SetOptions_OnSelectedColumnChanged(object sender, EventArgs e)
        {
            SetOptions_UpdateSelectedColumnInfo();
        }

        private void SetOptions_OnColumnEnabled(object sender, ItemCheckEventArgs e)
        {
            this.SetOptionsEntries[this.SetOptionsActiveEntry][e.Index].Enabled = (e.NewValue == CheckState.Checked);

            if (options_col_list_box.SelectedIndex == e.Index)
            {
                SetOptions_UpdateSelectedColumnInfo();
            }
        }

        private void SetOptions_UpdateColInfo()
        {
            if (this.UpdatingOptions)  return;

            this.UpdatingOptions = true;
            string Name = options_name.Text;
            this.SetOptionsEntries[this.SetOptionsActiveEntry][options_col_list_box.SelectedIndex].Name = Name;

            string Type = "";
            if (options_type_boolean.Checked)  Type = "boolean";
            else if (options_type_integer.Checked)  Type = "integer";
            else if (options_type_number.Checked)  Type = "number";
            else if (options_type_date.Checked)  Type = "date";
            else if (options_type_date_alt.Checked)  Type = "date-alt";
            else if (options_type_time.Checked)  Type = "time";
            else if (options_type_time_alt.Checked)  Type = "time-alt";
            else if (options_type_string.Checked)  Type = "string";
            else if (options_type_binary.Checked)  Type = "binary";
            this.SetOptionsEntries[this.SetOptionsActiveEntry][options_col_list_box.SelectedIndex].Type = Type;
            string OrigType = this.SetOptionsEntries[this.SetOptionsActiveEntry][options_col_list_box.SelectedIndex].OrigType;

            string Val = SetOptions_GetDisplayValue(this.SetOptionsEntries[this.SetOptionsActiveEntry][options_col_list_box.SelectedIndex].OrigVal, Type);

            int CurSel = options_col_list_box.SelectedIndex;
            options_col_list_box.Items[CurSel] = Name + " (" + Type + " - " + Val + ")" + (Type != OrigType ? " [Was " + OrigType + "]" : "");
            options_col_list_box.SelectedIndex = CurSel;

            this.UpdatingOptions = false;
        }

        private void SetOptions_OnColNameChanged(object sender, EventArgs e)
        {
            SetOptions_UpdateColInfo();
        }

        private void SetOptions_OnColTypeChanged(object sender, EventArgs e)
        {
            SetOptions_UpdateColInfo();
        }

        private void SetOptions_OnCommit(object sender, AeroWizard.WizardPageConfirmEventArgs e)
        {
            this.AllowNext = true;

            string SrcFilename = select_source_file.Text;
            string SrcFileExt = GetFileExt(SrcFilename);
            string DestFilename = select_dest_file.Text;
            string DestFileExt = GetFileExt(DestFilename);
            string SheetName = options_sheet_select_box.Text;

            try
            {
                this.ReadConvert.Init(SrcFilename, SheetName);
                this.WriteConvert.Init(DestFilename, SheetName);

                this.ConversionResults = "Conversion started.\r\n";

                List<JB64Header> Headers = new List<JB64Header>();
                foreach (SetOptionsEntry TempEntry in this.SetOptionsEntries[this.SetOptionsActiveEntry])
                {
                    if (TempEntry.Enabled)
                    {
                        Headers.Add(new JB64Header(TempEntry.Name, TempEntry.Type));
                    }
                }
                this.WriteConvert.WriteHeaders(Headers);

                this.ConversionResults += "Headers successfully written.\r\n";
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception occurred while attempting to initialize the conversion.\n\n" + ex.Message + "\n\n" + ex.StackTrace, "Error");

                this.AllowNext = false;
            }

            if (this.AllowNext)
            {
                // Save the sheet name if using a dropdown.
                if (options_sheet_select_box.DropDownStyle == ComboBoxStyle.DropDown)
                {
                    System.Collections.Specialized.StringCollection TempCollection = new System.Collections.Specialized.StringCollection();
                    TempCollection.Add(SheetName);
                    if (Properties.Settings.Default.SheetNamesHistory != null)
                    {
                        for (int x = 0; x < Properties.Settings.Default.SheetNamesHistory.Count; x++)
                        {
                            string Name = Properties.Settings.Default.SheetNamesHistory[x];

                            if (TempCollection.Count < 10 && Name != SheetName)
                            {
                                TempCollection.Add(Name);
                            }
                        }
                    }

                    Properties.Settings.Default.SheetNamesHistory = TempCollection;
                    Properties.Settings.Default.Save();
                }

                ProgressDialog Dlg = new ProgressDialog(this.WriteConvert.GetConversionMessage(), RunConversion);
                Dlg.ShowDialog();
            }

            if (!this.AllowNext)  e.Cancel = true;
        }

        private void RunConversion(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker TempWorker = (BackgroundWorker)sender;

            int RecordNum = 1;
            bool NullAllowed = this.WriteConvert.IsNullAllowed();
            SetOptionsEntry[] TempOptionsEntry = this.SetOptionsEntries[this.SetOptionsActiveEntry];
            while (!this.ReadConvert.EndOfRecords())
            {
                if (TempWorker.CancellationPending)
                {
                    this.AllowNext = false;
                    break;
                }

                // Read in a record.
                try
                {
                    JB64Value[] TempRecord = this.ReadConvert.ReadRecord();

                    // Transform the record.
                    try
                    {
                        for (int x = 0; x < TempRecord.Length; x++)
                        {
                            if (TempOptionsEntry[x].Enabled)
                            {
                                if (!NullAllowed)  TempRecord[x].ConvertToNotNull(TempOptionsEntry[x].Type);
                                if (TempOptionsEntry[x].OrigType == "number")
                                {
                                    if (TempOptionsEntry[x].Type == "date")  TempRecord[x] = new JB64Value(ExcelDateToUTC((double)TempRecord[x].Val));
                                    else if (TempOptionsEntry[x].Type == "date-alt")  TempRecord[x] = new JB64Value(ExcelDateToLocal((double)TempRecord[x].Val));
                                    else if (TempOptionsEntry[x].Type == "time")  TempRecord[x] = new JB64Value(ExcelTimeToUTC((double)TempRecord[x].Val));
                                    else if (TempOptionsEntry[x].Type == "time-alt")  TempRecord[x] = new JB64Value(ExcelTimeToLocal((double)TempRecord[x].Val));
                                }
                                TempRecord[x].ConvertTo(TempOptionsEntry[x].Type.Replace("-alt", ""));
                            }
                        }

                        // Write the record.
                        try
                        {
                            this.WriteConvert.WriteRecord(TempRecord);
                        }
                        catch (Exception)
                        {
                            this.ConversionResults += "Record #" + RecordNum + ":  Error writing the record.\r\n";
                        }
                    }
                    catch (Exception)
                    {
                        this.ConversionResults += "Record #" + RecordNum + ":  Error transforming the record.\r\n";
                    }
                }
                catch (Exception)
                {
                    this.ConversionResults += "Record #" + RecordNum + ":  Error reading the record.\r\n";
                }

                RecordNum++;

                int Percent = this.ReadConvert.PercentRead();
                TempWorker.ReportProgress(Percent, Percent.ToString() + "%");
            }

            this.WriteConvert.Close();
            this.ConversionResults += "Conversion completed.\r\n";
        }

        private void ViewResults_OnInitialize(object sender, AeroWizard.WizardPageInitEventArgs e)
        {
            results_conversion_log.Text = this.ConversionResults;

            string DestFilename = select_dest_file.Text;
            string DestFileExt = GetFileExt(DestFilename);

            if ((DestFileExt == ".xls" || DestFileExt == ".xlsx") && GetApplicationForFileExt(DestFileExt) != null)
            {
                results_open_in_excel.Enabled = true;
                results_open_in_excel.Checked = true;
            }
            else
            {
                results_open_in_excel.Enabled = false;
                results_open_in_excel.Checked = false;
            }
        }

        private void ViewResults_OnCommit(object sender, AeroWizard.WizardPageConfirmEventArgs e)
        {
            if (results_open_in_excel.Checked)
            {
                string DestFilename = select_dest_file.Text;

                if (File.Exists(DestFilename))
                {
                    string DestFileExt = GetFileExt(DestFilename);
                    ProcessStartInfo pi = new ProcessStartInfo();
                    pi.UseShellExecute = true;
                    pi.FileName = GetApplicationForFileExt(DestFileExt);
                    pi.WorkingDirectory = Path.GetDirectoryName(pi.FileName);
                    pi.Arguments = "\"" + DestFilename + "\"";
                    pi.Verb = "OPEN";
                    pi.ErrorDialog = true;
                    Process.Start(pi);
                }
            }

            Close();
        }

        public static string GetApplicationForFileExt(string FileExt)
        {
            string Result = GetClassesRootKeyDefaultValue(FileExt);
            if (Result == null)  return null;

            Result = GetClassesRootKeyDefaultValue(Result + "\\shell\\open\\command");
            if (Result == null)  return null;

            if (Result[0] == '"')
            {
                Result = Result.Substring(1);
                int Pos = Result.IndexOf('"');
                if (Pos > -1)  Result = Result.Substring(0, Pos);
            }
            else
            {
                int Pos = Result.IndexOf(' ');
                if (Pos > -1) Result = Result.Substring(0, Pos);
            }

            Result = Result.Trim();

            return Result;
        }

        private static string GetClassesRootKeyDefaultValue(string Path)
        {
            RegistryKey Key = Registry.ClassesRoot.OpenSubKey(Path);
            if (Key == null)  return null;

            object Val = Key.GetValue(null);
            if (Val == null)  return null;

            return Val.ToString();
        }
    }

    class SetOptionsEntry
    {
        public string Name { get; set; }
        public string OrigType { get; set; }
        public JB64Value OrigVal { get; set; }
        public string Type { get; set; }
        public bool Enabled { get; set; }

        public SetOptionsEntry(string Name, string OrigType, JB64Value OrigVal, bool Enabled)
        {
            this.Name = Name;
            this.OrigType = OrigType;
            this.OrigVal = OrigVal;
            this.Type = OrigType;
            this.Enabled = Enabled;
        }
    }

    interface ReadInterface
    {
        void Init(string Filename, string SheetName);
        List<SetOptionsEntry[]> GetSetOptionsEntryList(bool AllowBinary);
        System.Collections.Specialized.StringCollection GetSheetNames();
        ComboBoxStyle GetSheetNameBoxStyle();
        bool IsDateAltAllowed(string OrigType);
        bool IsTimeAltAllowed(string OrigType);
        JB64Value[] ReadRecord();
        bool EndOfRecords();
        int PercentRead();
    }

    class Reader_JB64 : ReadInterface
    {
        private JB64Decode Decoder;
        private StreamReader TempFile;
        private List<JB64Header> Headers;
        private long FileSize, DataRead;

        public Reader_JB64()
        {
            this.Decoder = new JB64Decode();
        }

        public void Init(string Filename, string SheetName)
        {
            this.TempFile = new StreamReader(Filename);
            this.FileSize = this.TempFile.BaseStream.Length;
            if (this.FileSize < 1)  this.FileSize = 1;
            string Line = this.TempFile.ReadLine();
            this.DataRead = Line.Length;
            this.Headers = this.Decoder.DecodeHeaders(Line);
        }

        public List<SetOptionsEntry[]> GetSetOptionsEntryList(bool AllowBinary)
        {
            JB64Value[] Result = ReadRecord();

            List<SetOptionsEntry[]> SetOptionsEntries = new List<SetOptionsEntry[]>();
            SetOptionsEntries.Add(new SetOptionsEntry[this.Headers.Count]);
            int x = 0;
            foreach (JB64Header Header in this.Headers)
            {
                string Type = Header.Type;
                if (Type.Length > 7 && Type.Substring(0, 7) == "custom:")  Type = "binary";
                if (Type == "binary" && !AllowBinary)  Type = "string";
                Result[x].ConvertToNotNull(Type);
                SetOptionsEntries[0][x] = new SetOptionsEntry(Header.Name, Type, Result[x], true);

                x++;
            }

            return SetOptionsEntries;
        }

        public System.Collections.Specialized.StringCollection GetSheetNames()
        {
            return Properties.Settings.Default.SheetNamesHistory;
        }

        public ComboBoxStyle GetSheetNameBoxStyle()
        {
            return ComboBoxStyle.DropDown;
        }

        public bool IsDateAltAllowed(string OrigType)
        {
            return false;
        }

        public bool IsTimeAltAllowed(string OrigType)
        {
            return false;
        }

        public JB64Value[] ReadRecord()
        {
            string Line = this.TempFile.ReadLine();
            this.DataRead += Line.Length;

            return this.Decoder.DecodeRecord(Line);
        }

        public bool EndOfRecords()
        {
            return this.TempFile.EndOfStream;
        }

        public int PercentRead()
        {
            return (int)(this.DataRead * 100 / this.FileSize);
        }
    }

    class Reader_Excel : ReadInterface
    {
        private FileStream TempFile;
        private IWorkbook Workbook;
        private ISheet CurrSheet;
        private int CurrRowNum, NumCols;

        public void Init(string Filename, string SheetName)
        {
            this.TempFile = File.OpenRead(Filename);
            this.Workbook = WorkbookFactory.Create(this.TempFile);

            if (SheetName != "")
            {
                this.CurrSheet = Workbook.GetSheet(SheetName);
                this.CurrRowNum = CurrSheet.FirstRowNum + 1;
                IRow HeaderRow = this.CurrSheet.GetRow(this.CurrSheet.FirstRowNum);
                this.NumCols = HeaderRow.LastCellNum - HeaderRow.FirstCellNum;
            }
        }

        public List<SetOptionsEntry[]> GetSetOptionsEntryList(bool AllowBinary)
        {
            List<SetOptionsEntry[]> SetOptionsEntries = new List<SetOptionsEntry[]>();

            foreach (ISheet TempSheet in Workbook)
            {
                // Get the headers and first record.
                if (TempSheet.FirstRowNum >= 0)
                {
                    IRow HeaderRow = TempSheet.GetRow(TempSheet.FirstRowNum);
                    IRow RecordRow = TempSheet.GetRow(TempSheet.FirstRowNum + 1);
                    if (HeaderRow != null && RecordRow != null)
                    {
                        SetOptionsEntries.Add(new SetOptionsEntry[HeaderRow.LastCellNum - HeaderRow.FirstCellNum]);

                        int y = HeaderRow.LastCellNum - HeaderRow.FirstCellNum;
                        for (int x = 0; x < y; x++)
                        {
                            ICell TempHeaderCell = HeaderRow.GetCell(HeaderRow.FirstCellNum + x);
                            ICell TempRecordCell = RecordRow.GetCell(RecordRow.FirstCellNum + x);

                            string HeaderName = (TempHeaderCell == null ? "" : TempHeaderCell.StringCellValue);

                            JB64Value Result;
                            if (TempRecordCell == null)
                            {
                                Result = new JB64Value("");
                            }
                            else if (TempRecordCell.CellType == CellType.Boolean)
                            {
                                Result = new JB64Value(TempRecordCell.BooleanCellValue);
                            }
                            else if (TempRecordCell.CellType == CellType.Numeric)
                            {
                                Result = new JB64Value(TempRecordCell.NumericCellValue);
                            }
                            else
                            {
                                Result = new JB64Value(TempRecordCell.StringCellValue);
                            }

                            SetOptionsEntries[SetOptionsEntries.Count - 1][x] = new SetOptionsEntry(HeaderName, Result.Type, Result, true);
                        }
                    }
                }
            }

            return SetOptionsEntries;
        }

        public System.Collections.Specialized.StringCollection GetSheetNames()
        {
            System.Collections.Specialized.StringCollection Result = new System.Collections.Specialized.StringCollection();

            foreach (ISheet TempSheet in Workbook)
            {
                if (TempSheet.FirstRowNum >= 0)
                {
                    IRow HeaderRow = TempSheet.GetRow(TempSheet.FirstRowNum);
                    IRow RecordRow = TempSheet.GetRow(TempSheet.FirstRowNum + 1);
                    if (HeaderRow != null && RecordRow != null)
                    {
                        Result.Add(TempSheet.SheetName);
                    }
                }
            }

            return Result;
        }

        public ComboBoxStyle GetSheetNameBoxStyle()
        {
            return ComboBoxStyle.DropDownList;
        }

        public bool IsDateAltAllowed(string OrigType)
        {
            return (OrigType == "number");
        }

        public bool IsTimeAltAllowed(string OrigType)
        {
            return (OrigType == "number");
        }

        public JB64Value[] ReadRecord()
        {
            IRow RecordRow = this.CurrSheet.GetRow(this.CurrRowNum);
            this.CurrRowNum++;
            if (RecordRow == null)  return null;
            JB64Value[] Result = new JB64Value[this.NumCols];
            for (int x = 0; x < this.NumCols; x++)
            {
                ICell TempRecordCell = RecordRow.GetCell(RecordRow.FirstCellNum + x);

                if (TempRecordCell == null)
                {
                    Result[x] = new JB64Value("");
                }
                else if (TempRecordCell.CellType == CellType.Boolean)
                {
                    Result[x] = new JB64Value(TempRecordCell.BooleanCellValue);
                }
                else if (TempRecordCell.CellType == CellType.Numeric)
                {
                    Result[x] = new JB64Value(TempRecordCell.NumericCellValue);
                }
                else
                {
                    Result[x] = new JB64Value(TempRecordCell.StringCellValue);
                }
            }

            return Result;
        }

        public bool EndOfRecords()
        {
            return (this.CurrRowNum > this.CurrSheet.LastRowNum);
        }

        public int PercentRead()
        {
            return ((this.CurrRowNum - this.CurrSheet.FirstRowNum) * 100) / (this.CurrSheet.LastRowNum - this.CurrSheet.FirstRowNum + 1);
        }
    }

    interface WriteInterface
    {
        bool IsNullAllowed();
        bool IsBinaryAllowed();
        void Init(string Filename, string SheetName);
        string GetConversionMessage();
        void WriteHeaders(List<JB64Header> Headers);
        void WriteRecord(JB64Value[] Data);
        void Close();
    }

    class Writer_JB64 : WriteInterface
    {
        private JB64Encode Encoder;
        private StreamWriter TempFile;

        public Writer_JB64()
        {
            this.Encoder = new JB64Encode();
        }

        public bool IsNullAllowed()
        {
            return true;
        }

        public bool IsBinaryAllowed()
        {
            return true;
        }

        public void Init(string Filename, string SheetName)
        {
            this.TempFile = new StreamWriter(Filename);
        }

        public string GetConversionMessage()
        {
            return "Converting to JSON-Base64";
        }

        public void WriteHeaders(List<JB64Header> Headers)
        {
            this.TempFile.Write(Encoder.EncodeHeaders(Headers));
        }

        public void WriteRecord(JB64Value[] Data)
        {
            this.TempFile.Write(Encoder.EncodeRecord(Data));
        }

        public void Close()
        {
            this.TempFile.Close();
            this.TempFile = null;
        }
    }

    class Writer_Excel : WriteInterface
    {
        FileStream TempFile;
        IWorkbook Workbook;
        ISheet CurrSheet;
        int CurrRow;

        public bool IsNullAllowed()
        {
            return false;
        }

        public bool IsBinaryAllowed()
        {
            return false;
        }

        public void Init(string Filename, string SheetName)
        {
            string FileExt = MainWizard.GetFileExt(Filename);
            if (FileExt == ".xls")  this.Workbook = new HSSFWorkbook();
            else  this.Workbook = new XSSFWorkbook();

            this.CurrSheet = this.Workbook.CreateSheet(SheetName);
            this.CurrRow = 0;

            this.TempFile = File.Create(Filename);
        }

        public string GetConversionMessage()
        {
            return "Converting to Excel";
        }

        public void WriteHeaders(List<JB64Header> Headers)
        {
            IRow TempRow = this.CurrSheet.CreateRow(this.CurrRow);
            this.CurrRow++;
            int x = 0;
            foreach (JB64Header Header in Headers)
            {
                TempRow.CreateCell(x).SetCellValue(Header.Name);

                x++;
            }
        }

        public void WriteRecord(JB64Value[] Data)
        {
            IRow TempRow = this.CurrSheet.CreateRow(this.CurrRow);
            this.CurrRow++;
            for (int x = 0; x < Data.Length; x++)
            {
                ICell TempCell = TempRow.CreateCell(x);
                if (Data[x].Type == "boolean")
                {
                    TempCell.SetCellValue((bool)Data[x].Val);
                }
                else if (Data[x].Type == "integer" || Data[x].Type == "number")
                {
                    Data[x].ConvertToNumber();
                    TempCell.SetCellValue((double)Data[x].Val);
                }
                else
                {
                    if (Data[x].Type == "date")  Data[x] = new JB64Value(MainWizard.UTCToLocalDate((string)Data[x].Val), "date");
                    if (Data[x].Type == "time")  Data[x] = new JB64Value(MainWizard.UTCToLocalTime((string)Data[x].Val), "time");

                    Data[x].ConvertToString();
                    TempCell.SetCellValue((string)Data[x].Val);
                }
            }
        }

        public void Close()
        {
            this.Workbook.Write(this.TempFile);
            this.TempFile.Close();
        }
    }
}
