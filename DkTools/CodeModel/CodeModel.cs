using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	internal class CodeModel
	{
		private CodeFile _file;
		private string _fileName;
		private Microsoft.VisualStudio.Text.ITextSnapshot _snapshot;
		private FileStore _store;
		private DefinitionProvider _defProvider;
		private CodeModel _prepModel;

		public CodeModel(FileStore store, string source, VsText.ITextSnapshot snapshot, string fileName)
		{
			if (store == null) throw new ArgumentNullException("store");
			_store = store;

			var codeSource = new CodeSource();
			codeSource.Append(source, new CodeAttributes(fileName, Position.Start, Position.Start.Advance(source), true, true));
			codeSource.Flush();

			// This is a visible model, so don't create definitions.
			var defProvider = new DefinitionProvider();
			defProvider.CreateDefinitions = false;

			Init(codeSource, fileName, true, defProvider);
		}

		public CodeModel(VsText.ITextSnapshot snapshot)
		{
			var source = snapshot.GetText();
			var fileName = snapshot.TextBuffer.TryGetFileName();
			_store = FileStore.GetOrCreateForTextBuffer(snapshot.TextBuffer);

			var codeSource = new CodeSource();
			codeSource.Append(source, new CodeAttributes(fileName, Position.Start, Position.Start.Advance(source), true, true));
			codeSource.Flush();
			codeSource.Snapshot = snapshot;

			// This is a visible model, so don't create definitions.
			var defProvider = new DefinitionProvider();
			defProvider.CreateDefinitions = false;

			Init(codeSource, fileName, true, defProvider);
		}

		public CodeModel(FileStore store, CodeSource source, string fileName, bool visible, DefinitionProvider defProvider)
		{
			if (store == null) throw new ArgumentNullException("store");
			_store = store;

			Init(source, fileName, visible, defProvider);
		}

		private void Init(CodeSource source, string fileName, bool visible, DefinitionProvider defProvider)
		{
#if DEBUG
			if (defProvider == null) throw new ArgumentNullException("defProvider");
			//Log.WriteDebug("Building code model for file [{0}]", fileName);
#endif
			this.RefreshTime = DateTime.Now;

			_fileName = fileName;
			_defProvider = defProvider;
			_file = new CodeFile(this);

			if (_defProvider.CreateDefinitions)
			{
				var defs = new List<Definition>();
				defs.Add(new FunctionDefinition("diag", null, DataType.Void, "void diag(expressions ...)"));
				defs.Add(new FunctionDefinition("gofield", null, DataType.Void, "void gofield(TableName.ColumnName)"));
				defs.Add(new FunctionDefinition("makestring", null, DataType.FromString("char(255)"), "char(255) makestring(expressions ...)"));
				defs.Add(new FunctionDefinition("oldvalue", null, DataType.Void, "oldvalue(TableName.ColumnName)"));
				defs.Add(new FunctionDefinition("qcolsend", null, DataType.Void, "void qcolsend(TableName.ColumnName ...)"));
				defs.Add(new FunctionDefinition("SetMessage", null, DataType.Int, "int SetMessage(MessageControlString, expressions ...)"));
				defs.Add(new FunctionDefinition("STRINGIZE", null, DataType.FromString("char(255)"), "STRINGIZE(x)"));
				defs.Add(new FunctionDefinition("UNREFERENCED_PARAMETER", null, DataType.Void, "UNREFERENCED_PARAMETER(parameter)"));

				foreach (var def in ProbeEnvironment.DictDefinitions) defs.Add(def);

				foreach (var def in ProbeToolsPackage.Instance.FunctionFileScanner.AllDefinitions) defs.Add(def);

				_file.AddDefinitions(defs);
			}
			else CopyDefinitionsFromProvider();

			_file.Parse(source, _fileName, new string[0], visible);

			if (_defProvider.CreateDefinitions) CopyDefinitionsToProvider();

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
		public IEnumerable<Microsoft.VisualStudio.TextManager.Interop.TextSpan> BraceMatching(int lineNum, int linePos)
		{
			var pos = _file.FindPosition(lineNum, linePos);
			var token = _file.FindTokenOfType(pos, typeof(IBraceMatchingToken));
			if (token == null) token = _file.FindNearbyTokenOfType(pos, typeof(IBraceMatchingToken));
			if (token != null && typeof(IBraceMatchingToken).IsAssignableFrom(token.GetType()))
			{
				var bm = token as IBraceMatchingToken;
				return (from t in bm.BraceMatchingTokens select t.Span.ToVsTextInteropSpan());
			}
			else
			{
				return new Microsoft.VisualStudio.TextManager.Interop.TextSpan[0];
			}
		}

		public IEnumerable<Span> FindMatchingBraces(int offset)
		{
			var pos = _file.FindPosition(offset);
			var token = _file.FindTokenOfType(pos, typeof(IBraceMatchingToken));
			if (token == null) token = _file.FindNearbyTokenOfType(pos, typeof(IBraceMatchingToken));
			if (token != null && typeof(IBraceMatchingToken).IsAssignableFrom(token.GetType()))
			{
				var bm = token as IBraceMatchingToken;
				return (from t in bm.BraceMatchingTokens select t.Span).ToArray();
			}
			else
			{
				return new Span[0];
			}
		}

		public IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				return _file.OutliningRegions;
			}
		}
		#endregion

		#region Util functions
		public Position GetPosition(int lineNum, int linePos)
		{
			return _file.FindPosition(lineNum, linePos);
		}

		public Position GetPosition(int offset)
		{
			return _file.FindPosition(offset);
		}

		public Position GetPosition(int offset, VsText.ITextSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException("snapshot");
			if (_snapshot == null) throw new InvalidOperationException("Model has no snapshot.");

			if (_snapshot != snapshot)
			{
				var pt = new Microsoft.VisualStudio.Text.SnapshotPoint(snapshot, offset).TranslateTo(_snapshot, Microsoft.VisualStudio.Text.PointTrackingMode.Positive);
				return GetPosition(pt.Position);
			}
			else
			{
				return GetPosition(offset);
			}
		}

		public Position GetPosition(VsText.SnapshotPoint snapPt)
		{
			return GetPosition(snapPt.Position, snapPt.Snapshot);
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

		public IEnumerable<FunctionToken> LocalFunctions
		{
			get { return _file.LocalFunctions; }
		}

		public IEnumerable<Token> FindTokens(Position pos)
		{
			return _file.FindDownward(pos);
		}

		public IEnumerable<Token> FindTokens(Position pos, Predicate<Token> pred)
		{
			return _file.FindDownward(pos, pred);
		}

		public IEnumerable<Token> FindTokens(int offset)
		{
			return _file.FindDownward(offset);
		}

		public string FileName
		{
			get { return _file.FileName; }
		}

		public CodeFile File
		{
			get { return _file; }
		}

		#region Include Files
		private List<CodeFile> _implicitIncludes = new List<CodeFile>();

		public CodeFile GetIncludeFile(string sourceFileName, string fileName, bool searchCurrentDir, IEnumerable<string> parentFiles)
		{
			var includeFile = _store.GetIncludeFile(sourceFileName, fileName, searchCurrentDir, parentFiles);
			if (includeFile == null) return null;

			return includeFile.GetCodeFile(this, parentFiles);
		}

		public IEnumerable<CodeFile> ImplicitIncludes
		{
			get { return _implicitIncludes; }
		}
		#endregion

		/// <summary>
		/// Gets a list of definitions that match this name.
		/// </summary>
		/// <param name="name">The name to match</param>
		/// <returns>A list of definitions that match the provided name.</returns>
		public IEnumerable<Definition> GetDefinitions(string name)
		{
			return _file.GetDefinitions(name);
		}

		/// <summary>
		/// Gets a list of definitions that match this name with a specific type.
		/// </summary>
		/// <typeparam name="T">The definition type to search for</typeparam>
		/// <param name="name">The name to match</param>
		/// <returns>A list of definitions that match the provided name.</returns>
		public IEnumerable<T> GetDefinitions<T>(string name) where T : Definition
		{
			return _file.GetDefinitions<T>(name);
		}

		public IEnumerable<Definition> GetDefinitions()
		{
			return _file.GetDefinitions();
		}

		public IEnumerable<T> GetDefinitions<T>() where T : Definition
		{
			return _file.GetDefinitions<T>();
		}

		private void CopyDefinitionsToProvider()
		{
			foreach (var def in _file.GetDefinitions())
			{
				_defProvider.AddGlobalDefinition(def);
			}

			var source = _file.CodeSource;
			foreach (var defLoc in _file.GetDescendentDefinitionLocations())
			{
				string fileName;
				Position pos;
				bool primaryFile;
				source.GetFilePosition(defLoc.Definition.SourceSpan.Start.Offset, out fileName, out pos, out primaryFile);
				if (primaryFile)
				{
					var offsetDiff = pos.Offset - defLoc.Definition.SourceSpan.Start.Offset;
					_defProvider.AddLocalDefinition(defLoc.LocalFileOffset + offsetDiff, defLoc.Definition);
				}
			}
		}

		private void CopyDefinitionsFromProvider()
		{
			_file.AddDefinitions(_defProvider.GlobalDefinitions);

			// Local definitions are handled in the token constructor.
		}

		public DefinitionProvider DefinitionProvider
		{
			get { return _defProvider; }
		}

		public CodeModel PreprocessorModel
		{
			get { return _prepModel; }
			set { _prepModel = value; }
		}
	}
}
