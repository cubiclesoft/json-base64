namespace ExcelConverter
{
    partial class MainWizard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label label1;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWizard));
            this.step_wizard = new AeroWizard.StepWizardControl();
            this.wizard_select_files = new AeroWizard.WizardPage();
            this.select_dest_file_browse = new System.Windows.Forms.Button();
            this.select_dest_file = new System.Windows.Forms.TextBox();
            this.select_dest_file_label = new System.Windows.Forms.Label();
            this.select_source_file_browse = new System.Windows.Forms.Button();
            this.select_source_file = new System.Windows.Forms.TextBox();
            this.select_source_file_label = new System.Windows.Forms.Label();
            this.wizard_set_options = new AeroWizard.WizardPage();
            this.options_type_binary = new System.Windows.Forms.RadioButton();
            this.options_name_info_label = new System.Windows.Forms.Label();
            this.options_name = new System.Windows.Forms.TextBox();
            this.options_check_all = new System.Windows.Forms.LinkLabel();
            this.options_uncheck_all = new System.Windows.Forms.LinkLabel();
            this.options_sheet_select_box = new System.Windows.Forms.ComboBox();
            this.options_col_list_box = new System.Windows.Forms.CheckedListBox();
            this.options_sheet_info_label = new System.Windows.Forms.Label();
            this.options_type_info_label = new System.Windows.Forms.Label();
            this.options_type_string = new System.Windows.Forms.RadioButton();
            this.options_type_time_alt = new System.Windows.Forms.RadioButton();
            this.options_type_time = new System.Windows.Forms.RadioButton();
            this.options_type_date_alt = new System.Windows.Forms.RadioButton();
            this.options_type_date = new System.Windows.Forms.RadioButton();
            this.options_type_number = new System.Windows.Forms.RadioButton();
            this.options_type_integer = new System.Windows.Forms.RadioButton();
            this.options_type_boolean = new System.Windows.Forms.RadioButton();
            this.options_col_info_label = new System.Windows.Forms.Label();
            this.wizard_view_results = new AeroWizard.WizardPage();
            this.results_open_in_excel = new System.Windows.Forms.CheckBox();
            this.results_conversion_log = new System.Windows.Forms.TextBox();
            this.results_conversion_log_label = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.step_wizard)).BeginInit();
            this.wizard_select_files.SuspendLayout();
            this.wizard_set_options.SuspendLayout();
            this.wizard_view_results.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(300, 48);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(10, 15);
            label1.TabIndex = 4;
            label1.Text = "|";
            // 
            // step_wizard
            // 
            this.step_wizard.Location = new System.Drawing.Point(0, 0);
            this.step_wizard.Name = "step_wizard";
            this.step_wizard.NextButtonText = "&Next >";
            this.step_wizard.Pages.Add(this.wizard_select_files);
            this.step_wizard.Pages.Add(this.wizard_set_options);
            this.step_wizard.Pages.Add(this.wizard_view_results);
            this.step_wizard.Size = new System.Drawing.Size(584, 650);
            this.step_wizard.TabIndex = 0;
            this.step_wizard.Title = "JSON-Base64 Converter";
            this.step_wizard.TitleIcon = ((System.Drawing.Icon)(resources.GetObject("step_wizard.TitleIcon")));
            this.step_wizard.Cancelling += new System.ComponentModel.CancelEventHandler(this.OnClickCancel);
            // 
            // wizard_select_files
            // 
            this.wizard_select_files.Controls.Add(this.select_dest_file_browse);
            this.wizard_select_files.Controls.Add(this.select_dest_file);
            this.wizard_select_files.Controls.Add(this.select_dest_file_label);
            this.wizard_select_files.Controls.Add(this.select_source_file_browse);
            this.wizard_select_files.Controls.Add(this.select_source_file);
            this.wizard_select_files.Controls.Add(this.select_source_file_label);
            this.wizard_select_files.Name = "wizard_select_files";
            this.wizard_select_files.NextPage = this.wizard_set_options;
            this.wizard_select_files.Size = new System.Drawing.Size(386, 495);
            this.wizard_select_files.TabIndex = 2;
            this.wizard_select_files.Text = "Select Files";
            this.wizard_select_files.Commit += new System.EventHandler<AeroWizard.WizardPageConfirmEventArgs>(this.SelectFiles_OnCommit);
            // 
            // select_dest_file_browse
            // 
            this.select_dest_file_browse.Location = new System.Drawing.Point(308, 74);
            this.select_dest_file_browse.Name = "select_dest_file_browse";
            this.select_dest_file_browse.Size = new System.Drawing.Size(75, 23);
            this.select_dest_file_browse.TabIndex = 5;
            this.select_dest_file_browse.Text = "Browse...";
            this.select_dest_file_browse.UseVisualStyleBackColor = true;
            this.select_dest_file_browse.Click += new System.EventHandler(this.SelectFiles_OnDestFileBrowse);
            // 
            // select_dest_file
            // 
            this.select_dest_file.AllowDrop = true;
            this.select_dest_file.Location = new System.Drawing.Point(7, 74);
            this.select_dest_file.Name = "select_dest_file";
            this.select_dest_file.Size = new System.Drawing.Size(295, 23);
            this.select_dest_file.TabIndex = 4;
            this.select_dest_file.DragDrop += new System.Windows.Forms.DragEventHandler(this.SelectFiles_OnDestFileDragDrop);
            this.select_dest_file.DragEnter += new System.Windows.Forms.DragEventHandler(this.SelectFiles_OnDestFileDragEnter);
            // 
            // select_dest_file_label
            // 
            this.select_dest_file_label.AutoSize = true;
            this.select_dest_file_label.Location = new System.Drawing.Point(4, 55);
            this.select_dest_file_label.Name = "select_dest_file_label";
            this.select_dest_file_label.Size = new System.Drawing.Size(91, 15);
            this.select_dest_file_label.TabIndex = 3;
            this.select_dest_file_label.Text = "&Destination File:";
            // 
            // select_source_file_browse
            // 
            this.select_source_file_browse.Location = new System.Drawing.Point(308, 23);
            this.select_source_file_browse.Name = "select_source_file_browse";
            this.select_source_file_browse.Size = new System.Drawing.Size(75, 23);
            this.select_source_file_browse.TabIndex = 2;
            this.select_source_file_browse.Text = "Browse...";
            this.select_source_file_browse.UseVisualStyleBackColor = true;
            this.select_source_file_browse.Click += new System.EventHandler(this.SelectFiles_OnSourceFileBrowse);
            // 
            // select_source_file
            // 
            this.select_source_file.AllowDrop = true;
            this.select_source_file.Location = new System.Drawing.Point(7, 23);
            this.select_source_file.Name = "select_source_file";
            this.select_source_file.Size = new System.Drawing.Size(295, 23);
            this.select_source_file.TabIndex = 1;
            this.select_source_file.DragDrop += new System.Windows.Forms.DragEventHandler(this.SelectFiles_OnSourceFileDragDrop);
            this.select_source_file.DragEnter += new System.Windows.Forms.DragEventHandler(this.SelectFiles_OnSourceFileDragEnter);
            // 
            // select_source_file_label
            // 
            this.select_source_file_label.AutoSize = true;
            this.select_source_file_label.Location = new System.Drawing.Point(4, 4);
            this.select_source_file_label.Name = "select_source_file_label";
            this.select_source_file_label.Size = new System.Drawing.Size(67, 15);
            this.select_source_file_label.TabIndex = 0;
            this.select_source_file_label.Text = "&Source File:";
            // 
            // wizard_set_options
            // 
            this.wizard_set_options.Controls.Add(this.options_type_binary);
            this.wizard_set_options.Controls.Add(this.options_name_info_label);
            this.wizard_set_options.Controls.Add(this.options_name);
            this.wizard_set_options.Controls.Add(label1);
            this.wizard_set_options.Controls.Add(this.options_check_all);
            this.wizard_set_options.Controls.Add(this.options_uncheck_all);
            this.wizard_set_options.Controls.Add(this.options_sheet_select_box);
            this.wizard_set_options.Controls.Add(this.options_col_list_box);
            this.wizard_set_options.Controls.Add(this.options_sheet_info_label);
            this.wizard_set_options.Controls.Add(this.options_type_info_label);
            this.wizard_set_options.Controls.Add(this.options_type_string);
            this.wizard_set_options.Controls.Add(this.options_type_time_alt);
            this.wizard_set_options.Controls.Add(this.options_type_time);
            this.wizard_set_options.Controls.Add(this.options_type_date_alt);
            this.wizard_set_options.Controls.Add(this.options_type_date);
            this.wizard_set_options.Controls.Add(this.options_type_number);
            this.wizard_set_options.Controls.Add(this.options_type_integer);
            this.wizard_set_options.Controls.Add(this.options_type_boolean);
            this.wizard_set_options.Controls.Add(this.options_col_info_label);
            this.wizard_set_options.Name = "wizard_set_options";
            this.wizard_set_options.NextPage = this.wizard_view_results;
            this.wizard_set_options.Size = new System.Drawing.Size(386, 495);
            this.wizard_set_options.TabIndex = 3;
            this.wizard_set_options.Text = "Set Options";
            this.wizard_set_options.Commit += new System.EventHandler<AeroWizard.WizardPageConfirmEventArgs>(this.SetOptions_OnCommit);
            // 
            // options_type_binary
            // 
            this.options_type_binary.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_binary.Location = new System.Drawing.Point(8, 471);
            this.options_type_binary.Name = "options_type_binary";
            this.options_type_binary.Size = new System.Drawing.Size(373, 20);
            this.options_type_binary.TabIndex = 18;
            this.options_type_binary.Text = "binary";
            this.options_type_binary.UseVisualStyleBackColor = true;
            this.options_type_binary.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_name_info_label
            // 
            this.options_name_info_label.AutoSize = true;
            this.options_name_info_label.Location = new System.Drawing.Point(4, 240);
            this.options_name_info_label.Name = "options_name_info_label";
            this.options_name_info_label.Size = new System.Drawing.Size(42, 15);
            this.options_name_info_label.TabIndex = 7;
            this.options_name_info_label.Text = "&Name:";
            // 
            // options_name
            // 
            this.options_name.Location = new System.Drawing.Point(52, 237);
            this.options_name.Name = "options_name";
            this.options_name.Size = new System.Drawing.Size(329, 23);
            this.options_name.TabIndex = 8;
            this.options_name.TextChanged += new System.EventHandler(this.SetOptions_OnColNameChanged);
            // 
            // options_check_all
            // 
            this.options_check_all.AutoSize = true;
            this.options_check_all.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(51)))), ((int)(((byte)(153)))));
            this.options_check_all.Location = new System.Drawing.Point(242, 48);
            this.options_check_all.Name = "options_check_all";
            this.options_check_all.Size = new System.Drawing.Size(57, 15);
            this.options_check_all.TabIndex = 3;
            this.options_check_all.TabStop = true;
            this.options_check_all.Text = "C&heck All";
            this.options_check_all.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SetOptions_OnCheckAllClicked);
            // 
            // options_uncheck_all
            // 
            this.options_uncheck_all.AutoSize = true;
            this.options_uncheck_all.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(51)))), ((int)(((byte)(153)))));
            this.options_uncheck_all.Location = new System.Drawing.Point(311, 48);
            this.options_uncheck_all.Name = "options_uncheck_all";
            this.options_uncheck_all.Size = new System.Drawing.Size(70, 15);
            this.options_uncheck_all.TabIndex = 5;
            this.options_uncheck_all.TabStop = true;
            this.options_uncheck_all.Text = "&Uncheck All";
            this.options_uncheck_all.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SetOptions_OnUncheckAllClicked);
            // 
            // options_sheet_select_box
            // 
            this.options_sheet_select_box.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.options_sheet_select_box.FormattingEnabled = true;
            this.options_sheet_select_box.Location = new System.Drawing.Point(3, 21);
            this.options_sheet_select_box.Name = "options_sheet_select_box";
            this.options_sheet_select_box.Size = new System.Drawing.Size(378, 23);
            this.options_sheet_select_box.TabIndex = 1;
            this.options_sheet_select_box.SelectedIndexChanged += new System.EventHandler(this.SetOptions_OnSelectedSheetChanged);
            // 
            // options_col_list_box
            // 
            this.options_col_list_box.FormattingEnabled = true;
            this.options_col_list_box.Location = new System.Drawing.Point(3, 65);
            this.options_col_list_box.Name = "options_col_list_box";
            this.options_col_list_box.Size = new System.Drawing.Size(378, 166);
            this.options_col_list_box.TabIndex = 6;
            this.options_col_list_box.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.SetOptions_OnColumnEnabled);
            this.options_col_list_box.SelectedIndexChanged += new System.EventHandler(this.SetOptions_OnSelectedColumnChanged);
            // 
            // options_sheet_info_label
            // 
            this.options_sheet_info_label.AutoSize = true;
            this.options_sheet_info_label.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_sheet_info_label.Location = new System.Drawing.Point(3, 4);
            this.options_sheet_info_label.Name = "options_sheet_info_label";
            this.options_sheet_info_label.Size = new System.Drawing.Size(39, 15);
            this.options_sheet_info_label.TabIndex = 0;
            this.options_sheet_info_label.Text = "&Sheet:";
            // 
            // options_type_info_label
            // 
            this.options_type_info_label.AutoSize = true;
            this.options_type_info_label.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_info_label.Location = new System.Drawing.Point(5, 268);
            this.options_type_info_label.Name = "options_type_info_label";
            this.options_type_info_label.Size = new System.Drawing.Size(36, 15);
            this.options_type_info_label.TabIndex = 9;
            this.options_type_info_label.Text = "&Type:";
            // 
            // options_type_string
            // 
            this.options_type_string.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_string.Location = new System.Drawing.Point(8, 448);
            this.options_type_string.Name = "options_type_string";
            this.options_type_string.Size = new System.Drawing.Size(373, 20);
            this.options_type_string.TabIndex = 17;
            this.options_type_string.Text = "string";
            this.options_type_string.UseVisualStyleBackColor = true;
            this.options_type_string.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_type_time_alt
            // 
            this.options_type_time_alt.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_time_alt.Location = new System.Drawing.Point(8, 425);
            this.options_type_time_alt.Name = "options_type_time_alt";
            this.options_type_time_alt.Size = new System.Drawing.Size(373, 20);
            this.options_type_time_alt.TabIndex = 16;
            this.options_type_time_alt.Text = "time-alt";
            this.options_type_time_alt.UseVisualStyleBackColor = true;
            this.options_type_time_alt.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_type_time
            // 
            this.options_type_time.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_time.Location = new System.Drawing.Point(8, 402);
            this.options_type_time.Name = "options_type_time";
            this.options_type_time.Size = new System.Drawing.Size(373, 20);
            this.options_type_time.TabIndex = 15;
            this.options_type_time.Text = "time";
            this.options_type_time.UseVisualStyleBackColor = true;
            this.options_type_time.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_type_date_alt
            // 
            this.options_type_date_alt.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_date_alt.Location = new System.Drawing.Point(8, 379);
            this.options_type_date_alt.Name = "options_type_date_alt";
            this.options_type_date_alt.Size = new System.Drawing.Size(373, 20);
            this.options_type_date_alt.TabIndex = 14;
            this.options_type_date_alt.Text = "date-alt";
            this.options_type_date_alt.UseVisualStyleBackColor = true;
            this.options_type_date_alt.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_type_date
            // 
            this.options_type_date.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_date.Location = new System.Drawing.Point(8, 356);
            this.options_type_date.Name = "options_type_date";
            this.options_type_date.Size = new System.Drawing.Size(373, 20);
            this.options_type_date.TabIndex = 13;
            this.options_type_date.Text = "date";
            this.options_type_date.UseVisualStyleBackColor = true;
            this.options_type_date.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_type_number
            // 
            this.options_type_number.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_number.Location = new System.Drawing.Point(8, 333);
            this.options_type_number.Name = "options_type_number";
            this.options_type_number.Size = new System.Drawing.Size(373, 20);
            this.options_type_number.TabIndex = 12;
            this.options_type_number.Text = "number";
            this.options_type_number.UseVisualStyleBackColor = true;
            this.options_type_number.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_type_integer
            // 
            this.options_type_integer.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_integer.Location = new System.Drawing.Point(8, 310);
            this.options_type_integer.Name = "options_type_integer";
            this.options_type_integer.Size = new System.Drawing.Size(373, 20);
            this.options_type_integer.TabIndex = 11;
            this.options_type_integer.Text = "integer";
            this.options_type_integer.UseVisualStyleBackColor = true;
            this.options_type_integer.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_type_boolean
            // 
            this.options_type_boolean.Checked = true;
            this.options_type_boolean.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_type_boolean.Location = new System.Drawing.Point(8, 287);
            this.options_type_boolean.Name = "options_type_boolean";
            this.options_type_boolean.Size = new System.Drawing.Size(373, 20);
            this.options_type_boolean.TabIndex = 10;
            this.options_type_boolean.TabStop = true;
            this.options_type_boolean.Text = "boolean";
            this.options_type_boolean.UseVisualStyleBackColor = true;
            this.options_type_boolean.Click += new System.EventHandler(this.SetOptions_OnColTypeChanged);
            // 
            // options_col_info_label
            // 
            this.options_col_info_label.AutoSize = true;
            this.options_col_info_label.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.options_col_info_label.Location = new System.Drawing.Point(2, 48);
            this.options_col_info_label.Name = "options_col_info_label";
            this.options_col_info_label.Size = new System.Drawing.Size(58, 15);
            this.options_col_info_label.TabIndex = 2;
            this.options_col_info_label.Text = "&Columns:";
            // 
            // wizard_view_results
            // 
            this.wizard_view_results.AllowCancel = false;
            this.wizard_view_results.Controls.Add(this.results_open_in_excel);
            this.wizard_view_results.Controls.Add(this.results_conversion_log);
            this.wizard_view_results.Controls.Add(this.results_conversion_log_label);
            this.wizard_view_results.IsFinishPage = true;
            this.wizard_view_results.Name = "wizard_view_results";
            this.wizard_view_results.Size = new System.Drawing.Size(386, 495);
            this.wizard_view_results.TabIndex = 4;
            this.wizard_view_results.Text = "View Results";
            this.wizard_view_results.Commit += new System.EventHandler<AeroWizard.WizardPageConfirmEventArgs>(this.ViewResults_OnCommit);
            this.wizard_view_results.Initialize += new System.EventHandler<AeroWizard.WizardPageInitEventArgs>(this.ViewResults_OnInitialize);
            // 
            // results_open_in_excel
            // 
            this.results_open_in_excel.Location = new System.Drawing.Point(3, 472);
            this.results_open_in_excel.Name = "results_open_in_excel";
            this.results_open_in_excel.Size = new System.Drawing.Size(379, 20);
            this.results_open_in_excel.TabIndex = 2;
            this.results_open_in_excel.Text = "&Open in Excel";
            this.results_open_in_excel.UseVisualStyleBackColor = true;
            // 
            // results_conversion_log
            // 
            this.results_conversion_log.Location = new System.Drawing.Point(7, 23);
            this.results_conversion_log.Multiline = true;
            this.results_conversion_log.Name = "results_conversion_log";
            this.results_conversion_log.ReadOnly = true;
            this.results_conversion_log.Size = new System.Drawing.Size(376, 443);
            this.results_conversion_log.TabIndex = 1;
            // 
            // results_conversion_log_label
            // 
            this.results_conversion_log_label.AutoSize = true;
            this.results_conversion_log_label.Location = new System.Drawing.Point(4, 4);
            this.results_conversion_log_label.Name = "results_conversion_log_label";
            this.results_conversion_log_label.Size = new System.Drawing.Size(93, 15);
            this.results_conversion_log_label.TabIndex = 0;
            this.results_conversion_log_label.Text = "&Conversion Log:";
            // 
            // MainWizard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 650);
            this.Controls.Add(this.step_wizard);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainWizard";
            this.Text = "JSON-Base64 Converter";
            ((System.ComponentModel.ISupportInitialize)(this.step_wizard)).EndInit();
            this.wizard_select_files.ResumeLayout(false);
            this.wizard_select_files.PerformLayout();
            this.wizard_set_options.ResumeLayout(false);
            this.wizard_set_options.PerformLayout();
            this.wizard_view_results.ResumeLayout(false);
            this.wizard_view_results.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private AeroWizard.StepWizardControl step_wizard;
        private AeroWizard.WizardPage wizard_select_files;
        private AeroWizard.WizardPage wizard_set_options;
        private AeroWizard.WizardPage wizard_view_results;
        private System.Windows.Forms.Button select_dest_file_browse;
        private System.Windows.Forms.TextBox select_dest_file;
        private System.Windows.Forms.Label select_dest_file_label;
        private System.Windows.Forms.Button select_source_file_browse;
        private System.Windows.Forms.TextBox select_source_file;
        private System.Windows.Forms.Label select_source_file_label;
        private System.Windows.Forms.ComboBox options_sheet_select_box;
        private System.Windows.Forms.CheckedListBox options_col_list_box;
        private System.Windows.Forms.Label options_sheet_info_label;
        private System.Windows.Forms.Label options_type_info_label;
        private System.Windows.Forms.RadioButton options_type_string;
        private System.Windows.Forms.RadioButton options_type_time_alt;
        private System.Windows.Forms.RadioButton options_type_time;
        private System.Windows.Forms.RadioButton options_type_date_alt;
        private System.Windows.Forms.RadioButton options_type_date;
        private System.Windows.Forms.RadioButton options_type_number;
        private System.Windows.Forms.RadioButton options_type_integer;
        private System.Windows.Forms.RadioButton options_type_boolean;
        private System.Windows.Forms.Label options_col_info_label;
        private System.Windows.Forms.TextBox results_conversion_log;
        private System.Windows.Forms.Label results_conversion_log_label;
        private System.Windows.Forms.CheckBox results_open_in_excel;
        private System.Windows.Forms.LinkLabel options_uncheck_all;
        private System.Windows.Forms.LinkLabel options_check_all;
        private System.Windows.Forms.Label options_name_info_label;
        private System.Windows.Forms.TextBox options_name;
        private System.Windows.Forms.RadioButton options_type_binary;
    }
}