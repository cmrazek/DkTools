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

		public CodeModel(string source, VsText.ITextSnapshot snapshot, string fileName)
		{
			var codeSource = new CodeSource();
			codeSource.Append(fileName, Position.Start, source);

			Init(codeSource, fileName, true);
		}

		public CodeModel(VsText.ITextSnapshot snapshot)
		{
			var source = snapshot.GetText();
			var fileName = CodeModelStore.GetFileNameForBuffer(snapshot.TextBuffer);

			var codeSource = new CodeSource();
			codeSource.Append(fileName, Position.Start, source);
			codeSource.Snapshot = snapshot;

			Init(codeSource, fileName, true);
		}

		public CodeModel(CodeSource source, string fileName, bool visible)
		{
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
		private static Dictionary<string, CodeFile> _cachedIncludeFiles = new Dictionary<string, CodeFile>();
		private List<CodeFile> _implicitIncludes = new List<CodeFile>();

		public CodeFile GetIncludeFile(string sourceFileName, string fileName, bool searchCurrentDir, IEnumerable<string> parentFiles)
		{
			if (string.IsNullOrEmpty(fileName)) return null;

			CodeFile file = null;
			string key;

			if (searchCurrentDir)
			{
				if (!string.IsNullOrEmpty(sourceFileName))
				{
					key = string.Concat(sourceFileName, ">", fileName).ToLower();
					lock (_cachedIncludeFiles)
					{
						_cachedIncludeFiles.TryGetValue(key, out file);
					}
					if (file != null) return file;

					var pathName = Path.Combine(Path.GetDirectoryName(sourceFileName), fileName);
					if (System.IO.File.Exists(pathName)) file = ProcessIncludeFile(pathName, parentFiles);
					else if (System.IO.File.Exists(pathName + "&")) file = ProcessIncludeFile(pathName + "&", parentFiles);

					if (file != null)
					{
						lock (_cachedIncludeFiles)
						{
							_cachedIncludeFiles[key] = file;
						}
						return file;
					}
				}
			}

			key = fileName.ToLower();
			lock (_cachedIncludeFiles)
			{
				_cachedIncludeFiles.TryGetValue(key, out file);
			}
			if (file != null) return file;

			foreach (var includeDir in ProbeEnvironment.IncludeDirs)
			{
				var pathName = Path.Combine(includeDir, fileName);
				if (System.IO.File.Exists(pathName)) file = ProcessIncludeFile(pathName, parentFiles);
				else if (System.IO.File.Exists(pathName + "&")) file = ProcessIncludeFile(pathName + "&", parentFiles);

				if (file != null) break;
			}

			if (file != null)
			{
				lock (_cachedIncludeFiles)
				{
					_cachedIncludeFiles[key] = file;
				}
			}

			return file;
		}

		private CodeFile ProcessIncludeFile(string fullPathName, IEnumerable<string> parentFiles)
		{
			try
			{
				Trace.WriteLine(string.Concat("Processing include file: ", fullPathName));

				if (parentFiles.Any(x => x.Equals(fullPathName, StringComparison.OrdinalIgnoreCase)))
				{
					Log.Write(EventLogEntryType.Warning, string.Format("Cyclical include found for file '{0}'", fullPathName));
					return null;
				}

				var merger = new FileMerger();
				merger.MergeFile(fullPathName, true);

				var content = merger.MergedContent;
				if (content == null) return null;

				var file = new CodeFile(this);
				file.IsMixed = merger.IsMixed;
				file.Parse(content, fullPathName, parentFiles, false);
				return file;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(string.Format("Exception when merging file '{0}': {1}", fullPathName, ex));
				return null;
			}
		}

		public static void OnFileSaved(string fileName)
		{
			// Purge all include files that have this file name, so they get reloaded when the next code model is built.

			var fileTitle = Path.GetFileNameWithoutExtension(fileName);
			var fileExt = Path.GetExtension(fileName);
			if (fileExt.EndsWith("&")) fileExt = fileExt.Substring(0, fileExt.Length - 1);

			var removeList = new List<string>();

			lock (_cachedIncludeFiles)
			{
				foreach (var key in _cachedIncludeFiles.Keys)
				{
					var index = key.IndexOf('>');
					var cachedFileName = index >= 0 ? key.Substring(index + 1) : key;
					if (cachedFileName.EndsWith("&")) cachedFileName = cachedFileName.Substring(0, cachedFileName.Length - 1);

					if (string.Equals(Path.GetFileNameWithoutExtension(cachedFileName), fileTitle, StringComparison.OrdinalIgnoreCase) &&
						string.Equals(Path.GetExtension(cachedFileName), fileExt, StringComparison.OrdinalIgnoreCase))
					{
						removeList.Add(key);
					}
				}

				foreach (var rem in removeList) _cachedIncludeFiles.Remove(rem);
			}
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
