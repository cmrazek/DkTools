using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal interface IPreprocessorReader
	{
		bool EOF { get; }
		char ReadChar();
		char ReadChar(out CodeAttributes att);
		char PeekChar();
		char PeekChar(out CodeAttributes att);
		string Peek(int numChars);
		bool MoveNext();
		bool MoveNext(int length);
		string ReadSegmentUntil(Func<char, bool> callback);
		string ReadSegmentUntil(Func<char, bool> callback, out CodeAttributes att);
		string ReadAllUntil(Func<char, bool> callback);
		string ReadIdentifier();
	}
}
