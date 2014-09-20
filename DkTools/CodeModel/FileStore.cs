using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel
{
	internal sealed class FileStore
	{
		private Dictionary<string, IncludeFile> _sameDirIncludeFiles = new Dictionary<string, IncludeFile>();
		private Dictionary<string, IncludeFile> _globalIncludeFiles = new Dictionary<string, IncludeFile>();

		private CodeModel _model;
		private Guid _guid;

		private static CodeModel _stdLibModel;

		public FileStore()
		{
			_guid = Guid.NewGuid();
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
#if DEBUG
			if (parentFiles == null) throw new ArgumentNullException("parentFiles");
#endif
			if (string.IsNullOrEmpty(fileName)) return null;

			// Check if the include file is cached.
			IncludeFile file = null;
			var fileNameLower = fileName.ToLower();

			if (searchSameDir && !string.IsNullOrEmpty(sourceFileName))
			{
				if (_sameDirIncludeFiles.TryGetValue(fileNameLower, out file))
				{
					file.CheckForRefreshRequired();
					return file;
				}
			}

			if (_globalIncludeFiles.TryGetValue(fileNameLower, out file))
			{
				file.CheckForRefreshRequired();
				return file;
			}

			// Search the disk in same directory.
			if (searchSameDir && !string.IsNullOrEmpty(sourceFileName))
			{
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

			// Search the disk in global include directories.
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

		public string LocateIncludeFile(string sourceFileName, string fileName, bool searchSameDir)
		{
			if (string.IsNullOrEmpty(fileName)) return null;

			// Check if the include file is cached.
			IncludeFile file = null;
			var fileNameLower = fileName.ToLower();

			if (searchSameDir && !string.IsNullOrEmpty(sourceFileName))
			{
				if (_sameDirIncludeFiles.TryGetValue(fileNameLower, out file)) return file.FullPathName;
			}

			if (_globalIncludeFiles.TryGetValue(fileNameLower, out file)) return file.FullPathName;

			// Search the disk in same directory.
			if (searchSameDir && !string.IsNullOrEmpty(sourceFileName))
			{
				var pathName = Path.Combine(Path.GetDirectoryName(sourceFileName), fileName);
				if (File.Exists(pathName))
				{
					return Path.GetFullPath(pathName);
				}

				var ampersandFileName = pathName + "&";
				if (File.Exists(ampersandFileName))
				{
					return Path.GetFullPath(ampersandFileName);
				}

				var plusFileName = pathName = "+";
				if (File.Exists(plusFileName))
				{
					return Path.GetFullPath(plusFileName);
				}
			}

			// Search the disk in global include directories.
			foreach (var includeDir in ProbeEnvironment.IncludeDirs)
			{
				var pathName = Path.Combine(includeDir, fileName);
				if (System.IO.File.Exists(pathName))
				{
					return Path.GetFullPath(pathName);
				}

				var ampersandFileName = pathName + "&";
				if (System.IO.File.Exists(ampersandFileName))
				{
					return Path.GetFullPath(ampersandFileName);
				}

				var plusFileName = pathName = "+";
				if (System.IO.File.Exists(plusFileName))
				{
					return Path.GetFullPath(plusFileName);
				}
			}

			return null;
		}

		private bool CheckForCyclicalInclude(string fullPathName, IEnumerable<string> parentFiles)
		{
#if DEBUG
			if (parentFiles == null) throw new ArgumentNullException("parentFiles");
#endif
			if (parentFiles.Any(x => x.Equals(fullPathName, StringComparison.OrdinalIgnoreCase)))
			{
				Log.Write(LogLevel.Warning, string.Format("Cyclical include found for file '{0}'", fullPathName));
				return true;
			}
			return false;
		}

		public CodeModel GetCurrentModel(VsText.ITextSnapshot snapshot, string reason)
		{
#if DEBUG
			if (snapshot == null) throw new ArgumentNullException("snapshot");
#endif

			if (_model != null)
			{
				if (snapshot != null && _model.Snapshot.Version.VersionNumber < snapshot.Version.VersionNumber)
				{
					_model = CreatePreprocessedModel(snapshot, reason);
				}
			}
			else
			{
				_model = CreatePreprocessedModel(snapshot, reason);
			}

			return _model;
		}

		public CodeModel GetMostRecentModel(VsText.ITextSnapshot snapshot, string reason)
		{
#if DEBUG
			if (snapshot == null) throw new ArgumentNullException("snapshot");
#endif
			if (_model == null)
			{
				_model = CreatePreprocessedModel(snapshot, reason);
				_model.Snapshot = snapshot;
			}

			return _model;
		}

		public CodeModel CreatePreprocessedModel(VsText.ITextSnapshot snapshot, string reason)
		{
			var model = CreatePreprocessedModel(snapshot.GetText(), snapshot.TextBuffer.TryGetFileName(), reason);
			model.Snapshot = snapshot;
			return model;
		}

		public CodeModel CreatePreprocessedModel(string content, string fileName, string reason)
		{
#if DEBUG
			Log.WriteDebug("Creating preprocessed model. Reason: {0}", reason);
			var startTime = DateTime.Now;
#endif

			var visibleSource = new CodeSource();
			visibleSource.Append(content, fileName, 0, content.Length, true, true, false);
			visibleSource.Flush();
			var reader = new CodeSource.CodeSourcePreprocessorReader(visibleSource);
			var prepSource = new CodeSource();

			var defProvider = new DefinitionProvider(fileName);

			var ext = Path.GetExtension(fileName);
			var serverContext = GetServerContextFromFileExtension(ext);
			var prep = new Preprocessor(this);
			prep.Preprocess(reader, prepSource, fileName, new string[0], serverContext);
			prep.AddDefinitionsToProvider(defProvider);

			var prepModel = new PreprocessorModel(prepSource, defProvider, fileName);

			var visibleModel = CodeModel.CreateVisibleModelForPreprocessed(visibleSource, this, prepModel);
			visibleModel.PreprocessorModel = prepModel;
			visibleModel.DisabledSections = prepSource.GenerateDisabledSections().ToArray();

#if DEBUG
			var elapsed = DateTime.Now.Subtract(startTime).TotalMilliseconds;
			Log.WriteDebug("Created model in {0} msecs.", elapsed);
#endif

			return visibleModel;
		}

		public CodeModel Model
		{
			get { return _model; }
		}

		public IEnumerable<FunctionDropDownItem> GetFunctionDropDownList(VsText.ITextSnapshot snapshot)
		{
			var model = GetMostRecentModel(snapshot, "Function drop-down list.");

			var prepModel = model.PreprocessorModel;
			if (prepModel == null) yield break;

			foreach (Tokens.FunctionPlaceholderToken func in model.File.FindDownward(x => x is Tokens.FunctionPlaceholderToken))
			{
				yield return new FunctionDropDownItem { Name = func.Text, Span = func.Span, EntireFunctionSpan = func.EntireFunctionSpan };
			}
		}

		public Guid Guid
		{
			get { return _guid; }
		}

		public static ServerContext GetServerContextFromFileExtension(string ext)
		{
			switch (ext.ToLower())
			{
				case ".sc":
				case ".sc+":
				case ".sc&":
				case ".st":
				case ".st+":
				case ".st&":
					return ServerContext.Server;
				case ".cc":
				case ".cc+":
				case ".cc&":
				case ".ct":
				case ".ct+":
				case ".ct&":
					return ServerContext.Client;
				case ".nc":
				case ".nc+":
				case ".nc&":
					return ServerContext.Neutral;
				case ".i":
				case ".i+":
				case ".i&":
				case ".il":
				case ".il+":
				case ".il&":
					return ServerContext.Include;
				default:
					return ServerContext.Unknown;
			}
		}

		private static void CreateStdLibModel()
		{
			var tempStore = new FileStore();
			var includeFile = tempStore.GetIncludeFile(null, "stdlib.i", false, new string[0]);
			if (includeFile != null)
			{
				_stdLibModel = tempStore.CreatePreprocessedModel(includeFile.Source.Text, includeFile.FullPathName, "stdlib.i model");
			}
			else
			{
				_stdLibModel = tempStore.CreatePreprocessedModel(string.Empty, "stdlib.i", "stdlib.i model (blank)");
			}
		}

		public static CodeModel StdLibModel
		{
			get
			{
				if (_stdLibModel == null) CreateStdLibModel();
				return _stdLibModel;
			}
		}

		public sealed class IncludeFile
		{
			private FileStore _store;
			private string _fullPathName;
			private CodeSource _source;
			private CodeFile _codeFile;
			private DateTime _lastCheck;
			private DateTime _lastModifiedDate;

			public IncludeFile(FileStore store, string fileName)
			{
				if (store == null) throw new ArgumentNullException("store");
				if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

				_store = store;
				_fullPathName = fileName;

				Shell.FileSaved += Shell_FileSaving;
			}

			~IncludeFile()
			{
				Shell.FileSaved -= Shell_FileSaving;
			}

			public CodeSource Source
			{
				get
				{
					if (_source == null)
					{
						try
						{
							var content = File.ReadAllText(_fullPathName);
							_source = new CodeSource();
							_source.Append(content, _fullPathName, 0, content.Length, true, false, false);
							_source.Flush();

							var fileInfo = new FileInfo(_fullPathName);
							_lastModifiedDate = fileInfo.LastWriteTime;
							_lastCheck = DateTime.Now;
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

					_lastCheck = DateTime.Now;
				}
				return _codeFile;
			}

			public string FullPathName
			{
				get { return _fullPathName; }
			}

			public void CheckForRefreshRequired()
			{
				if (_lastCheck.AddSeconds(Constants.IncludeFileCheckFrequency) <= DateTime.Now)
				{
					var fileInfo = new FileInfo(_fullPathName);
					var modDate = fileInfo.LastWriteTime;
					if (Math.Abs(modDate.Subtract(_lastModifiedDate).TotalSeconds) > 1.0)
					{
						Log.WriteDebug("Detected change in include file: {0}", _fullPathName);
						OnFileChangeSuspected();
						_lastModifiedDate = modDate;
					}
					_lastCheck = DateTime.Now;
				}
			}

			private void Shell_FileSaving(object sender, Shell.FileSavedEventArgs e)
			{
				if (e.FileName.Equals(_fullPathName, StringComparison.OrdinalIgnoreCase))
				{
					Log.WriteDebug("Detected change in include file (saving): {0}", _fullPathName);
					OnFileChangeSuspected();
				}
			}

			private void OnFileChangeSuspected()
			{
				_source = null;
				_codeFile = null;
			}
		}

		public class FunctionDropDownItem
		{
			public string Name { get; set; }
			public Span Span { get; set; }
			public Span EntireFunctionSpan { get; set; }
		}
	}
}
