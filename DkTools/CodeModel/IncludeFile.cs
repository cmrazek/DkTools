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
				try
				{
					var merger = new FileMerger();
					merger.MergeFile(appSettings, _fullPathName, null, false, false);
					_source = merger.MergedContent;

					var fileInfo = new FileInfo(_fullPathName);
					_lastModifiedDate = fileInfo.LastWriteTime;
					_lastCheck = DateTime.Now;

					foreach (var mergeFileName in merger.FileNames)
					{
						var content = merger.GetFileContent(mergeFileName);
						if (content == null) throw new InvalidOperationException("Merger content is null.");
						_preMergeContent[mergeFileName.ToLower()] = content;
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when attempting to read content of include file '{0}'.", _fullPathName);
					_source = null;
				}
			}
			return _source;
		}

		public CodeFile GetCodeFile(ProbeAppSettings appSettings, CodeModel model, IEnumerable<string> parentFiles)
		{
			if (_codeFile == null)
			{
				try
				{
					Log.Debug("Processing include file: {0}", _fullPathName);

					var content = GetSource(appSettings);
					if (content == null) return null;

					var file = new CodeFile(model);
					file.Parse(content, _fullPathName, parentFiles, false);
					_codeFile = file;
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when processing include file '{0}'.", _fullPathName);
					_codeFile = null;
				}

				_lastCheck = DateTime.Now;
			}
			return _codeFile;
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
