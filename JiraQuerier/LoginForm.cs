using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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
                }

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Invalid user name or password" + Environment.NewLine +
                    Environment.NewLine +
                    ex.Message + " (" + ex.GetType().FullName + ")",
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
            _password.Text = "UqcZ}y8H";
            _acceptButton.PerformClick();
#endif
        }
    }
}
