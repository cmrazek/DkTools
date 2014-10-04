using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace DkTools
{
	internal class MefServices
	{
		private Dictionary<Type, object> _services = new Dictionary<Type, object>();

		private static MefServices GetForTextView(IWpfTextView wpfView)
		{
			if (wpfView == null) return null;

			MefServices svc;
			if (wpfView.Properties.TryGetProperty<MefServices>(typeof(MefServices), out svc) && svc != null) return svc;

			svc = new MefServices();
			wpfView.Properties[typeof(MefServices)] = svc;
			return svc;
		}

		public static MefServices GetForTextBuffer(ITextBuffer buf)
		{
			if (buf == null) return null;

			MefServices svc;
			if (buf.Properties.TryGetProperty<MefServices>(typeof(MefServices), out svc) && svc != null) return svc;

			svc = new MefServices();
			buf.Properties[typeof(MefServices)] = svc;
			return svc;
		}

		public T GetService<T>()
		{
			object obj;
			if (_services.TryGetValue(typeof(T), out obj)) return (T)obj;
			return default(T);
		}

		public void SetService<T>(T svc)
		{
			_services[typeof(T)] = svc;
		}
	}
}
