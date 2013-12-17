using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace JiraQuerier
{
    public partial class MainForm : SystemEx.Windows.Forms.Form, IStatusBarProvider
    {
        public const string FileDialogFilter = "JavaScript (*.js)|*.js|All Files (*.*)|*.*";

        private readonly OutputControl _outputControl;
        private JiraApi _api;

        private EditorControl ActiveDocument
        {
            get { return (EditorControl)_dockPanel.ActiveDocument; }
        }

        public MainForm()
        {
            InitializeComponent();

            _dockPanel.Theme = new VS2012LightTheme();

            _outputControl = new OutputControl();

            _outputControl.Show(_dockPanel, DockState.DockBottom);

            UpdateEnabled();
        }

        private void _fileNew_Click(object sender, EventArgs e)
        {
            OpenEditor(null);
        }

        private void _fileOpen_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.CheckFileExists = true;
                dialog.Filter = FileDialogFilter;
                dialog.Multiselect = false;
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    OpenEditor(dialog.FileName);
                }
            }
        }

        private void OpenEditor(string fileName)
        {
            var editor = new EditorControl(_api, this);

            if (fileName != null)
                editor.Open(fileName);

            editor.Show(_dockPanel, DockState.Document);
        }

        private void _dockPanel_ActiveDocumentChanged(object sender, EventArgs e)
        {
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            bool haveDocument = _dockPanel.ActiveDocument != null;

            _toolStrip.Enabled = haveDocument;
            _fileSave.Enabled = haveDocument;
            _fileSaveAs.Enabled = haveDocument;
            _fileClose.Enabled = haveDocument;
            _windowNextTab.Enabled = haveDocument;
            _windowPreviousTab.Enabled = haveDocument;
        }

        private void _fileSave_Click(object sender, EventArgs e)
        {
            ActiveDocument.PerformSave();
        }

        private void _fileSaveAs_Click(object sender, EventArgs e)
        {
            ActiveDocument.PerformSaveAs();
        }

        private void _fileClose_Click(object sender, EventArgs e)
        {
            ActiveDocument.Close();
        }

        private void _run_Click(object sender, EventArgs e)
        {
            ActiveDocument.DebugRun(false);
        }

        private void _startBreaked_Click(object sender, EventArgs e)
        {
            ActiveDocument.DebugStartBreaked();
        }

        private void _openDebugger_Click(object sender, EventArgs e)
        {
            ActiveDocument.DebugOpenDebugger();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            using (var form = new LoginForm(this))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    Dispose();
                    return;
                }

                _api = form.Api;
            }

            bool hadOne = false;

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (!hadOne)
                {
                    hadOne = true;
                    continue;
                }

                if (File.Exists(arg))
                    OpenEditor(arg);
            }
        }

        public void ClearOutput()
        {
            _outputControl.ClearOutput();
        }

        public void ActivateNextTab(bool forward)
        {
            var documents = new List<IDockContent>(_dockPanel.Documents);

            if (documents.Count == 0)
                return;

            int activeDocumentIndex = documents.IndexOf(ActiveDocument);

            if (activeDocumentIndex == -1)
                activeDocumentIndex = 0;
            else
            {
                activeDocumentIndex += (forward ? 1 : -1);

                if (activeDocumentIndex < 0)
                    activeDocumentIndex = documents.Count - 1;
                if (activeDocumentIndex >= documents.Count)
                    activeDocumentIndex = 0;
            }

            (documents[activeDocumentIndex] as DockContent).Show(_dockPanel);
        }

        private void _windowNextTab_Click(object sender, EventArgs e)
        {
            ActivateNextTab(true);
        }

        private void _windowPreviousTab_Click(object sender, EventArgs e)
        {
            ActivateNextTab(false);
        }

        public void SetLineColumn(int? line, int? column, int? chars)
        {
            _statusLine.Text = line.HasValue ? line.Value.ToString() : null;
            _statusCol.Text = column.HasValue ? column.Value.ToString() : null;
            _statusCh.Text = chars.HasValue ? chars.Value.ToString() : null;
		}

        public void SetStatus(string status)
        {
            if (status != null)
                status = status.Replace("&", "&&");

            _statusLabel.Text = status;
            _statusStrip.Update();
        }

        private void _fileExit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
