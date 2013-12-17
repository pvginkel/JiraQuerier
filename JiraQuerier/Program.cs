using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;

namespace JiraQuerier
{
    static class Program
    {
        public static RegistryKey BaseKey
        {
            get { return Registry.CurrentUser.CreateSubKey("Software\\JIRA Querier"); }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
