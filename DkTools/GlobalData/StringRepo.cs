using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.GlobalData
{
	internal class StringRepo
	{
		private List<string> _strings;
		private List<int> _index;

		public StringRepo()
		{
			_strings = new List<string>();
			_index = new List<int>();

			_strings.Add(string.Empty);
			_index.Add(0);
		}

		public StringRepo(int initialCapacity)
		{
			_strings = new List<string>(initialCapacity);
			_index = new List<int>(initialCapacity);

			_strings.Add(string.Empty);
			_index.Add(0);
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write(_strings.Count - 1);
			foreach (var str in _strings.Skip(1)) writer.Write(str);
		}

		public void Read(BinaryReader rdr)
		{
			var count = rdr.ReadInt32();
			if (count < 0) throw new InvalidRepoException("Invalid number of strings.");
			while (count-- != 0)
			{
				var str = rdr.ReadString();
				var id = _strings.Count;
				_strings.Add(str);
				AddToIndex(str, id);
			}
		}

		public int Count => _strings.Count;

		public int Store(string str)
		{
			if (string.IsNullOrEmpty(str)) return 0;

			var newId = _strings.Count;
			var min = 0;
			var max = _index.Count - 1;
			int mid;
			int result;

			while (true)
			{
				if (min == max)
				{
					result = string.Compare(_strings[_index[min]], str);
					if (result > 0)
					{
						_strings.Add(str);
						_index.Insert(min, newId);	// Insert before current item
						return newId;
					}
					if (result < 0)
					{
						_strings.Add(str);
						_index.Insert(min + 1, newId);	// Insert after current item
						return newId;
					}
					return _index[min];	// String already exists; return ID of current item.
				}

				mid = (min + max) / 2;
				result = string.Compare(_strings[_index[mid]], str);
				if (result > 0)
				{
					max = mid - 1;  // Search lower half next
					if (max < min) max = min;
				}
				else if (result < 0)
				{
					min = mid + 1; // Search upper half next
					if (min > max) min = max;
				}
				else return _index[mid];	// String already exists
			}
		}

		public int GetId(string str)
		{
			if (string.IsNullOrEmpty(str)) return 0;

			var min = 0;
			var max = _index.Count - 1;
			int mid;
			int result;

			while (true)
			{
				if (min == max) return string.Compare(_strings[_index[min]], str) == 0 ? _index[min] : -1;
				mid = (min + max) / 2;
				result = string.Compare(_strings[_index[mid]], str);
				if (result > 0)
				{
					max = mid - 1;  // Search lower half next
					if (max < min) max = min;
				}
				else if (result < 0)
				{
					min = mid + 1; // Search upper half next
					if (min > max) min = max;
				}
				else return _index[mid];	// String already exists
			}
		}

		public string GetString(int id)
		{
			if (id < 0 || id >= _strings.Count) throw new ArgumentOutOfRangeException(nameof(id));
			return _strings[id];
		}

		private void AddToIndex(string str, int id)
		{
			var min = 0;
			var max = _index.Count - 1;
			int mid;
			int result;

			while (true)
			{
				if (min == max)
				{
					result = string.Compare(_strings[_index[min]], str);
					_index.Insert(result < 0 ? min + 1 : min, id);
					return;
				}

				mid = (min + max) / 2;
				result = string.Compare(_strings[_index[mid]], str);
				if (result > 0)
				{
					max = mid - 1;  // Search lower half next
					if (max < min) max = min;
				}
				else if (result < 0)
				{
					min = mid + 1; // Search upper half next
					if (min > max) min = max;
				}
				else
				{
					_index.Insert(mid, id);
					return;
				}
			}
		}

		public void DumpToFile(string fileName)
		{
			using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			using (var writer = new StreamWriter(fileStream))
			{
				foreach (var i in _index) writer.WriteLine(_strings[i]);
			}
		}
	}
}
