using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal interface IPreprocessorWriter
	{
		void Append(string text, CodeAttributes attribs);
		void Append(CodeSource source);
		void Flush();
	}
}
