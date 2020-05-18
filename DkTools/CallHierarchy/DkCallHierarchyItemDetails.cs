using DkTools.CodeModel;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CallHierarchy
{
	class DkCallHierarchyItemDetails : ICallHierarchyItemDetails
	{
		private string _text;
		private int _lineNum;
		private int _linePos;
		private FilePosition _filePos;

		public DkCallHierarchyItemDetails(string text, int lineNum, int linePos, FilePosition filePos)
		{
			_text = text;
			_lineNum = lineNum;
			_linePos = linePos;
			_filePos = filePos;
		}

		public string Text => _text;
		public string File => _filePos.FileName;
		public int StartLine => _lineNum;
		public int StartColumn => _linePos;
		public int EndLine => _lineNum;
		public int EndColumn => _linePos;
		public bool SupportsNavigateTo => !_filePos.IsEmpty;

		public void NavigateTo()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!_filePos.IsEmpty) Shell.OpenDocument(_filePos);
		}
	}
}
