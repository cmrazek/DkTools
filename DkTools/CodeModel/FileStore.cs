using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	internal class FileStore
	{
		private Dictionary<string, IncludeFile> _sameDirIncludeFiles = new Dictionary<string, IncludeFile>();
		private Dictionary<string, IncludeFile> _globalIncludeFiles = new Dictionary<string, IncludeFile>();
		private CodeModel _model;	// Model based on the most recent snapshot.

		public FileStore()
		{
		}

		public static FileStore GetOrCreateForTextBuffer(VsText.ITextBuffer buf)
		{
			if (buf == null) throw new ArgumentNullException("buf");

			FileStore cache;
			if (buf.Properties.TryGetProperty(typeof(FileStore), out cache)) return cache;

			cache = new FileStore();
			buf.Properties[typeof(FileStore)] = cache;

			return cache;
		}

		public IncludeFile GetIncludeFile(string sourceFileName, string fileName, bool searchSameDir, IEnumerable<string> parentFiles)
		{
			if (string.IsNullOrEmpty(fileName)) return null;

			IncludeFile file = null;
			var fileNameLower = fileName.ToLower();

			if (searchSameDir)
			{
				if (!string.IsNullOrEmpty(sourceFileName))
				{
					if (_sameDirIncludeFiles.TryGetValue(fileNameLower, out file)) return file;

					var pathName = Path.Combine(Path.GetDirectoryName(sourceFileName), fileName);
					if (System.IO.File.Exists(pathName))
					{
						if (CheckForCyclicalInclude(pathName, parentFiles)) return null;
						file = new IncludeFile(this, pathName);
						_sameDirIncludeFiles[fileNameLower] = file;
						return file;
					}

					var ampersandFileName = pathName + "&";
					if (System.IO.File.Exists(ampersandFileName))
					{
						if (CheckForCyclicalInclude(ampersandFileName, parentFiles)) return null;
						file = new IncludeFile(this, ampersandFileName);
						_sameDirIncludeFiles[fileNameLower] = file;
						return file;
					}

					var plusFileName = pathName = "+";
					if (File.Exists(plusFileName))
					{
						if (CheckForCyclicalInclude(plusFileName, parentFiles)) return null;
						file = new IncludeFile(this, plusFileName);
						_sameDirIncludeFiles[fileNameLower] = file;
						return file;
					}
				}
			}

			if (_globalIncludeFiles.TryGetValue(fileNameLower, out file)) return file;

			foreach (var includeDir in ProbeEnvironment.IncludeDirs)
			{
				var pathName = Path.Combine(includeDir, fileName);
				if (System.IO.File.Exists(pathName))
				{
					if (CheckForCyclicalInclude(pathName, parentFiles)) return null;
					file = new IncludeFile(this, pathName);
					_globalIncludeFiles[fileNameLower] = file;
					return file;
				}

				var ampersandFileName = pathName + "&";
				if (System.IO.File.Exists(ampersandFileName))
				{
					if (CheckForCyclicalInclude(ampersandFileName, parentFiles)) return null;
					file = new IncludeFile(this, ampersandFileName);
					_globalIncludeFiles[fileNameLower] = file;
					return file;
				}

				var plusFileName = pathName = "+";
				if (System.IO.File.Exists(plusFileName))
				{
					if (CheckForCyclicalInclude(plusFileName, parentFiles)) return null;
					file = new IncludeFile(this, plusFileName);
					_globalIncludeFiles[fileNameLower] = file;
					return file;
				}
			}

			return null;
		}

		private bool CheckForCyclicalInclude(string fullPathName, IEnumerable<string> parentFiles)
		{
			if (parentFiles.Any(x => x.Equals(fullPathName, StringComparison.OrdinalIgnoreCase)))
			{
				Log.Write(EventLogEntryType.Warning, string.Format("Cyclical include found for file '{0}'", fullPathName));
				return true;
			}
			return false;
		}

		public CodeModel GetModelForSnapshotOrNewer(VsText.ITextSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException("snapshot");

			if (_model != null)
			{
				if (snapshot != null && _model.Snapshot.Version.VersionNumber < snapshot.Version.VersionNumber)
				{
					//_model = new CodeModel(snapshot);
					_model = CreatePreprocessedModel(snapshot);
					_model.Snapshot = snapshot;
				}
			}
			else
			{
				//_model = new CodeModel(snapshot);
				_model = CreatePreprocessedModel(snapshot);
				_model.Snapshot = snapshot;
			}

			return _model;
		}

		private CodeModel CreatePreprocessedModel(VsText.ITextSnapshot snapshot)
		{
			var content = snapshot.GetText();
			var fileName = snapshot.TextBuffer.TryGetFileName();

			var visibleSource = new CodeSource();
			visibleSource.Append(content, fileName, Position.Start, Position.Start.Advance(content), true, true);
			var reader = new CodeSource.CodeSourcePreprocessorReader(visibleSource);
			var prepSource = new CodeSource();

			var defProvider = new DefinitionProvider();
			defProvider.CreateDefinitions = true;

			var prep = new Preprocessor(this);
			prep.Preprocess(reader, prepSource, fileName, new string[0], true);
			var prepModel = new CodeModel(this, prepSource, fileName, false, defProvider);

			defProvider.CreateDefinitions = false;
			//Debug.WriteLine(defProvider.DumpDefinitions());	// TODO: remove

			var visibleModel = new CodeModel(this, visibleSource, fileName, true, defProvider);
			visibleModel.Snapshot = snapshot;

			return visibleModel;
		}

		public CodeModel Model
		{
			get { return _model; }
		}

		public class IncludeFile
		{
			private FileStore _store;
			private string _fullPathName;
			private CodeSource _source;
			private CodeFile _codeFile;

			public IncludeFile(FileStore store, string fileName)
			{
				if (store == null) throw new ArgumentNullException("store");
				if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

				_store = store;
				_fullPathName = fileName;
			}

			public CodeSource Source
			{
				get
				{
					if (_source == null)
					{
						try
						{
							var merger = new FileMerger();
							merger.MergeFile(_fullPathName, true);

							var content = merger.MergedContent;
							if (content == null)
							{
								Log.WriteDebug("Unable to get merged content for include file '{0}'.", _fullPathName);
								return null;
							}

							_source = content;
						}
						catch (Exception ex)
						{
							Log.WriteEx(ex, "Exception when attempting to read content of include file '{0}'.", _fullPathName);
							_source = null;
						}
					}
					return _source;
				}
			}

			public CodeFile GetCodeFile(CodeModel model, IEnumerable<string> parentFiles)
			{
				if (_codeFile == null)
				{
					try
					{
						Log.WriteDebug("Processing include file: {0}", _fullPathName);

						var content = Source;
						if (content == null) return null;

						var file = new CodeFile(model);
						file.Parse(content, _fullPathName, parentFiles, false);
						_codeFile = file;
					}
					catch (Exception ex)
					{
						Log.WriteEx(ex, "Exception when processing include file '{0}'.", _fullPathName);
						_codeFile = null;
					}
				}
				return _codeFile;
			}

			public string FullPathName
			{
				get { return _fullPathName; }
			}
		}
	}
}
