using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using Jint.Expressions;
using Jint.Native;
using WeifenLuo.WinFormsUI.Docking;

namespace Jint.Debugger
{
    internal partial class ProgramControl : DockContent
    {
        private readonly DebuggerForm _debuggerForm;
        private DebuggerState _state;
        private Continuation _continuation;
        private DebugInformation _lastDebugInformation;
        private List<JsScope> _allowedScopes;
        private JintEngine _engine;
        private CaretMark _caretMark;

        public DebuggerState State
        {
            get { return _state; }
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged(EventArgs.Empty);
                }
            }
        }

        public event EventHandler StateChanged;

        protected virtual void OnStateChanged(EventArgs e)
        {
            var ev = StateChanged;
            if (ev != null)
                ev(this, e);
        }

        public ProgramControl(Program program, DebuggerForm debuggerForm)
        {
            if (program == null)
                throw new ArgumentNullException("program");
            if (debuggerForm == null)
                throw new ArgumentNullException("debuggerForm");

            _debuggerForm = debuggerForm;

            InitializeComponent();

            _textEditor.SetHighlighting("JavaScript");
            _textEditor.IsReadOnly = true;

            _textEditor.Text = program.ProgramSource;

            const string consolas = "Consolas";

            var font = new Font(consolas, _textEditor.Font.Size);

            if (font.FontFamily.Name == consolas)
                _textEditor.Font = font;

            _textEditor.ActiveTextAreaControl.TextArea.IconBarMargin.MouseDown += IconBarMargin_MouseDown;
        }

        void IconBarMargin_MouseDown(AbstractMargin sender, Point mousepos, MouseButtons mouseButtons)
        {
            if (mouseButtons != MouseButtons.Left)
                return;

            var textArea = _textEditor.ActiveTextAreaControl.TextArea;

            int yPos = mousepos.Y;
            int lineHeight = textArea.TextView.FontHeight;
            int lineNumber = (yPos + textArea.VirtualTop.Y) / lineHeight;

            foreach (var breakPoint in _engine.BreakPoints)
            {
                if (breakPoint.Line == lineNumber + 1)
                    return;
            }

            string text = textArea.Document.GetText(textArea.Document.GetLineSegment(lineNumber));
            int offset = -1;

            for (int i = 0; i < text.Length; i++)
            {
                if (
                    !Char.IsWhiteSpace(text[i]) &&
                    text[i] != '/'
                ) {
                    offset = i;
                    break;
                }
            }

            if (offset != -1)
            {
                _engine.BreakPoints.Add(new BreakPoint(lineNumber + 1, offset));

                _debuggerForm.ReloadAllBreakPoints();

                _textEditor.Refresh();
            }
        }

        public void ProcessStep(JintEngine engine, DebugInformation e, Continuation continuation, BreakType breakType)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (continuation == null)
                throw new ArgumentNullException("continuation");

            Debug.Assert(_continuation == null);

            if (breakType == BreakType.Step && ProcessAllowedScopes(e, continuation))
                return;

            if (_engine != engine)
            {
                _engine = engine;
                LoadBreakPoints();
            }

            _continuation = continuation;
            _lastDebugInformation = e;

            var document = _textEditor.Document;

            int start = GetOffset(e.CurrentStatement.Source.Start);
            int end = GetOffset(e.CurrentStatement.Source.Stop);

            var position = document.OffsetToPosition(start);

            document.MarkerStrategy.AddMarker(new TextMarker(start, end - start, TextMarkerType.SolidBlock, Color.Yellow));
            _textEditor.ActiveTextAreaControl.TextArea.Caret.Position = position;

            _caretMark = new CaretMark(document, position);

            document.BookmarkManager.AddMark(_caretMark);

            _textEditor.Refresh();

            State = DebuggerState.CanContinue | DebuggerState.CanStep;

            LoadLocals();
        }

        public void LoadBreakPoints()
        {
            var document = _textEditor.Document;
            document.BookmarkManager.Clear();

            foreach (var breakPoint in _engine.BreakPoints)
            {
                if (breakPoint.Line > _textEditor.Document.TotalNumberOfLines)
                    continue;

                var mark = new BreakPointMark(
                    document,
                    new TextLocation(
                        breakPoint.Char,
                        breakPoint.Line - 1
                    )
                );

                var breakPointCopy = breakPoint;

                mark.Removed += (s, e) =>
                {
                    _engine.BreakPoints.Remove(breakPointCopy);
                    _debuggerForm.ReloadAllBreakPoints();
                };

                document.BookmarkManager.AddMark(mark);
            }

            if (_caretMark != null)
                document.BookmarkManager.AddMark(_caretMark);
        }

        private bool ProcessAllowedScopes(DebugInformation e, Continuation continuation)
        {
            // Verify whether we need to step.

            if (_allowedScopes != null)
            {
                var currentScopes = new List<JsScope>(e.Scopes);

                currentScopes.Reverse();

                bool areEqual = true;

                for (int i = 0, count = Math.Min(currentScopes.Count, _allowedScopes.Count); i < count; i++)
                {
                    if (currentScopes[i] != _allowedScopes[i])
                    {
                        areEqual = false;
                        break;
                    }
                }

                bool doStep = false;

                if (!areEqual)
                {
                    // If the part of the stacks that overlap are not equal, we
                    // know for sure we need to step because we're in a different
                    // scope.

                    doStep = true;
                }
                else if (currentScopes.Count <= _allowedScopes.Count)
                {
                    // If the depth of the current stack is less than the allowed
                    // stacks, we're sure we can step because we went to a
                    // higher scope.

                    doStep = true;
                }

                if (!doStep)
                {
                    JintDebugger.BreakOnNextStatement = true;

                    continuation.Signal();
                    return true;
                }
            }

            return false;
        }

        private int GetOffset(SourceCodeDescriptor.Location location)
        {
            return _textEditor.Document.PositionToOffset(new TextLocation(
                location.Char, location.Line - 1
            ));
        }

        public void PerformContinue()
        {
            ResetState();

            Continue();
        }

        private void Continue()
        {
            _lastDebugInformation = null;

            _continuation.Signal();
            _continuation = null;
        }

        public void PerformStep(StepType stepType)
        {
            ResetState();

            if (stepType != StepType.Into)
            {
                _allowedScopes = new List<JsScope>(_lastDebugInformation.Scopes);

                _allowedScopes.Reverse();

                if (stepType == StepType.Out)
                    _allowedScopes.RemoveAt(_allowedScopes.Count - 1);
            }

            JintDebugger.BreakOnNextStatement = true;

            Continue();
        }

        private void ResetState()
        {
            _textEditor.Document.MarkerStrategy.RemoveAll(p => true);
            _textEditor.Document.BookmarkManager.RemoveMark(_caretMark);
            _caretMark = null;

            _textEditor.Refresh();

            State = 0;
            _allowedScopes = null;

            _debuggerForm.ResetTabs();
        }

        public void LoadLocals()
        {
            if (_lastDebugInformation != null)
                _debuggerForm.LoadTabs(_lastDebugInformation);
            else
                _debuggerForm.ResetTabs();
        }
    }
}
