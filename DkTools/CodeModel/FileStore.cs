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

		private const int NumberOfIncludeParentFiles = 3;

		public FileStore()
		{
			_guid = Guid.NewGuid();
			ProbeToolsPackage.RefreshAllDocumentsRequired += OnRefreshAllDocumentsRequired;
			ProbeToolsPackage.RefreshDocumentRequired += OnRefreshDocumentRequired;
			ProbeAppSettings.FileChanged += ProbeAppSettings_FileChanged;
			ProbeAppSettings.FileDeleted += ProbeAppSettings_FileDeleted;
		}

		~FileStore()
		{
			ProbeToolsPackage.RefreshAllDocumentsRequired -= OnRefreshAllDocumentsRequired;
			ProbeToolsPackage.RefreshDocumentRequired -= OnRefreshDocumentRequired;
			ProbeAppSettings.FileChanged -= ProbeAppSettings_FileChanged;
			ProbeAppSettings.FileDeleted -= ProbeAppSettings_FileDeleted;
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
					return file;
				}
			}

			if (_globalIncludeFiles.TryGetValue(fileNameLower, out file))
			{
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
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

			var source = new CodeSource();
			source.Append(snapshot.GetText(), fileName, 0, snapshot.Length, true, true, false);
			source.Flush();

			var model = CreatePreprocessedModel(appSettings, source, fileName, true, reason, null);
			model.Snapshot = snapshot;
			return model;
		}

		public CodeModel CreatePreprocessedModel(ProbeAppSettings appSettings, string fileName, VsText.ITextSnapshot snapshot, bool visible, string reason)
		{
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

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
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
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
				if (!def.SourceFileName.Equals(model.FilePath, StringComparison.OrdinalIgnoreCase)) continue;

				yield return new FunctionDropDownItem(def, def.Name, new Span(def.SourceStartPos, def.SourceStartPos), def.EntireSpan);
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

		private void OnRefreshAllDocumentsRequired(object sender, EventArgs e)
		{
			_model = null;
		}

		private void OnRefreshDocumentRequired(object sender, ProbeToolsPackage.RefreshDocumentEventArgs e)
		{
			if (_model != null && e.FilePath.EqualsI(_model.FilePath))
			{
				_model = null;
			}
		}

		private void ProbeAppSettings_FileChanged(object sender, ProbeAppSettings.FileEventArgs e)
		{
			try
			{
				InvalidateFile(e.FilePath);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		private void ProbeAppSettings_FileDeleted(object sender, ProbeAppSettings.FileEventArgs e)
		{
			try
			{
				InvalidateFile(e.FilePath);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		private void InvalidateFile(string filePath)
		{
			// If the file is an include file this model depends on, then clear those from the cache as well.
			var includeFilesAffected = false;
			var filesToRemove = new List<string>();
			foreach (var kv in _sameDirIncludeFiles)
			{
				if (string.Equals(kv.Value.FullPathName, filePath, StringComparison.OrdinalIgnoreCase))
				{
					filesToRemove.Add(kv.Key);
					includeFilesAffected = true;
				}
			}
			foreach (var key in filesToRemove)
			{
				_sameDirIncludeFiles.Remove(key);
			}

			filesToRemove.Clear();
			foreach (var kv in _globalIncludeFiles)
			{
				if (string.Equals(kv.Value.FullPathName, filePath, StringComparison.OrdinalIgnoreCase))
				{
					filesToRemove.Add(kv.Key);
					includeFilesAffected = true;
				}
			}
			foreach (var key in filesToRemove)
			{
				_globalIncludeFiles.Remove(key);
			}

			filesToRemove.Clear();
			foreach (var includeFilePath in _includeParentDefs.Keys)
			{
				if (string.Equals(includeFilePath, filePath, StringComparison.OrdinalIgnoreCase))
				{
					filesToRemove.Add(includeFilePath);
					includeFilesAffected = true;
				}
			}
			foreach (var key in filesToRemove)
			{
				_includeParentDefs.Remove(key);
			}

			// If an include file was touched by this change, then rebuild the entire model.
			if (includeFilesAffected)
			{
				// The main model needs to be refreshed as well, since it depends on that include file.
				if (_model != null && !string.IsNullOrEmpty(_model.FilePath))
				{
					Log.Debug("FileStore is triggering a refresh for document '{0}' because a refresh was detected for an include file.", _model.FilePath);
					ProbeToolsPackage.Instance.FireRefreshDocument(_model.FilePath);
				}

				_model = null;
			}
			// If the file touched is the main model, then require it to be completely rebuilt.
			else if (string.Equals(_model?.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
			{
				_model = null;
			}
		}

		public Definition[] GetIncludeParentDefinitions(ProbeAppSettings appSettings, string includePathName)
		{
			Definition[] cachedDefs;
			if (_includeParentDefs.TryGetValue(includePathName.ToLower(), out cachedDefs)) return cachedDefs;

			Log.Debug("Getting include file parent definitions: {0}", includePathName);

			IEnumerable<string> parentFileNames;
			var app = FunctionFileScanning.FFScanner.CurrentApp;
			if (app != null)
			{
				parentFileNames = app.Repo.GetDependentFiles(includePathName, NumberOfIncludeParentFiles);
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

		public class FunctionDropDownItem
		{
			public FunctionDropDownItem(FunctionDefinition definition, string name, Span span, Span entireFunctionSpan)
			{
				Definition = definition ?? throw new ArgumentNullException(nameof(definition));
				Name = name;
				Span = span;
				EntireFunctionSpan = entireFunctionSpan;
			}

			public FunctionDefinition Definition { get; private set; }
			public string Name { get; private set; }
			public Span Span { get; private set; }
			public Span EntireFunctionSpan { get; private set; }
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
