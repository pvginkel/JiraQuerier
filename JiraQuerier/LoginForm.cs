using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Windows.Forms;
using JintDebugger;

namespace JiraQuerier
{
    public partial class LoginForm : SystemEx.Windows.Forms.Form
    {
        private readonly IStatusBarProvider _statusBarProvider;

        public JiraApi Api { get; private set; }

        public LoginForm(IStatusBarProvider statusBarProvider)
        {
            if (statusBarProvider == null)
                throw new ArgumentNullException("statusBarProvider");

            _statusBarProvider = statusBarProvider;

            InitializeComponent();

            using (var key = Program.BaseKey)
            {
                _userName.Text = (string)key.GetValue("User name");
                _site.Text = (string)key.GetValue("Site");
#if DEBUG
                _password.Text = (string)key.GetValue("Password");
#endif
            }
        }

        private void _acceptButton_Click(object sender, EventArgs e)
        {
            try
            {
                var api = new JiraApi(_site.Text, _userName.Text, _password.Text, _statusBarProvider);

                api.Request("rest/api/2/issue/createmeta", null, null);

                Api = api;

                using (var key = Program.BaseKey)
                {
                    key.SetValue("User name", _userName.Text);
                    key.SetValue("Site", _site.Text);
#if DEBUG
                    key.SetValue("Password", _password.Text);
#endif
                }

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                string message;

                if ((ex as WebException)?.Response != null && ((HttpWebResponse)((WebException)ex).Response).StatusCode == HttpStatusCode.Forbidden)
                    message = "Invalid user name or password";
                else
                    message = "Could not connect to JIRA" + Environment.NewLine + Environment.NewLine + ex.Message + " (" + ex.GetType().FullName + ")";

                MessageBox.Show(
                    this,
                    message,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void LoginForm_Shown(object sender, EventArgs e)
        {
            if (_userName.Text.Length > 0)
                _password.Focus();

#if DEBUG
            _acceptButton.PerformClick();
#endif
        }
    }
}
