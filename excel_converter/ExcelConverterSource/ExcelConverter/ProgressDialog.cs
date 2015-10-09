using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ExcelConverter
{
    public partial class ProgressDialog : Form
    {
        public ProgressDialog(string InitialTitle, DoWorkEventHandler Handler)
        {
            InitializeComponent();
            this.Text = InitialTitle;
            background_worker.DoWork += Handler;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            background_worker.RunWorkerAsync();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            if (cancel_button.Enabled)
            {
                background_worker.CancelAsync();
                cancel_button.Enabled = false;
                progress_bar_label.Text = "Canceling, please wait...";
            }
        }

        private void BackgroundProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (cancel_button.Enabled)
            {
                progress_bar.Value = e.ProgressPercentage;
                progress_bar_label.Text = (string)e.UserState;
            }
        }

        private void BackgroundRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            OnCancel(sender, e);

            if (background_worker.IsBusy)  e.Cancel = true;
        }
    }
}
