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

		private IncludeToken(GroupToken parent, Scope scope, Span span, PreprocessorToken prepToken, string fileName, bool searchFileDir)
			: base(parent, scope, new Token[] { prepToken })
		{
			_prepToken = prepToken;
			_fileName = fileName;
			_searchFileDir = searchFileDir;
		}

		private static Regex _rxAngleBrackets = new Regex(@"^\<([^>]+)\>");
		private static Regex _rxQuotes = new Regex(@"^""([^""]+)""");

		public static IncludeToken Parse(GroupToken parent, Scope scope, PreprocessorToken prepToken)
		{
			var file = scope.File;

			file.SkipWhiteSpaceAndComments(scope);

			var fileNameStartPos = file.Position;

			file.SeekEndOfLine();
			var lineText = file.GetText(new Span(fileNameStartPos, file.Position)).Trim();

			var fileName = "";
			var searchFileDir = false;
			StringLiteralToken stringLitToken = null;

			var match = _rxAngleBrackets.Match(lineText);
			if (match.Success)
			{
				fileName = match.Groups[1].Value.Trim();

				var rawFileName = lineText.Substring(0, match.Groups[1].Index + match.Groups[1].Length + 1);
				stringLitToken = new StringLiteralToken(parent, scope, new Span(fileNameStartPos, fileNameStartPos.Advance(rawFileName)), rawFileName);
			}
			else if ((match = _rxQuotes.Match(lineText)).Success)
			{
				fileName = match.Groups[1].Value.Trim();
				searchFileDir = true;

				var rawFileName = lineText.Substring(0, match.Groups[1].Index + match.Groups[1].Length + 1);
				stringLitToken = new StringLiteralToken(parent, scope, new Span(fileNameStartPos, fileNameStartPos.Advance(rawFileName)), rawFileName);
			}

			var ret = new IncludeToken(parent, scope, new Span(prepToken.Span.Start, file.Position), prepToken, fileName, searchFileDir);
			if (stringLitToken != null) ret.AddToken(stringLitToken);
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
