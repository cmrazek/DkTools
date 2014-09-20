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
			LinkedList<Definition> list;
			if (!_defs.TryGetValue(item.Name, out list))
			{
				list = new LinkedList<Definition>();
				_defs[item.Name] = list;
			}

			list.AddFirst(item);
			_count++;
		}

		public void Add(IEnumerable<Definition> defs)
		{
			foreach (var def in defs) Add(def);
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

		public bool Remove(Definition item)
		{
			LinkedList<Definition> list;
			if (_defs.TryGetValue(item.Name, out list)) return list.Remove(item);
			return false;
		}

		public IEnumerable<Definition> this[string name]
		{
			get
			{
				LinkedList<Definition> list;
				if (_defs.TryGetValue(name, out list)) return list;
				return new Definition[0];
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
			if (_defs.TryGetValue(name, out list)) return list;
			return new Definition[0];
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
	}
}
