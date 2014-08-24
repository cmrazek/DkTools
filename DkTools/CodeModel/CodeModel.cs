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

		public CodeModel(FileStore store, string source, VsText.ITextSnapshot snapshot, string fileName)
		{
			if (store == null) throw new ArgumentNullException("store");
			_store = store;

			var codeSource = new CodeSource();
			codeSource.Append(source, new CodeAttributes(fileName, Position.Start, true));

			Init(codeSource, fileName, true);
		}

		public CodeModel(VsText.ITextSnapshot snapshot)
		{
			var source = snapshot.GetText();
			var fileName = snapshot.TextBuffer.TryGetFileName();
			_store = FileStore.GetOrCreateForTextBuffer(snapshot.TextBuffer);

			var codeSource = new CodeSource();
			codeSource.Append(source, new CodeAttributes(fileName, Position.Start, true));
			codeSource.Snapshot = snapshot;

			Init(codeSource, fileName, true);
		}

		public CodeModel(FileStore store, CodeSource source, string fileName, bool visible)
		{
			if (store == null) throw new ArgumentNullException("store");
			_store = store;

			Init(source, fileName, visible);
		}

		private void Init(CodeSource source, string fileName, bool visible)
		{
#if DEBUG
			Log.WriteDebug("Building code model for file [{0}]", fileName);
#endif
			this.RefreshTime = DateTime.Now;

			_fileName = fileName;
			_file = new CodeFile(this);

			if (!string.IsNullOrEmpty(_fileName))
			{
				switch (Path.GetFileName(_fileName).ToLower())
				{
					case "stdlib.i":
					case "stdlib.i&":
						// Don't include this file if the user happens to have stdlib.i open right now.
						break;
					default:
						{
							var inclFile = GetIncludeFile(string.Empty, "stdlib.i", false, new string[0]);
							if (inclFile != null) _implicitIncludes.Add(inclFile);
						}
						break;
				}
			}

			_file.AddDefinition(new FunctionDefinition("diag", null, DataType.Void, "void diag(expressions ...)"));
			_file.AddDefinition(new FunctionDefinition("gofield", null, DataType.Void, "void gofield(TableName.ColumnName)"));
			_file.AddDefinition(new FunctionDefinition("makestring", null, DataType.FromString("char(255)"), "char(255) makestring(expressions ...)"));
			_file.AddDefinition(new FunctionDefinition("oldvalue", null, DataType.Void, "oldvalue(TableName.ColumnName)"));
			_file.AddDefinition(new FunctionDefinition("qcolsend", null, DataType.Void, "void qcolsend(TableName.ColumnName ...)"));
			_file.AddDefinition(new FunctionDefinition("SetMessage", null, DataType.Int, "int SetMessage(MessageControlString, expressions ...)"));
			_file.AddDefinition(new FunctionDefinition("STRINGIZE", null, DataType.FromString("char(255)"), "STRINGIZE(x)"));

			foreach (var def in ProbeEnvironment.DictDefinitions) _file.AddDefinition(def);

			foreach (var incl in _implicitIncludes) _file.AddImplicitInclude(incl);

			foreach (var def in ProbeToolsPackage.Instance.FunctionFileScanner.AllDefinitions) _file.AddDefinition(def);

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
	}
}
