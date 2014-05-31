using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DkTools.CodeProcessing
{
	internal class CodeLine
	{
		private CodeFile _file;
		private int _lineNum;
		private string _text;
		private bool _isLabel = false;
		private string _label = string.Empty;

		private Regex _rxLabel = new Regex(@"^\s*#label\s+(.+)$");

		public CodeLine(CodeFile file, int lineNum, string text)
		{
			_file = file;
			_lineNum = lineNum;
			_text = text;

			var match = _rxLabel.Match(_text);
			if (match.Success)
			{
				_isLabel = true;
				_label = match.Groups[1].Value.Trim();
			}
		}

		public bool IsMatch(Regex rx)
		{
			return rx.IsMatch(_text);
		}

		public Match Match(Regex rx)
		{
			return rx.Match(_text);
		}

		public CodeFile File
		{
			get { return _file; }
		}

		public int LineNum
		{
			get { return _lineNum; }
		}

		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		public string FileName
		{
			get { return _file != null ? _file.FileName : string.Empty; }
		}

		public bool IsLabel(string labelName)
		{
			return _isLabel && _label == labelName;
		}

		public int Length
		{
			get { return _text.Length; }
		}
	}
}
