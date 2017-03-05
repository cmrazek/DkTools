using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools
{
	internal sealed class GroupedList<K,V>
	{
		private Dictionary<K, List<V>> _dict = new Dictionary<K, List<V>>();

		public void Add(K key, V value)
		{
			List<V> list;
			if (_dict.TryGetValue(key, out list))
			{
				if (list == null)
				{
					list = new List<V>();
					_dict[key] = list;
				}
				list.Add(value);
			}
			else
			{
				list = new List<V>();
				list.Add(value);
				_dict[key] = list;
			}
		}

		public IEnumerable<V> this[K key]
		{
			get
			{
				List<V> list;
				if (_dict.TryGetValue(key, out list) && list != null) return list;
				return new V[0];
			}
		}

		public IEnumerable<V> Values
		{
			get
			{
				foreach (var node in _dict.Values)
				{
					if (node == null) continue;
					foreach (var value in node)
					{
						yield return value;
					}
				}
			}
		}
	}
}
