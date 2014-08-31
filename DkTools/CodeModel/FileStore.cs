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

		private CodeModel _model;

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

			// Check if the include file is cached.
			IncludeFile file = null;
			var fileNameLower = fileName.ToLower();

			if (searchSameDir && !string.IsNullOrEmpty(sourceFileName))
			{
				if (_sameDirIncludeFiles.TryGetValue(fileNameLower, out file)) return file;
			}

			if (_globalIncludeFiles.TryGetValue(fileNameLower, out file)) return file;

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

		private bool CheckForCyclicalInclude(string fullPathName, IEnumerable<string> parentFiles)
		{
			if (parentFiles.Any(x => x.Equals(fullPathName, StringComparison.OrdinalIgnoreCase)))
			{
				Log.Write(EventLogEntryType.Warning, string.Format("Cyclical include found for file '{0}'", fullPathName));
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
			Log.WriteDebug("Creating preprocessed model. Reason: {0}", reason);

			var visibleSource = new CodeSource();
			visibleSource.Append(content, fileName, Position.Start, Position.Start.Advance(content), true, true);
			visibleSource.Flush();
			var reader = new CodeSource.CodeSourcePreprocessorReader(visibleSource);
			var prepSource = new CodeSource();

			var defProvider = new DefinitionProvider();
			defProvider.Preprocessor = true;

			var includeStdLib = true;
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				var titleExt = Path.GetFileName(fileName);
				if (titleExt.Equals("stdlib.i", StringComparison.OrdinalIgnoreCase) ||
					titleExt.Equals("stdlib.i&", StringComparison.OrdinalIgnoreCase) ||
					titleExt.Equals("stdlib.i+", StringComparison.OrdinalIgnoreCase))
				{
					includeStdLib = false;
				}
			}

			var prep = new Preprocessor(this);
			prep.Preprocess(reader, prepSource, fileName, new string[0], includeStdLib);
			var prepModel = CodeModel.CreatePreprocessorModel(this, prepSource, fileName, defProvider);

			defProvider.Preprocessor = false;

			var visibleModel = prepModel.CreateVisibleModelForPreprocessed(visibleSource);
			visibleModel.PreprocessorModel = prepModel;

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

		public class FunctionDropDownItem
		{
			public string Name { get; set; }
			public Span Span { get; set; }
			public Span EntireFunctionSpan { get; set; }
		}
	}
}
