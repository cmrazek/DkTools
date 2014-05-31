using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DkTools.FunctionFileScanning
{
	internal class FileSystemWatcherCollection : ICollection<FileSystemWatcher>, IDisposable
	{
		private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

		public void Dispose()
		{
			if (_watchers != null)
			{
				foreach (var w in _watchers) w.Dispose();
				_watchers = null;
			}
		}

		public int Count { get { return _watchers.Count; } }

		public bool IsReadOnly { get { return false; } }

		public void Add(FileSystemWatcher item)
		{
			_watchers.Add(item);
		}

		public void Clear()
		{
			_watchers.Clear();
		}

		public bool Contains(FileSystemWatcher item)
		{
			return _watchers.Contains(item);
		}

		public void CopyTo(FileSystemWatcher[] array, int arrayIndex)
		{
			_watchers.CopyTo(array, arrayIndex);
		}

		public bool Remove(FileSystemWatcher item)
		{
			return _watchers.Remove(item);
		}

		public bool ContainsPath(string path)
		{
			return (from w in _watchers where string.Equals(w.Path, path, StringComparison.OrdinalIgnoreCase) select w).Any();
		}

		public IEnumerator<FileSystemWatcher> GetEnumerator()
		{
			return _watchers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _watchers.GetEnumerator();
		}
	}
}
