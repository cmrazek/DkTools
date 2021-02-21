using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class DefinitionCollection	//	 : ICollection<Definition>
	{
		private Dictionary<string, LinkedList<Definition>> _defs = new Dictionary<string, LinkedList<Definition>>();
		private int _count;

		public DefinitionCollection()
		{
		}

		public int Count
		{
			get { return _count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Add(Definition item)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			if (string.IsNullOrEmpty(item.Name)) throw new InvalidOperationException("A definition must have a name");

			if (!_defs.TryGetValue(item.Name, out var list))
			{
				list = new LinkedList<Definition>();
				_defs[item.Name] = list;
			}

			list.AddFirst(item);
			_count++;

			// Case-insensitive items should be added again as lowercase
			if (!item.CaseSensitive && item.Name.HasUpper())
			{
				var nameLower = item.Name.ToLower();
				if (!_defs.TryGetValue(nameLower, out list))
				{
					list = new LinkedList<Definition>();
					_defs[nameLower] = list;
				}

				list.AddFirst(item);
				_count++;
			}
		}

		public void Add(IEnumerable<Definition> defs)
		{
			foreach (var def in defs)
			{
				if (def != null) Add(def);
			}
		}

		public void Clear()
		{
			_defs.Clear();
			_count = 0;
		}

		public bool Contains(Definition item)
		{
			LinkedList<Definition> list;
			if (_defs.TryGetValue(item.Name, out list)) return list.Contains(item);
			return false;
		}

		public void CopyTo(Definition[] array, int arrayIndex)
		{
			if (array == null) throw new ArgumentNullException("array");
			if (arrayIndex < 0) throw new ArgumentOutOfRangeException("array");
			if (arrayIndex + _count > array.Length) throw new ArgumentException("Number of items is greater than the length of array.");

			foreach (var list in _defs.Values)
			{
				foreach (var def in list)
				{
					array[arrayIndex++] = def;
				}
			}
		}

		public IEnumerable<Definition> this[string name]
		{
			get
			{
				LinkedList<Definition> list;
				if (_defs.TryGetValue(name, out list))
				{
					foreach (var item in list) yield return item;
				}

				if (name.HasUpper() && _defs.TryGetValue(name.ToLower(), out list))
				{
					foreach (var item in list)
					{
						if (!item.CaseSensitive) yield return item;
					}
				}
			}
		}

		public IEnumerable<Definition> All
		{
			get
			{
				foreach (var list in _defs.Values)
				{
					foreach (var def in list) yield return def;
				}
			}
		}

		public IEnumerable<Definition> Get(string name)
		{
			LinkedList<Definition> list;
			if (_defs.TryGetValue(name, out list))
			{
				foreach (var item in list) yield return item;
			}

			if (name.HasUpper() && _defs.TryGetValue(name.ToLower(), out list))
			{
				foreach (var item in list)
				{
					if (!item.CaseSensitive) yield return item;
				}
			}
		}

		public IEnumerable<T> Get<T>(string name) where T: Definition
		{
			LinkedList<Definition> list;
			if (_defs.TryGetValue(name, out list))
			{
				foreach (var def in list)
				{
					if (def is T) yield return def as T;
				}
			}

			if (name.HasUpper() && _defs.TryGetValue(name.ToLower(), out list))
			{
				foreach (var def in list)
				{
					if (def is T && !def.CaseSensitive) yield return def as T;
				}
			}
		}

		public IEnumerable<T> Get<T>() where T : Definition
		{
			foreach (var list in _defs.Values)
			{
				foreach (var def in list)
				{
					if (def is T) yield return def as T;
				}
			}
		}

		public void RemoveAll<T>()
		{
			List<string> keysToRemove = null;
			List<Definition> defsToRemove = null;

			foreach (var kv in _defs)
			{
				foreach (var def in kv.Value)
				{
					if (def is T)
					{
						if (defsToRemove == null) defsToRemove = new List<Definition>();
						defsToRemove.Add(def);
					}
				}

				if (defsToRemove != null && defsToRemove.Count > 0)
				{
					foreach (var def in defsToRemove) kv.Value.Remove(def);
					defsToRemove.Clear();

					if (kv.Value.Count == 0)
					{
						if (keysToRemove == null) keysToRemove = new List<string>();
						keysToRemove.Add(kv.Key);
					}
				}
			}

			if (keysToRemove != null && keysToRemove.Count > 0)
			{
				foreach (var key in keysToRemove) _defs.Remove(key);
			}
		}
	}
}
