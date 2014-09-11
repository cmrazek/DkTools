using System;
using System.Collections.Generic;
using System.Text;

namespace DkTools
{
	internal class TextFilter
	{
		private string _filter;
		private List<SearchTerm> _searchTerms = new List<SearchTerm>();

		public TextFilter()
		{ }

		public TextFilter(string filter)
		{
			this.Filter = filter;
		}

		public bool Match(string text)
		{
			if (_searchTerms.Count == 0) return true;
			if (string.IsNullOrWhiteSpace(text)) return false;

			foreach (var term in _searchTerms)
			{
				if (!term.Match(text)) return false;
			}
			return true;
		}

		public string Filter
		{
			get { return _filter; }
			set
			{
				_searchTerms.Clear();
				_filter = value == null ? string.Empty : value;
				foreach (string termText in _filter.Split(' ', '\t', '\r', '\n'))
				{
					if (string.IsNullOrWhiteSpace(termText)) continue;

					var term = new SearchTerm();
					foreach (string word in termText.Split('|'))
					{
						if (string.IsNullOrWhiteSpace(word)) continue;
						term.words.Add(word);
					}

					if (term.words.Count > 0) _searchTerms.Add(term);
				}
			}
		}

		public bool IsEmpty
		{
			get { return _searchTerms.Count == 0; }
		}

		private class SearchTerm
		{
			public List<string> words = new List<string>();

			public bool Match(string text)
			{
				foreach (var word in words)
				{
					if (text.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) >= 0) return true;
				}
				return false;
			}
		}
	}
}
