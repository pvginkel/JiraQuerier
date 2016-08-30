using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JintDebugger;
using System.Windows.Forms;

namespace JiraQuerier
{
    public class MainForm : JavaScriptForm
    {
        private JiraApi _api;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            using (var form = new LoginForm(this))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    Dispose();
                    return;
                }

                _api = form.Api;
            }
        }

        protected override void OnEngineCreated(EngineCreatedEventArgs e)
        {
            base.OnEngineCreated(e);

            e.Engine.SetValue("api", _api);
            // e.Engine.SetValue("require", new Func<string, object>(RequireFunction));
        }
    }
}
