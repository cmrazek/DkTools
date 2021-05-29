using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Modeling.Tokens;
using DK.Preprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DK.Modeling
{
	public class CodeModel
	{
		private CodeFile _file;
		private string _fileName;
		private string _fileTitle;
		private DkAppSettings _appSettings;
		private FileStore _store;
		private DefinitionProvider _defProvider;
		private PreprocessorModel _prepModel;
		private CodeSpan[] _disabledSections;
		private ModelType _modelType;
		private string _className;
		private FileContext _fileContext;
		private Preprocessor.Reference[] _prepRefs;

		private CodeModel()
		{ }

		private CodeModel(DkAppSettings appSettings, FileStore store)
		{
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_store = store ?? throw new ArgumentNullException(nameof(store));
		}

		public static CodeModel CreateVisibleModelForPreprocessed(CodeSource visibleSource, DkAppSettings appSettings, FileStore store, PreprocessorModel prepModel)
		{
			var model = new CodeModel(appSettings, store);
			var codeFile = new CodeFile(model);

			model.Init(visibleSource, codeFile, prepModel.FileName, true, prepModel.DefinitionProvider);
			return model;
		}

		public static CodeModel CreateFullModelForPreprocessed(CodeSource source, DkAppSettings appSettings, FileStore store, PreprocessorModel prepModel)
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

			_fileContext = FileContextHelper.GetFileContextFromFileName(_fileName);

			if (FileContextHelper.FileNameIsFunction(fileName))
			{
				_modelType = ModelType.Function;
			}
			else if (FileContextHelper.FileNameIsClass(fileName, out _className))
			{
				_modelType = ModelType.Class;
			}
			else
			{
				_modelType = ModelType.Other;
			}

			_defProvider = defProvider;
			_file = new CodeFile(this);

			_file.Parse(source, _fileName, new string[0], visible);

			this.RefreshTime = DateTime.Now;
		}

		#region External Properties
		public DateTime LastAccessTime { get; set; }
		public DateTime RefreshTime { get; set; }

		public object Snapshot { get; set; }
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

		public string FilePath
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

		public DkAppSettings AppSettings
		{
			get { return _appSettings; }
		}

		public FileStore FileStore
		{
			get { return _store; }
		}

		public CodeSpan[] DisabledSections
		{
			get { return _disabledSections; }
			set { _disabledSections = value; }
		}

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

		internal IEnumerable<Preprocessor.Reference> PreprocessorReferences
		{
			get { return _prepRefs; }
			set { _prepRefs = value.ToArray(); }
		}

		public CodeSource Source
		{
			get { return _prepModel.Source; }
		}
	}

	public enum ModelType
	{
		Other,
		Function,
		Class
	}
}
