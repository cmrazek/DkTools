using DK;
using DK.Code;
using System.Collections.Generic;
using System.IO;

namespace DkTools
{
	class CodeInfoStore
	{
		private Dictionary<string, string> _files = new Dictionary<string, string>();

		private string GetFileContent(string filePath)
		{
			if (_files.TryGetValue(filePath.ToLower(), out var content)) return content;

			if (File.Exists(filePath))
			{
				content = File.ReadAllText(filePath);
				_files[filePath.ToLower()] = content;
				return content;
			}

			return null;
		}

		public bool FilePositionToLineAndOffset(FilePosition filePos, out int lineNumOut, out int linePosOut)
		{
			if (filePos.IsEmpty)
			{
				lineNumOut = 0;
				linePosOut = 0;
				return false;
			}

			var content = GetFileContent(filePos.FileName);
			if (content == null)
			{
				lineNumOut = 0;
				linePosOut = 0;
				return false;
			}

			StringHelper.CalcLineAndPosFromOffset(content, filePos.Position, out lineNumOut, out linePosOut);
			return true;
		}

		private static readonly char[] _lineEndChars = new char[] { '\r', '\n' };

		public string GetTextLineAtFilePosition(FilePosition filePos)
		{
			if (filePos.IsEmpty) return null;

			var content = GetFileContent(filePos.FileName);
			if (content == null) return null;

			if (filePos.Position > content.Length) return null;

			int lineStart = 0;
			int index;
			if (filePos.Position > 0)
			{
				index = content.LastIndexOfAny(_lineEndChars, filePos.Position - 1);
				if (index >= 0) lineStart = index + 1;
			}

			int lineEnd = filePos.Position;
			index = content.IndexOfAny(_lineEndChars, lineEnd);
			if (index > lineEnd) lineEnd = index;

			return content.Substring(lineStart, lineEnd - lineStart);
		}
	}
}
