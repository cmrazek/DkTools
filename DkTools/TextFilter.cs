using System;
using System.Collections.Generic;
using System.Text;

namespace DkTools
{
	internal class TextFilter
	{
		private string _filter;
		private List<string> _words = new List<string>();

		public TextFilter(string filter)
		{
			_filter = filter;
			foreach (string word in filter.Split(' '))
			{
				string w = word.Trim();
				if (!string.IsNullOrEmpty(w)) _words.Add(w);
			}
		}

		public bool Match(string text)
		{
			if (_words.Count == 0) return true;

			foreach (string word in _words)
			{
				if (text.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) < 0) return false;
			}
			return true;
		}

		public string Filter
		{
			get { return _filter; }
		}
	}
}
