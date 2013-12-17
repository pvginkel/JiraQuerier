﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SystemEx.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Form = System.Windows.Forms.Form;

namespace Jint.Debugger
{
    public partial class CallStackControl : DockContent
    {
        private DebugInformation _displayedLocalsSource;

        public CallStackControl()
        {
            Font = SystemFonts.MessageBoxFont;

            InitializeComponent();

            VisualStyleUtil.StyleListView(_listView);
        }

        public void ResetCallStack()
        {
            _listView.Items.Clear();
            _listView.Enabled = false;

            _displayedLocalsSource = null;
        }

        public void LoadCallStack(DebugInformation debug)
        {
            if (_displayedLocalsSource == debug)
                return;

            _displayedLocalsSource = debug;

            _listView.Enabled = true;

            _listView.BeginUpdate();

            _listView.Items.Clear();

            foreach (string line in debug.CallStack)
            {
                _listView.Items.Add(new ListViewItem(line));
            }

            _listView.EndUpdate();
        }

        private void _listView_SizeChanged(object sender, EventArgs e)
        {
            _nameHeader.Width = _listView.ClientSize.Width - 6;
        }
    }
}
