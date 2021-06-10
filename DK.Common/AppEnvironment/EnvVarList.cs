using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DK.AppEnvironment
{
    internal class EnvVarList : ICollection<EnvVar>
    {
        private List<EnvVar> _vars = new List<EnvVar>();

        public void LoadFromEnvironment()
        {
            _vars.Clear();

            var envVars = Environment.GetEnvironmentVariables();
            foreach (var ev in envVars.Keys)
            {
                var value = envVars[ev];
                _vars.Add(new EnvVar(ev.ToString(), value.ToString()));
            }
        }

        public string this[string name]
        {
            get
            {
				var v = TryGetVariableNode(name);
				if (v != null) return v.Value;
				return string.Empty;
            }
            set
            {
                var v = TryGetVariableNode(name);
                if (v != null) v.Value = value;
                else _vars.Add(new EnvVar(name, value));
            }
        }

		public EnvVar this[int index]
		{
			get
			{
				if (index < 0 || index >= _vars.Count) throw new ArgumentOutOfRangeException();
				return _vars[index];
			}
		}

        public EnvVar GetVariableNode(string name)
        {
            foreach (var v in _vars)
            {
                if (v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return v;
            }

            throw new ArgumentOutOfRangeException("name");
        }

        public EnvVar TryGetVariableNode(string name)
        {
            foreach (var v in _vars)
            {
                if (v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return v;
            }

            return null;
        }

		public IEnumerable<string> GetDirectoryList(string name)
		{
			var v = TryGetVariableNode(name);
			if (v != null)
			{
				foreach (var dir in v.Value.Split(';'))
				{
					if (string.IsNullOrWhiteSpace(dir)) continue;
					yield return dir.Trim();
				}
			}
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern int GetShortPathName(
			[MarshalAs(UnmanagedType.LPTStr)]
			string path,
			[MarshalAs(UnmanagedType.LPTStr)]
			StringBuilder shortPath,
			int shortPathLength
			);

		public void SetDirectoryList(string name, IEnumerable<string> dirList, int pathLimit = int.MaxValue)
		{
			var sb = new StringBuilder();
			var dirs = new List<string>();
			foreach (var dir in dirList)
			{
				if (!string.IsNullOrWhiteSpace(dir))
				{
					if (sb.Length > 0) sb.Append(';');
					var trimmedDir = dir.Trim();
					sb.Append(trimmedDir);
					dirs.Add(trimmedDir);
				}
			}

			if (sb.Length > pathLimit)
			{
				sb.Clear();

				foreach (var dir in dirs)
				{
					if (Directory.Exists(dir))
					{
						var shortPath = new StringBuilder(260);
						GetShortPathName(dir, shortPath, shortPath.Capacity);

						if (sb.Length + shortPath.Length + 1 < pathLimit)
						{
							if (sb.Length > 0) sb.Append(';');
							sb.Append(shortPath.ToString());
						}
						else break;
					}
				}
			}

			this[name] = sb.ToString();
		}

		public int Count
		{
			get { return _vars.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Add(EnvVar item)
		{
			this[item.Name] = item.Value;
		}

		public void Clear()
		{
			_vars.Clear();
		}

		public bool Contains(EnvVar item)
		{
			foreach (var v in _vars)
			{
				if (v.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		public void CopyTo(EnvVar[] array, int arrayIndex)
		{
			_vars.CopyTo(array, arrayIndex);
		}

		public bool Remove(EnvVar item)
		{
			EnvVar itemToRemove = null;
			foreach (var v in _vars)
			{
				if (v.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase))
				{
					itemToRemove = v;
					break;
				}
			}

			if (itemToRemove != null) return _vars.Remove(itemToRemove);
			return false;
		}

		public IEnumerator<EnvVar> GetEnumerator()
		{
			return new EnvVarEnumerator(this);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new EnvVarEnumerator(this);
		}

		public void Sort()
		{
			_vars.Sort((a, b) => string.Compare(a.Name, b.Name, true));
		}
    }

	internal class EnvVar
	{
		public string Name { get; private set; }
		public string Value { get; set; }

		public EnvVar(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}

	internal class EnvVarEnumerator : IEnumerator<EnvVar>
	{
		private EnvVarList _list;
		private int _index;

		public EnvVarEnumerator(EnvVarList list)
		{
			_list = list;
			_index = -1;
		}

		public EnvVar Current
		{
			get { return _list[_index]; }
		}

		object System.Collections.IEnumerator.Current
		{
			get { return _list[_index]; }
		}

		public bool MoveNext()
		{
			if (_index + 1 < _list.Count)
			{
				_index++;
				return true;
			}

			return false;
		}

		public void Reset()
		{
			_index = 0;
		}

		public void Dispose()
		{
			_list = null;
		}
	}
}
