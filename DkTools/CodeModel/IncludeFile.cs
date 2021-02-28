using DkTools.CodeModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	sealed class IncludeFile
	{
		private FileStore _store;
		private string _fullPathName;
		private CodeSource _source;
		private CodeFile _codeFile;
		private DateTime _lastCheck;
		private DateTime _lastModifiedDate;
		private Dictionary<string, string> _preMergeContent = new Dictionary<string, string>();

		public IncludeFile(FileStore store, string fileName)
		{
			if (store == null) throw new ArgumentNullException("store");
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

			_store = store;
			_fullPathName = fileName;
		}

		public CodeSource GetSource(ProbeAppSettings appSettings)
		{
			if (_source == null)
			{
				_source = IncludeFileCache.GetIncludeFileSource(_fullPathName);
				if (_source == null)
				{
					_source = new CodeSource();
					_source.Append(string.Empty, _fullPathName, 0, 0, true, false, false);
					_source.Flush();
				}
			}
			return _source;
		}

		public string FullPathName
		{
			get { return _fullPathName; }
		}

		public IEnumerable<string> PreMergeFileNames
		{
			get
			{
				return _preMergeContent.Keys;
			}
		}

		public string GetPreMergeContent(string fileName)
		{
			string content;
			if (_preMergeContent.TryGetValue(fileName.ToLower(), out content)) return content;
			return null;
		}
	}
}
