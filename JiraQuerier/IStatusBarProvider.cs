using System;
using System.Collections.Generic;
using System.Text;

namespace JiraQuerier
{
    public interface IStatusBarProvider
    {
        void SetLineColumn(int? line, int? column, int? chars);
        void SetStatus(string status);
    }
}
