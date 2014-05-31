using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeProcessing
{
	internal class CodeFile
	{
		public string FileName { get; private set; }
		public bool Base { get; private set; }

		public CodeFile(string fileName, bool baseFile)
		{
			FileName = fileName;
			Base = baseFile;
		}
	}
}
