using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel
{
	internal class CodeModel
	{
		private CodeFile _file;
		private string _fileName;
		private string _fileTitle;
		private Microsoft.VisualStudio.Text.ITextSnapshot _snapshot;
		private ProbeAppSettings _appSettings;
		private FileStore _store;
		private DefinitionProvider _defProvider;
		private PreprocessorModel _prepModel;
		private Span[] _disabledSections;
		private ModelType _modelType;
		private string _className;
		private FileContext _fileContext;
		private Preprocessor.Reference[] _prepRefs;

		private CodeModel()
		{ }

		private CodeModel(ProbeAppSettings appSettings, FileStore store)
		{
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_store = store ?? throw new ArgumentNullException(nameof(store));
		}

		public static CodeModel CreateVisibleModelForPreprocessed(CodeSource visibleSource, ProbeAppSettings appSettings, FileStore store, PreprocessorModel prepModel)
		{
			var model = new CodeModel(appSettings, store);
			var codeFile = new CodeFile(model);

			model.Init(visibleSource, codeFile, prepModel.FileName, true, prepModel.DefinitionProvider);
			return model;
		}

		public static CodeModel CreateFullModelForPreprocessed(CodeSource source, ProbeAppSettings appSettings, FileStore store, PreprocessorModel prepModel)
		{
			var model = new CodeModel(appSettings, store);
			var codeFile = new CodeFile(model);

			model.Init(source, codeFile, prepModel.FileName, false, prepModel.DefinitionProvider);
			return model;
		}

		private void Init(CodeSource source, CodeFile file, string fileName, bool visible, DefinitionProvider defProvider)
		{
#if DEBUG
			if (defProvider == null) throw new ArgumentNullException("defProvider");
#endif
			this.RefreshTime = DateTime.Now;

			_fileName = fileName;
			if (!string.IsNullOrEmpty(_fileName)) _fileTitle = Path.GetFileNameWithoutExtension(_fileName);

			_fileContext = FileContextUtil.GetFileContextFromFileName(_fileName);

			if (FunctionFileScanning.FFUtil.FileNameIsFunction(fileName))
			{
				_modelType = ModelType.Function;
			}
			else if (FunctionFileScanning.FFUtil.FileNameIsClass(fileName, out _className))
			{
				_modelType = DkTools.CodeModel.ModelType.Class;
			}
			else
			{
				_modelType = DkTools.CodeModel.ModelType.Other;
			}

			_defProvider = defProvider;
			_file = new CodeFile(this);

			_file.Parse(source, _fileName, new string[0], visible);

			this.RefreshTime = DateTime.Now;
		}

		#region External Properties
		public DateTime LastAccessTime { get; set; }
		public DateTime RefreshTime { get; set; }

		public Microsoft.VisualStudio.Text.ITextSnapshot Snapshot
		{
			get { return _snapshot; }
			set { _snapshot = value; }
		}
		#endregion

		#region Brace matching and outlining
		public IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				return _file.OutliningRegions;
			}
		}
		#endregion

		#region Util functions
		/// <summary>
		/// Adjusts a position from another snapshot to the model's snapshot.
		/// </summary>
		public int AdjustPosition(int pos, VsText.ITextSnapshot snapshot)
		{
			if (snapshot == null || _snapshot == null || _snapshot == snapshot)
			{
				return pos;
			}

			var pt = new Microsoft.VisualStudio.Text.SnapshotPoint(snapshot, pos).TranslateTo(_snapshot, Microsoft.VisualStudio.Text.PointTrackingMode.Positive);
			return pt.Position;
		}

		public int AdjustPosition(VsText.SnapshotPoint snapPt)
		{
			return AdjustPosition(snapPt.Position, snapPt.Snapshot);
		}

		public int TranslateOffset(int offset, Microsoft.VisualStudio.Text.ITextSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException("snapshot");
			if (_snapshot == null) throw new InvalidOperationException("Model has no snapshot.");

			if (_snapshot != snapshot)
			{
				var pt = new Microsoft.VisualStudio.Text.SnapshotPoint(snapshot, offset).TranslateTo(_snapshot, Microsoft.VisualStudio.Text.PointTrackingMode.Positive);
				return pt.Position;
			}
			else
			{
				return offset;
			}
		}

		public string DumpTree()
		{
			return _file.DumpTreeText();
		}
		#endregion

		public IEnumerable<Token> FindTokens(int pos)
		{
			return _file.FindDownward(pos);
		}

		public IEnumerable<Token> FindTokens(int pos, Predicate<Token> pred)
		{
			return _file.FindDownward(pos, pred);
		}

		public string FileName
		{
			get { return _file.FileName; }
		}

		public string FileTitle
		{
			get { return _fileTitle; }
		}

		public CodeFile File
		{
			get { return _file; }
		}

		public ProbeAppSettings AppSettings
		{
			get { return _appSettings; }
		}

		public FileStore FileStore
		{
			get { return _store; }
		}

		public Span[] DisabledSections
		{
			get { return _disabledSections; }
			set { _disabledSections = value; }
		}

		#region Include Files
		private List<CodeFile> _implicitIncludes = new List<CodeFile>();

		public CodeFile GetIncludeFile(string sourceFileName, string fileName, bool searchCurrentDir, IEnumerable<string> parentFiles)
		{
			var includeFile = _store.GetIncludeFile(_appSettings, sourceFileName, fileName, searchCurrentDir, parentFiles);
			if (includeFile == null) return null;

			return includeFile.GetCodeFile(_appSettings, this, parentFiles);
		}

		public IEnumerable<CodeFile> ImplicitIncludes
		{
			get { return _implicitIncludes; }
		}
		#endregion

		#region Definitions
		public DefinitionProvider DefinitionProvider
		{
			get { return _defProvider; }
		}
		#endregion

		public PreprocessorModel PreprocessorModel
		{
			get { return _prepModel; }
			set { _prepModel = value; }
		}

		public ModelType ModelType
		{
			get { return _modelType; }
		}

		public string ClassName
		{
			get { return _className; }
		}

		public FileContext FileContext
		{
			get { return _fileContext; }
		}

		public IEnumerable<Preprocessor.Reference> PreprocessorReferences
		{
			get { return _prepRefs; }
			set { _prepRefs = value.ToArray(); }
		}

		public CodeSource Source
		{
			get { return _prepModel.Source; }
		}
	}

	internal enum ModelType
	{
		Other,
		Function,
		Class
	}
}
