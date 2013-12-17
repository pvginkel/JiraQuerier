using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Jint.Debugger
{
    public static class JintDebugger
    {
        private static readonly object _syncRoot = new object();
        private static DebuggerForm _form;
        private static bool _breakOnNextStatement;

        public static bool BreakOnNextStatement
        {
            get { return _breakOnNextStatement; }
            set
            {
                if (_breakOnNextStatement != value)
                {
                    _breakOnNextStatement = value;
                    OnBreakOnNextStatementChanged(EventArgs.Empty);
                }
            }
        }

        internal static event EventHandler BreakOnNextStatementChanged;

        private static void OnBreakOnNextStatementChanged(EventArgs e)
        {
            var ev = BreakOnNextStatementChanged;
            if (ev != null)
                ev(null, EventArgs.Empty);
        }

        public static JintEngine CreateEngine()
        {
            var engine = new JintEngine();

            engine.SetDebugMode(true);

            engine.Break += engine_Break;
            engine.Step += engine_Step;

            return engine;
        }

        static void engine_Break(object sender, DebugInformation e)
        {
            ProcessStep(sender, e, BreakType.Break);
        }

        static void engine_Step(object sender, DebugInformation e)
        {
            ProcessStep(sender, e, BreakType.Step);
        }

        private static void ProcessStep(object sender, DebugInformation e, BreakType breakType)
        {
            lock (_syncRoot)
            {
                if (breakType == BreakType.Step && !BreakOnNextStatement)
                    return;

                BreakOnNextStatement = false;

                ShowDebugger();

                using (var continuation = _form.ProcessStep((JintEngine)sender, e, breakType))
                {
                    continuation.Wait();
                }
            }
        }

        public static void ShowDebugger()
        {
            lock (_syncRoot)
            {
                if (TryActivate())
                    return;

                using (var @event = new ManualResetEvent(false))
                {
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            _form = new DebuggerForm();

                            try
                            {
                                _form.Shown += (s, e) => @event.Set();

                                Application.Run(_form);
                            }
                            finally
                            {
                                var form = _form;
                                _form = null;

                                form.Dispose();
                            }
                        }
                        catch
                        {
                            // Ignore exceptions from the debugger.
                        }
                    });

                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();

                    @event.WaitOne();
                }
            }
        }

        private static bool TryActivate()
        {
            if (_form == null)
                return false;

            try
            {
                _form.BeginInvoke(new MethodInvoker(_form.Activate));
            }
            catch
            {
                // Ignore exceptions for disposed form.
            }

            return true;
        }
    }
}
