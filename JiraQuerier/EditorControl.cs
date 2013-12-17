using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Jint;
using Jint.Debugger;
using Jint.Delegates;
using WeifenLuo.WinFormsUI.Docking;

namespace JiraQuerier
{
    internal partial class EditorControl : DockContent
    {
        private readonly JiraApi _api;
        private readonly IStatusBarProvider _statusBarProvider;
        private string _tabText;
        private JintEngine _engine;

        public string FileName { get; private set; }
        public bool IsDirty { get; private set; }

        public EditorControl(JiraApi api, IStatusBarProvider statusBarProvider)
        {
            if (api == null)
                throw new ArgumentNullException("api");
            if (statusBarProvider == null)
                throw new ArgumentNullException("statusBarProvider");

            _api = api;
            _statusBarProvider = statusBarProvider;

            Font = SystemFonts.MessageBoxFont;

            InitializeComponent();

            _textEditor.SetHighlighting("JavaScript");

            const string consolas = "Consolas";

            var font = new Font(consolas, _textEditor.Font.Size);

            if (font.FontFamily.Name == consolas)
                _textEditor.Font = font;

            _textEditor.ActiveTextAreaControl.Caret.PositionChanged += Caret_PositionChanged;

            SetTabText("New File");
        }

        void Caret_PositionChanged(object sender, EventArgs e)
        {
            UpdateLineCol();
        }

        private void SetTabText(string tabText)
        {
            _tabText = tabText;

            if (IsDirty)
                tabText += "*";

            Text = TabText = tabText;
        }

        internal void Open(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            FileName = fileName;

            SetTabText(Path.GetFileName(fileName));

            _textEditor.Text = File.ReadAllText(fileName);

            SetDirty(false);
        }

        private void SetDirty(bool isDirty)
        {
            if (IsDirty != isDirty)
            {
                IsDirty = isDirty;
                SetTabText(_tabText);
            }
        }

        private void _textEditor_TextChanged(object sender, EventArgs e)
        {
            SetDirty(true);
        }

        internal void Save(string fileName)
        {
            if (fileName != null)
            {
                FileName = fileName;
                SetTabText(Path.GetFileName(fileName));
            }

            File.WriteAllText(FileName, _textEditor.Text);
            SetDirty(false);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F5:
                    DebugRun(true);
                    break;

                case Keys.F10:
                case Keys.F11:
                case Keys.Shift | Keys.F11:
                    DebugStartBreaked();
                    break;

                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }

            return true;
        }

        public bool PerformSave()
        {
            if (FileName == null)
                return PerformSaveAs();

            Save(null);

            return true;
        }

        public bool PerformSaveAs()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.AddExtension = true;
                dialog.CheckPathExists = true;
                dialog.Filter = MainForm.FileDialogFilter;
                dialog.OverwritePrompt = true;
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    Save(dialog.FileName);
                    return true;
                }
            }

            return false;
        }

        internal void DebugRun(bool debug)
        {
            PerformSave();

            try
            {
                ((MainForm)Parent.FindForm()).ClearOutput();

                _engine = JintDebugger.CreateEngine();

                _engine.SetDebugMode(debug);
                _engine.DisableSecurity();
                _engine.AllowClr();
                _engine.SetParameter("api", _api);
                _engine.SetFunction("require", new Func<string, object>(RequireFunction));

                _engine.Run(_textEditor.Text);

                _engine = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "An exception occurred while executing the script:" + Environment.NewLine +
                    Environment.NewLine +
                    ex.Message + " (" + ex.GetType().FullName + ")" + Environment.NewLine +
                    Environment.NewLine +
                    ex.StackTrace,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private object RequireFunction(string fileName)
        {
            if (!Path.IsPathRooted(fileName))
                fileName = Path.Combine(Path.GetDirectoryName(FileName), fileName);

            return _engine.Run(File.ReadAllText(fileName));
        }

        private void EditorControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
                return;

            e.Cancel = !PerformSaveIfDirty();
        }

        private bool PerformSaveIfDirty()
        {
            if (IsDirty)
            {
                var result = MessageBox.Show(
                    this,
                    "Do you want to save your changes?",
                    Text,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning
                );

                switch (result)
                {
                    case DialogResult.Yes:
                        if (!PerformSave())
                            return false;
                        break;

                    case DialogResult.Cancel:
                        return false;
                }
            }

            return true;
        }

        internal void DebugStartBreaked()
        {
            JintDebugger.BreakOnNextStatement = true;
            DebugRun(true);
        }

        internal void DebugOpenDebugger()
        {
            JintDebugger.ShowDebugger();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            UpdateLineCol();
        }

        private void UpdateLineCol()
        {
            var position = _textEditor.ActiveTextAreaControl.Caret.Position;
            var line = _textEditor.Document.GetLineSegment(position.Line);
            int chars = _textEditor.Document.GetText(line).Substring(0, position.Column).Replace("\t", "    ").Length;

            _statusBarProvider.SetLineColumn(
                position.Line + 1, chars + 1, position.Column + 1
            );
        }
    }
}
