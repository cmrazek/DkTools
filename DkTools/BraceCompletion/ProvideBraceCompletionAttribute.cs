using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.BraceCompletion
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal sealed class ProvideBraceCompletionAttribute : RegistrationAttribute
    {
        private string _languageName;

        public ProvideBraceCompletionAttribute(string languageName)
        {
            _languageName = languageName;
        }

        public override void Register(RegistrationContext context)
        {
            var key = context.CreateKey(@"Languages\Language Services\" + _languageName);
            key.SetValue("ShowBraceCompletion", 1);
        }

        public override void Unregister(RegistrationContext context)
        {
        }
    }
}
