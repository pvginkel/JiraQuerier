using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Jint.Delegates;
using Jint.Native;
using WeifenLuo.WinFormsUI.Docking;

namespace Jint.Debugger
{
    internal partial class DebuggerForm : SystemEx.Windows.Forms.Form
    {
        private int _programCounter;
        private readonly Dictionary<string, ProgramControl> _controls = new Dictionary<string, ProgramControl>();
        private readonly VariablesControl _localsControl;
        private readonly VariablesControl _globalsControl;
        private readonly CallStackControl _callStack;

        protected ProgramControl ActiveProgram
        {
            get
            {
                if (_dockPanel.ActiveDocumentPane != null)
                    return _dockPanel.ActiveDocumentPane.ActiveContent as ProgramControl;

                return null;
            }
        }

        public DebuggerForm()
        {
            InitializeComponent();

            JintDebugger.BreakOnNextStatementChanged += JintDebugger_BreakOnNextStatementChanged;

            _dockPanel.Theme = new VS2012LightTheme();

            Disposed += DebuggerForm_Disposed;

            _localsControl = new VariablesControl
            {
                Text = "Locals"
            };

            _localsControl.Show(_dockPanel, DockState.DockBottom);

            _globalsControl = new VariablesControl
            {
                Text = "Globals"
            };

            _globalsControl.Show(_dockPanel, DockState.DockBottom);

            _callStack = new CallStackControl();

            _callStack.Show(_dockPanel, DockState.DockBottom);

            _localsControl.DockHandler.Activate();

            ResetTabs();
        }

        void DebuggerForm_Disposed(object sender, EventArgs e)
        {
            JintDebugger.BreakOnNextStatementChanged -= JintDebugger_BreakOnNextStatementChanged;
        }

        void JintDebugger_BreakOnNextStatementChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
                BeginInvoke(new Action<bool>(DoEnableBreak), !JintDebugger.BreakOnNextStatement);
            else
                DoEnableBreak(!JintDebugger.BreakOnNextStatement);
        }

        private void DoEnableBreak(bool enabled)
        {
            _break.Enabled = enabled;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F5: _continue.PerformClick(); break;
                case Keys.F10: _stepOver.PerformClick(); break;
                case Keys.F11: _stepInto.PerformClick(); break;
                case Keys.Shift | Keys.F11: _stepOut.PerformClick(); break;
                default: return base.ProcessCmdKey(ref msg, keyData);
            }

            return true;
        }

        private void _break_Click(object sender, EventArgs e)
        {
            JintDebugger.BreakOnNextStatement = true;
        }

        internal Continuation ProcessStep(JintEngine engine, DebugInformation e, BreakType breakType)
        {
            var continuation = new Continuation();

            if (InvokeRequired)
                BeginInvoke(new Action<JintEngine, DebugInformation, Continuation, BreakType>(DoProcessStep), engine, e, continuation, breakType);
            else
                DoProcessStep(engine, e, continuation, breakType);

            return continuation;
        }

        private void DoProcessStep(JintEngine engine, DebugInformation e, Continuation continuation, BreakType breakType)
        {
            ProgramControl control;

            if (!_controls.TryGetValue(e.Program.ProgramSource, out control))
            {
                control = new ProgramControl(e.Program, this);

                control.StateChanged += control_StateChanged;
                control.Text += "Program " + ++_programCounter;

                control.Disposed += (s, ea) => _controls.Remove(e.Program.ProgramSource);

                _controls.Add(e.Program.ProgramSource, control);

                control.Show(_dockPanel, DockState.Document);
            }
            else
            {
                control.DockHandler.Activate();
            }

            control.ProcessStep(engine, e, continuation, breakType);
        }

        void control_StateChanged(object sender, EventArgs e)
        {
            if (_dockPanel.ActiveDocument == sender)
                UpdateFromActiveDocument();
        }

        private void _dockPanel_ActiveDocumentChanged(object sender, EventArgs e)
        {
            UpdateFromActiveDocument();
        }

        private void UpdateFromActiveDocument()
        {
            DebuggerState state = 0;

            if (ActiveProgram != null)
            {
                state = ActiveProgram.State;
                ActiveProgram.LoadLocals();
            }
            else
            {
                ResetTabs();
            }

            _continue.Enabled = (state & DebuggerState.CanContinue) != 0;

            _stepInto.Enabled =
            _stepOut.Enabled =
            _stepOver.Enabled =
                (state & DebuggerState.CanStep) != 0;
        }

        private void _continue_Click(object sender, EventArgs e)
        {
            ActiveProgram.PerformContinue();
        }

        private void _stepInto_Click(object sender, EventArgs e)
        {
            ActiveProgram.PerformStep(StepType.Into);
        }

        private void _stepOver_Click(object sender, EventArgs e)
        {
            ActiveProgram.PerformStep(StepType.Over);
        }

        private void _stepOut_Click(object sender, EventArgs e)
        {
            ActiveProgram.PerformStep(StepType.Out);
        }

        public void LoadTabs(DebugInformation debug)
        {
            _localsControl.LoadVariables(debug, VariablesMode.Locals);
            _globalsControl.LoadVariables(debug, VariablesMode.Globals);
            _callStack.LoadCallStack(debug);
        }

        public void ResetTabs()
        {
            _localsControl.ResetVariables();
            _globalsControl.ResetVariables();
            _callStack.ResetCallStack();
        }

        public void ReloadAllBreakPoints()
        {
            foreach (var control in _controls.Values)
            {
                control.LoadBreakPoints();
            }
        }

        private delegate void Action<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }
}
