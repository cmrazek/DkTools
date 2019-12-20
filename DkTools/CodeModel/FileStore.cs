using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel
{
	internal sealed class FileStore
	{
		private Dictionary<string, IncludeFile> _sameDirIncludeFiles = new Dictionary<string, IncludeFile>();
		private Dictionary<string, IncludeFile> _globalIncludeFiles = new Dictionary<string, IncludeFile>();
		private Dictionary<string, Definitions.Definition[]> _includeParentDefs = new Dictionary<string, Definitions.Definition[]>();

		private CodeModel _model;
		private Guid _guid;

		private static CodeModel _stdLibModel;
		private static PreprocessorDefine[] _stdLibDefines;

		public event EventHandler<ModelUpdatedEventArgs> ModelUpdated;
		public static event EventHandler AllModelRebuildRequired;

		private const int NumberOfIncludeParentFiles = 3;

		public FileStore()
		{
			_guid = Guid.NewGuid();
			AllModelRebuildRequired += FileStore_AllModelRebuildRequired;
		}

		public static FileStore GetOrCreateForTextBuffer(VsText.ITextBuffer buf)
		{
			if (buf == null) throw new ArgumentNullException("buf");

			if (buf.ContentType.TypeName != Constants.DkContentType) return null;

			FileStore cache;
			if (buf.Properties.TryGetProperty(typeof(FileStore), out cache)) return cache;

			cache = new FileStore();
			buf.Properties[typeof(FileStore)] = cache;

			return cache;
		}

		public IncludeFile GetIncludeFile(ProbeAppSettings appSettings, string sourceFileName, string fileName, bool searchSameDir, IEnumerable<string> parentFiles)
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
				if (File.Exists(pathName))
				{
					if (CheckForCyclicalInclude(pathName, parentFiles)) return null;
					file = new IncludeFile(this, pathName);
					_sameDirIncludeFiles[fileNameLower] = file;
					return file;
				}
			}

			// Search the disk in global include directories.
			if (appSettings.Initialized)
			{
				foreach (var includeDir in appSettings.IncludeDirs)
				{
					var pathName = Path.Combine(includeDir, fileName);
					if (File.Exists(pathName))
					{
						if (CheckForCyclicalInclude(pathName, parentFiles)) return null;
						file = new IncludeFile(this, pathName);
						_globalIncludeFiles[fileNameLower] = file;
						return file;
					}
				}
			}

			return null;
		}

		public string LocateIncludeFile(ProbeAppSettings appSettings, string sourceFileName, string fileName, bool searchSameDir)
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
			}

			// Search the disk in global include directories.
			if (appSettings.Initialized)
			{
				foreach (var includeDir in appSettings.IncludeDirs)
				{
					var pathName = Path.Combine(includeDir, fileName);
					if (System.IO.File.Exists(pathName))
					{
						return Path.GetFullPath(pathName);
					}
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

		public CodeModel GetCurrentModel(ProbeAppSettings appSettings, string fileName, VsText.ITextSnapshot snapshot, string reason)
		{
#if DEBUG
			if (snapshot == null) throw new ArgumentNullException("snapshot");
#endif

			if (_model != null)
			{
				if (snapshot != null && _model.Snapshot.Version.VersionNumber < snapshot.Version.VersionNumber)
				{
					_model = CreatePreprocessedModel(appSettings, fileName, snapshot, reason);

					var ev = ModelUpdated;
					if (ev != null) ev(this, new ModelUpdatedEventArgs(_model));
				}
			}
			else
			{
				_model = CreatePreprocessedModel(appSettings, fileName, snapshot, reason);

				var ev = ModelUpdated;
				if (ev != null) ev(this, new ModelUpdatedEventArgs(_model));
			}

			return _model;
		}

		public CodeModel GetMostRecentModel(ProbeAppSettings appSettings, string fileName, VsText.ITextSnapshot snapshot, string reason)
		{
#if DEBUG
			if (snapshot == null) throw new ArgumentNullException("snapshot");
#endif
			if (_model == null)
			{
				_model = CreatePreprocessedModel(appSettings, fileName, snapshot, reason);

				var ev = ModelUpdated;
				if (ev != null) ev(this, new ModelUpdatedEventArgs(_model));
			}

			return _model;
		}

		public CodeModel RegenerateModel(ProbeAppSettings appSettings, string fileName, VsText.ITextSnapshot snapshot, string reason)
		{
#if DEBUG
			if (snapshot == null) throw new ArgumentNullException("snapshot");
#endif

			_model = CreatePreprocessedModel(appSettings, fileName, snapshot, reason);

			var ev = ModelUpdated;
			if (ev != null) ev(this, new ModelUpdatedEventArgs(_model));

			return _model;
		}

		public CodeModel CreatePreprocessedModel(ProbeAppSettings appSettings, string fileName, VsText.ITextSnapshot snapshot, string reason)
		{
			var source = new CodeSource();
			source.Append(snapshot.GetText(), fileName, 0, snapshot.Length, true, true, false);
			source.Flush();

			var model = CreatePreprocessedModel(appSettings, source, fileName, true, reason, null);
			model.Snapshot = snapshot;
			return model;
		}

		public CodeModel CreatePreprocessedModel(ProbeAppSettings appSettings, string fileName, VsText.ITextSnapshot snapshot, bool visible, string reason)
		{
			CodeSource source;
			IEnumerable<Preprocessor.IncludeDependency> includeDependencies = null;
			if (visible || string.IsNullOrEmpty(fileName))
			{
				source = new CodeSource();
				source.Append(snapshot.GetText(), fileName, 0, snapshot.Length, true, true, false);
				source.Flush();
			}
			else
			{
				var merger = new FileMerger();
				merger.MergeFile(appSettings, fileName, snapshot.GetText(), false, true);
				source = merger.MergedContent;

				includeDependencies = (from f in merger.FileNames
									   select new Preprocessor.IncludeDependency(f, false, true, merger.GetFileContent(f))).ToArray();
			}

			var model = CreatePreprocessedModel(appSettings, source, fileName, visible, reason, includeDependencies);
			model.Snapshot = snapshot;
			return model;
		}

		public CodeModel CreatePreprocessedModel(ProbeAppSettings appSettings, CodeSource source, string fileName,
			bool visible, string reason, IEnumerable<Preprocessor.IncludeDependency> includeDependencies)
		{
#if DEBUG
			Log.Debug("Creating preprocessed model. Reason: {0}", reason);
			var startTime = DateTime.Now;
#endif

			var reader = new CodeSource.CodeSourcePreprocessorReader(source);
			var prepSource = new CodeSource();

			var defProvider = new DefinitionProvider(appSettings, fileName);

			var fileContext = FileContextUtil.GetFileContextFromFileName(fileName);
			var prep = new Preprocessor(appSettings, this);
			if (includeDependencies != null) prep.AddIncludeDependencies(includeDependencies);
			prep.Preprocess(reader, prepSource, fileName, new string[0], fileContext, stdlibDefines: _stdLibDefines);
			prep.AddDefinitionsToProvider(defProvider);

			if (fileContext == FileContext.Include && !string.IsNullOrEmpty(fileName))
			{
				var includeParentDefs = GetIncludeParentDefinitions(appSettings, fileName);
				defProvider.AddGlobalFromFile(includeParentDefs);
			}

#if DEBUG
			var midTime1 = DateTime.Now;
#endif

			var prepModel = new PreprocessorModel(prepSource, defProvider, fileName, visible, prep.IncludeDependencies)
			{
				Preprocessor = prep
			};

#if DEBUG
			var midTime2 = DateTime.Now;
#endif

			CodeModel modelToReturn;
			if (visible)
			{
				modelToReturn = CodeModel.CreateVisibleModelForPreprocessed(source, appSettings, this, prepModel);
				modelToReturn.PreprocessorModel = prepModel;
				modelToReturn.DisabledSections = prepSource.GenerateDisabledSections().ToArray();
			}
			else
			{
				modelToReturn = CodeModel.CreateFullModelForPreprocessed(prepSource, appSettings, this, prepModel);
				modelToReturn.PreprocessorModel = prepModel;
			}

			modelToReturn.PreprocessorReferences = prep.References;

#if DEBUG
			var endTime = DateTime.Now;
			var elapsedTime = endTime.Subtract(startTime).TotalMilliseconds;
			var prepTime = midTime1.Subtract(startTime).TotalMilliseconds;
			var modelTime = midTime2.Subtract(midTime1).TotalMilliseconds;
			var visTime = endTime.Subtract(midTime2).TotalMilliseconds;
			Log.Debug("Created model in {0} msecs ({1} preprocessor, {2} model, {3} visible)", elapsedTime, prepTime, modelTime, visTime);
#endif

			return modelToReturn;
		}

		public CodeModel Model
		{
			get { return _model; }
		}

		public IEnumerable<FunctionDropDownItem> GetFunctionDropDownList(ProbeAppSettings appSettings,
			string fileName, VsText.ITextSnapshot snapshot)
		{
			var model = GetMostRecentModel(appSettings, fileName, snapshot, "Function drop-down list.");

			var prepModel = model.PreprocessorModel;
			if (prepModel == null) yield break;

			foreach (var func in model.PreprocessorModel.LocalFunctions)
			{
				var def = func.Definition;
				if (def.EntireSpan.Length == 0) continue;
				if (!def.SourceFileName.Equals(model.FileName, StringComparison.OrdinalIgnoreCase)) continue;

				yield return new FunctionDropDownItem
				{
					Name = def.Name,
					Span = new Span(def.SourceStartPos, def.SourceStartPos),
					EntireFunctionSpan = def.EntireSpan
				};
			}
		}

		public Guid Guid
		{
			get { return _guid; }
		}

		private static void CreateStdLibModel(ProbeAppSettings appSettings)
		{
			var tempStore = new FileStore();
			var includeFile = tempStore.GetIncludeFile(appSettings, null, "stdlib.i", false, new string[0]);
			if (includeFile != null)
			{
				_stdLibModel = tempStore.CreatePreprocessedModel(appSettings, includeFile.GetSource(appSettings),
					includeFile.FullPathName, false, "stdlib.i model", null);
			}
			else
			{
				var blankSource = new CodeSource();
				blankSource.Flush();

				_stdLibModel = tempStore.CreatePreprocessedModel(appSettings, blankSource, "stdlib.i", false,
					"stdlib.i model (blank)", null);
			}

			_stdLibDefines = _stdLibModel.PreprocessorModel.Preprocessor.Defines.ToArray();
		}

		public static CodeModel GetStdLibModel(ProbeAppSettings appSettings)
		{
			if (_stdLibModel == null) CreateStdLibModel(appSettings);
			return _stdLibModel;
		}

		public static void FireAllModelRebuildRequired()
		{
			var ev = AllModelRebuildRequired;
			if (ev != null) ev(null, EventArgs.Empty);
		}

		private void FileStore_AllModelRebuildRequired(object sender, EventArgs e)
		{
			_model = null;
		}

		public Definition[] GetIncludeParentDefinitions(ProbeAppSettings appSettings, string includePathName)
		{
			Definition[] cachedDefs;
			if (_includeParentDefs.TryGetValue(includePathName.ToLower(), out cachedDefs)) return cachedDefs;

			Log.Debug("Getting include file parent definitions: {0}", includePathName);

			IEnumerable<string> parentFileNames;
			var ds = DefinitionStore.Current;
			if (ds != null)
			{
				parentFileNames = ds.GetIncludeParentFiles(includePathName, NumberOfIncludeParentFiles);
			}
			else
			{
				parentFileNames = new string[0];
			}

			Definition[] commonDefines = null;

			if (!parentFileNames.Any())
			{
				Log.Debug("This file is not included by any other file.");
				commonDefines = new Definition[0];
				_includeParentDefs[includePathName.ToLower()] = commonDefines;
				return commonDefines;
			}

			foreach (var parentPathName in parentFileNames)
			{
				Log.Debug("Preprocessing include parent: {0}", parentPathName);

				var merger = new FileMerger();
				merger.MergeFile(appSettings, parentPathName, null, false, true);
				var source = merger.MergedContent;

				var reader = new CodeSource.CodeSourcePreprocessorReader(source);
				var prepSource = new CodeSource();

				var fileContext = FileContextUtil.GetFileContextFromFileName(parentPathName);
				var prep = new Preprocessor(appSettings, this);
				var prepResult = prep.Preprocess(reader, prepSource, parentPathName, new string[0], fileContext, includePathName);
				if (!prepResult.IncludeFileReached)
				{
					Log.Warning("Include file not reached when preprocessing parent.\r\nInclude File: {0}\r\nParent File: {1}",
						includePathName, parentPathName);
					continue;
				}

				var defs = prep.ActiveDefineDefinitions;
				if (!defs.Any())
				{
					// No defines will be common
					Log.Debug("No definitions found in include parent file: {0}", parentPathName);
					commonDefines = new Definition[0];
					break;
				}

				if (commonDefines == null)
				{
					commonDefines = defs.ToArray();
					Log.Debug("{1} definition(s) found in include parent file: {0}", parentPathName, commonDefines.Length);
				}
				else
				{
					// Create array of defines common to all
					commonDefines = (from c in commonDefines where defs.Any(d => d.Name == c.Name) select c).ToArray();
					Log.Debug("{1} definition(s) found in include parent file: {0}", parentPathName, commonDefines.Length);
					if (commonDefines.Length == 0) break;
				}
			}

			if (commonDefines == null) commonDefines = new Definition[0];
			Log.Debug("Using {0} definition(s) from include parent files.", commonDefines.Length);
			_includeParentDefs[includePathName.ToLower()] = commonDefines;
			return commonDefines;
		}

		public sealed class IncludeFile
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

				Shell.FileSaved += Shell_FileSaving;
			}

			~IncludeFile()
			{
				Shell.FileSaved -= Shell_FileSaving;
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

			public void CheckForRefreshRequired()
			{
				if (_lastCheck.AddSeconds(Constants.IncludeFileCheckFrequency) <= DateTime.Now)
				{
					var fileInfo = new FileInfo(_fullPathName);
					var modDate = fileInfo.LastWriteTime;
					if (Math.Abs(modDate.Subtract(_lastModifiedDate).TotalSeconds) > 1.0)
					{
						Log.Debug("Detected change in include file: {0}", _fullPathName);
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
					Log.Debug("Detected change in include file (saving): {0}", _fullPathName);
					OnFileChangeSuspected();
				}
			}

			private void OnFileChangeSuspected()
			{
				_source = null;
				_codeFile = null;
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

		public class FunctionDropDownItem
		{
			public string Name { get; set; }
			public Span Span { get; set; }
			public Span EntireFunctionSpan { get; set; }
		}

		public class ModelUpdatedEventArgs : EventArgs
		{
			public CodeModel Model { get; private set; }

			public ModelUpdatedEventArgs(CodeModel model)
			{
				Model = model;
			}
		}
	}
}
