using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Diagnostics;
using DK.Preprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DK.Modeling
{
	public sealed class FileStore
	{
		private Dictionary<string, IncludeFile> _sameDirIncludeFiles = new Dictionary<string, IncludeFile>();
		private Dictionary<string, IncludeFile> _globalIncludeFiles = new Dictionary<string, IncludeFile>();
		private Dictionary<string, Definition[]> _includeParentDefs = new Dictionary<string, Definition[]>();

		private DkAppContext _app;
		private CodeModel _model;
		private Guid _guid;

		private static CodeModel _stdLibModel;
		private static PreprocessorDefine[] _stdLibDefines;

		private const int NumberOfIncludeParentFiles = 3;

		public FileStore(DkAppContext app)
		{
			_app = app ?? throw new ArgumentNullException(nameof(app));
			_guid = Guid.NewGuid();
            app.RefreshAllDocumentsRequired += OnRefreshAllDocumentsRequired;
            app.RefreshDocumentRequired += OnRefreshDocumentRequired;
            app.FileChanged += OnFileChanged;
            app.FileDeleted += OnFileDeleted;
		}

		~FileStore()
		{
            _app.RefreshAllDocumentsRequired -= OnRefreshAllDocumentsRequired;
            _app.RefreshDocumentRequired -= OnRefreshDocumentRequired;
            _app.FileChanged -= OnFileChanged;
            _app.FileDeleted -= OnFileDeleted;
		}

		internal IncludeFile GetIncludeFile(DkAppSettings appSettings, string sourceFileName, string fileName, bool searchSameDir, IEnumerable<string> parentFiles)
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
				var pathName = PathUtil.CombinePath(PathUtil.GetDirectoryName(sourceFileName), fileName);
				if (appSettings.FileSystem.FileExists(pathName))
				{
					if (CheckForCyclicalInclude(pathName, parentFiles)) return null;
					file = new IncludeFile(pathName);
					_sameDirIncludeFiles[fileNameLower] = file;
					return file;
				}
			}

			// Search the disk in global include directories.
			if (appSettings.Initialized)
			{
				foreach (var includeDir in appSettings.IncludeDirs)
				{
					var pathName = PathUtil.CombinePath(includeDir, fileName);
					if (appSettings.FileSystem.FileExists(pathName))
					{
						if (CheckForCyclicalInclude(pathName, parentFiles)) return null;
						file = new IncludeFile(pathName);
						_globalIncludeFiles[fileNameLower] = file;
						return file;
					}
				}
			}

			return null;
		}

		internal string LocateIncludeFile(DkAppSettings appSettings, string sourceFileName, string fileName, bool searchSameDir)
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
				var pathName = PathUtil.CombinePath(PathUtil.GetDirectoryName(sourceFileName), fileName);
				if (appSettings.FileSystem.FileExists(pathName))
				{
					return appSettings.FileSystem.GetFullPath(pathName);
				}
			}

			// Search the disk in global include directories.
			if (appSettings.Initialized)
			{
				foreach (var includeDir in appSettings.IncludeDirs)
				{
					var pathName = PathUtil.CombinePath(includeDir, fileName);
					if (appSettings.FileSystem.FileExists(pathName))
					{
						return appSettings.FileSystem.GetFullPath(pathName);
					}
				}
			}

			return null;
		}

		internal bool CheckForCyclicalInclude(string fullPathName, IEnumerable<string> parentFiles)
		{
#if DEBUG
			if (parentFiles == null) throw new ArgumentNullException("parentFiles");
#endif
			if (parentFiles.Any(x => x.Equals(fullPathName, StringComparison.OrdinalIgnoreCase)))
			{
				_app.Log.Write(LogLevel.Warning, string.Format("Cyclical include found for file '{0}'", fullPathName));
				return true;
			}
			return false;
		}

		public CodeModel CreatePreprocessedModel(
			DkAppSettings appSettings,
			CodeSource source,
			string fileName,
			bool visible,
			string reason,
			CancellationToken cancel,
			IEnumerable<IncludeDependency> includeDependencies = null)
		{
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
#if DEBUG
			_app.Log.Debug("Creating preprocessed model. Reason: {0}", reason);
			var startTime = DateTime.Now;
#endif

			var reader = new CodeSource.CodeSourcePreprocessorReader(source);
			var prepSource = new CodeSource();

			var defProvider = new DefinitionProvider(appSettings, fileName);

			var fileContext = FileContextHelper.GetFileContextFromFileName(fileName);
			var prep = new Preprocessor(appSettings, this);
			if (includeDependencies != null) prep.AddIncludeDependencies(includeDependencies);
			prep.Preprocess(reader, prepSource, fileName, new string[0], fileContext, stdlibDefines: _stdLibDefines, cancel: cancel);
			prep.AddDefinitionsToProvider(defProvider);

			if (fileContext == FileContext.Include && !string.IsNullOrEmpty(fileName))
			{
				var includeParentDefs = GetIncludeParentDefinitions(appSettings, fileName, cancel);
				defProvider.AddGlobalFromFile(includeParentDefs);
			}

#if DEBUG
			var midTime1 = DateTime.Now;
#endif

			var prepModel = new PreprocessorModel(appSettings, prepSource, defProvider, fileName, visible, prep.IncludeDependencies, cancel)
			{
				Preprocessor = prep
			};

#if DEBUG
			var midTime2 = DateTime.Now;
#endif

			CodeModel modelToReturn;
			if (visible)
			{
				modelToReturn = CodeModel.CreateVisibleModelForPreprocessed(source, appSettings, this, prepModel, cancel);
				modelToReturn.PreprocessorModel = prepModel;
				modelToReturn.DisabledSections = prepSource.GenerateDisabledSections().ToArray();
			}
			else
			{
				modelToReturn = CodeModel.CreateFullModelForPreprocessed(prepSource, appSettings, this, prepModel, cancel);
				modelToReturn.PreprocessorModel = prepModel;
			}

			modelToReturn.PreprocessorReferences = prep.References;

#if DEBUG
			var endTime = DateTime.Now;
			var elapsedTime = endTime.Subtract(startTime).TotalMilliseconds;
			var prepTime = midTime1.Subtract(startTime).TotalMilliseconds;
			var modelTime = midTime2.Subtract(midTime1).TotalMilliseconds;
			var visTime = endTime.Subtract(midTime2).TotalMilliseconds;
			_app.Log.Debug("Created model in {0} msecs ({1} preprocessor, {2} model, {3} visible)", elapsedTime, prepTime, modelTime, visTime);
#endif

			return modelToReturn;
		}

		public CodeModel Model
		{
			get { return _model; }
			set { _model = value; }
		}

		private static void CreateStdLibModel(DkAppContext app)
		{
			var tempStore = new FileStore(app);
			var includeFile = tempStore.GetIncludeFile(app.Settings, sourceFileName: null, "stdlib.i", searchSameDir: false, StringHelper.EmptyStringArray);
			if (includeFile != null)
			{
				_stdLibModel = tempStore.CreatePreprocessedModel(app.Settings, includeFile.GetSource(app.Settings),
					includeFile.FullPathName, false, "stdlib.i model", CancellationToken.None, includeDependencies: null);
			}
			else
			{
				var blankSource = new CodeSource();
				blankSource.Flush();

				_stdLibModel = tempStore.CreatePreprocessedModel(app.Settings, blankSource, "stdlib.i", false,
					"stdlib.i model (blank)", CancellationToken.None, includeDependencies: null);
			}

			_stdLibDefines = _stdLibModel.PreprocessorModel.Preprocessor.Defines.ToArray();
		}

		public static CodeModel GetStdLibModel(DkAppContext app)
		{
			if (_stdLibModel == null) CreateStdLibModel(app);
			return _stdLibModel;
		}

		private void OnRefreshAllDocumentsRequired(object sender, EventArgs e)
		{
			_model = null;
		}

		private void OnRefreshDocumentRequired(object sender, RefreshDocumentEventArgs e)
		{
			if (_model != null && e.FilePath.EqualsI(_model.FilePath))
			{
				_model = null;
			}
		}

		private void OnFileChanged(object sender, FileEventArgs e)
		{
			try
			{
				InvalidateFile(e.FilePath);
			}
			catch (Exception ex)
			{
				_app.Log.Error(ex);
			}
		}

		private void OnFileDeleted(object sender, FileEventArgs e)
		{
			try
			{
				InvalidateFile(e.FilePath);
			}
			catch (Exception ex)
			{
				_app.Log.Error(ex);
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
					_app.Log.Debug("FileStore is triggering a refresh for document '{0}' because a refresh was detected for an include file.", _model.FilePath);
                    _app.OnRefreshDocumentRequired(_model.FilePath);
				}

				_model = null;
			}
			// If the file touched is the main model, then require it to be completely rebuilt.
			else if (string.Equals(_model?.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
			{
                _app.OnRefreshDocumentRequired(_model.FilePath);
				_model = null;
			}
		}

		public Definition[] GetIncludeParentDefinitions(DkAppSettings appSettings, string includePathName, CancellationToken cancel)
		{
			Definition[] cachedDefs;
			if (_includeParentDefs.TryGetValue(includePathName.ToLower(), out cachedDefs)) return cachedDefs;

			_app.Log.Debug("Getting include file parent definitions: {0}", includePathName);

			var parentFileNames = appSettings.Repo.GetDependentFiles(includePathName, NumberOfIncludeParentFiles);

			Definition[] commonDefines = null;

			if (!parentFileNames.Any())
			{
				_app.Log.Debug("This file is not included by any other file.");
				commonDefines = new Definition[0];
				_includeParentDefs[includePathName.ToLower()] = commonDefines;
				return commonDefines;
			}

			foreach (var parentPathName in parentFileNames)
			{
				_app.Log.Debug("Preprocessing include parent: {0}", parentPathName);

				var merger = new FileMerger(appSettings);
				merger.MergeFile(parentPathName, null, false, true);
				var source = merger.MergedContent;

				var reader = new CodeSource.CodeSourcePreprocessorReader(source);
				var prepSource = new CodeSource();

				var fileContext = FileContextHelper.GetFileContextFromFileName(parentPathName);
				var prep = new Preprocessor(appSettings, this);
				var prepResult = prep.Preprocess(reader, prepSource, parentPathName, new string[0], fileContext, cancel, stopAtIncludeFile: includePathName);
				if (!prepResult.IncludeFileReached)
				{
					_app.Log.Warning("Include file not reached when preprocessing parent.\r\nInclude File: {0}\r\nParent File: {1}",
						includePathName, parentPathName);
					continue;
				}

				var defs = prep.ActiveDefineDefinitions;
				if (!defs.Any())
				{
					// No defines will be common
					_app.Log.Debug("No definitions found in include parent file: {0}", parentPathName);
					commonDefines = new Definition[0];
					break;
				}

				if (commonDefines == null)
				{
					commonDefines = defs.ToArray();
					_app.Log.Debug("{1} definition(s) found in include parent file: {0}", parentPathName, commonDefines.Length);
				}
				else
				{
					// Create array of defines common to all
					commonDefines = (from c in commonDefines where defs.Any(d => d.Name == c.Name) select c).ToArray();
					_app.Log.Debug("{1} definition(s) found in include parent file: {0}", parentPathName, commonDefines.Length);
					if (commonDefines.Length == 0) break;
				}
			}

			if (commonDefines == null) commonDefines = new Definition[0];
			_app.Log.Debug("Using {0} definition(s) from include parent files.", commonDefines.Length);
			_includeParentDefs[includePathName.ToLower()] = commonDefines;
			return commonDefines;
		}
	}
}
