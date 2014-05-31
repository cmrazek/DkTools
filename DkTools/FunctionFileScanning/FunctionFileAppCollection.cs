using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.FunctionFileScanning.FunctionFileDatabase;

namespace DkTools.FunctionFileScanning
{
	internal class FunctionFileAppCollection : ICollection<FunctionFileApp>, IDisposable
	{
		private Dictionary<string, FunctionFileApp> _apps = new Dictionary<string, FunctionFileApp>();

		public int Count
		{
			get { return _apps.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Add(FunctionFileScanner scanner, Application_t app)
		{
			_apps.Add(app.name, new FunctionFileApp(scanner, app));
		}

		public void Add(FunctionFileApp item)
		{
			_apps[item.Name] = item;
		}

		public void Clear()
		{
			_apps.Clear();
		}

		public bool Contains(FunctionFileApp item)
		{
			return _apps.ContainsValue(item);
		}

		public void CopyTo(FunctionFileApp[] array, int arrayIndex)
		{
			_apps.Values.CopyTo(array, arrayIndex);
		}

		public bool Remove(FunctionFileApp item)
		{
			return _apps.Remove(item.Name);
		}

		public void Dispose()
		{
			if (_apps != null)
			{
				foreach (var app in _apps.Values) app.Dispose();
				_apps.Clear();
				_apps = null;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _apps.GetEnumerator();
		}

		IEnumerator<FunctionFileApp> IEnumerable<FunctionFileApp>.GetEnumerator()
		{
			return _apps.Values.GetEnumerator();
		}

		public FunctionFileApp this[string name]
		{
			get
			{
				FunctionFileApp app;
				if (_apps.TryGetValue(name, out app)) return app;
				return null;
			}
			set
			{
				_apps[name] = value;
			}
		}

		public IEnumerable<FunctionFileApp> Values
		{
			get { return _apps.Values; }
		}

		public FunctionFileApp TryGet(string name)
		{
			FunctionFileApp app;
			if (_apps.TryGetValue(name, out app)) return app;
			return null;
		}
	}
}
