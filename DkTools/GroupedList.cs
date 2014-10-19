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
				if (_dict.TryGetValue(key, out list)) return list;
				return new V[0];
			}
		}

		public IEnumerable<V> Values
		{
			get
			{
				foreach (var node in _dict)
				{
					foreach (var value in node.Value)
					{
						yield return value;
					}
				}
			}
		}
	}
}
