using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DkTools.CodeModel.Tokens
{
	internal class IncludeToken : GroupToken
	{
		private PreprocessorToken _prepToken;
		private string _fileName;
		private bool _processed = false;
		private bool _searchFileDir = false;
		private string _fullPathName;

		public class IncludeDef
		{
			public string SourceFileName { get; set; }
			public string FileName { get; set; }
			public bool SearchFileDir { get; set; }
		}

		private IncludeToken(Scope scope, Span span, PreprocessorToken prepToken, string fileName, bool searchFileDir)
			: base(scope)
		{
			AddToken(_prepToken = prepToken);
			_fileName = fileName;
			_searchFileDir = searchFileDir;
		}

		private static Regex _rxAngleBrackets = new Regex(@"^\<([^>]+)\>");
		private static Regex _rxQuotes = new Regex(@"^""([^""]+)""");

		public static IncludeToken Parse(Scope scope, PreprocessorToken prepToken)
		{
			var code = scope.Code;
			code.SkipWhiteSpace();
			var fileNameStartPos = code.Position;

			var fileName = string.Empty;
			var searchFileDir = false;
			StringLiteralToken fileNameToken = null;
			Span includeSpan = prepToken.Span;

			if (code.ReadIncludeStringLiteral())
			{
				fileName = code.Text;
				searchFileDir = fileName.StartsWith("\"");
				fileNameToken = new StringLiteralToken(scope, code.Span, code.Text);
				includeSpan = new Span(prepToken.Span.Start, fileNameToken.Span.End);

				if ((fileName.StartsWith("\"") && fileName.EndsWith("\"")) ||
					(fileName.StartsWith("<") && fileName.EndsWith(">")))
				{
					fileName = fileName.Substring(1, fileName.Length - 2);
				}
			}

			var ret = new IncludeToken(scope, includeSpan, prepToken, fileName, searchFileDir);
			if (fileNameToken != null) ret.AddToken(fileNameToken);
			return ret;
		}

		public override bool BreaksStatement
		{
			get
			{
				return true;
			}
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("fileName", _fileName);
			xml.WriteAttributeString("span", Span.ToString());
			base.DumpTreeInner(xml);
		}

		public bool Processed
		{
			get { return _processed; }
		}

		public override IEnumerable<IncludeDef> GetUnprocessedIncludes()
		{
			if (!_processed && !string.IsNullOrEmpty(_fileName))
			{
				return new IncludeDef[] { new IncludeDef { SourceFileName = Scope.File.FileName, FileName = _fileName, SearchFileDir = _searchFileDir } };
			}
			else
			{
				return new IncludeDef[0];
			}
		}

		public string FullPathName
		{
			get
			{
				if (_fullPathName == null)
				{
					var fileStore = Scope.FileStore;
					if (fileStore != null) _fullPathName = fileStore.LocateIncludeFile(File.FileName, _fileName, _searchFileDir);
					if (string.IsNullOrEmpty(_fullPathName)) _fullPathName = string.Empty;
				}
				return _fullPathName;
			}
		}
	}
}
