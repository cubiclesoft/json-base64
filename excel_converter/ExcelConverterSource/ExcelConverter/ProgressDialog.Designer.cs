namespace ExcelConverter
{
    partial class ProgressDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressDialog));
            this.progress_bar_label = new System.Windows.Forms.Label();
            this.progress_bar = new System.Windows.Forms.ProgressBar();
            this.cancel_button = new System.Windows.Forms.Button();
            this.background_worker = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            // 
            // progress_bar_label
            // 
            this.progress_bar_label.AutoSize = true;
            this.progress_bar_label.Location = new System.Drawing.Point(13, 13);
            this.progress_bar_label.Name = "progress_bar_label";
            this.progress_bar_label.Size = new System.Drawing.Size(51, 13);
            this.progress_bar_label.TabIndex = 0;
            this.progress_bar_label.Text = "Progress:";
            // 
            // progress_bar
            // 
            this.progress_bar.Location = new System.Drawing.Point(16, 30);
            this.progress_bar.Name = "progress_bar";
            this.progress_bar.Size = new System.Drawing.Size(422, 23);
            this.progress_bar.TabIndex = 1;
            // 
            // cancel_button
            // 
            this.cancel_button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel_button.Location = new System.Drawing.Point(188, 59);
            this.cancel_button.Name = "cancel_button";
            this.cancel_button.Size = new System.Drawing.Size(75, 23);
            this.cancel_button.TabIndex = 2;
            this.cancel_button.Text = "&Cancel";
            this.cancel_button.UseVisualStyleBackColor = true;
            this.cancel_button.Click += new System.EventHandler(this.OnCancel);
            // 
            // background_worker
            // 
            this.background_worker.WorkerReportsProgress = true;
            this.background_worker.WorkerSupportsCancellation = true;
            this.background_worker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BackgroundProgressChanged);
            this.background_worker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundRunWorkerCompleted);
            // 
            // ProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancel_button;
            this.ClientSize = new System.Drawing.Size(450, 94);
            this.Controls.Add(this.cancel_button);
            this.Controls.Add(this.progress_bar);
            this.Controls.Add(this.progress_bar_label);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.Load += new System.EventHandler(this.OnLoad);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label progress_bar_label;
        private System.Windows.Forms.ProgressBar progress_bar;
        private System.Windows.Forms.Button cancel_button;
        private System.ComponentModel.BackgroundWorker background_worker;
    }
}