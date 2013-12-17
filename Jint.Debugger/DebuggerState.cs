using System;
using System.Collections.Generic;
using System.Text;

namespace Jint.Debugger
{
    [Flags]
    internal enum DebuggerState
    {
        None = 0,
        CanContinue = 1,
        CanStep = 2
    }
}
