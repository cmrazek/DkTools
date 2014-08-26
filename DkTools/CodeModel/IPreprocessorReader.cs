using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal interface IPreprocessorReader
	{
		void SetWriter(IPreprocessorWriter writer);
		bool EOF { get; }

		char Peek();
		string Peek(int numChars);
		string PeekUntil(Func<char, bool> callback);
		string PeekIdentifier();

		void Use(int numChars);
		void UseUntil(Func<char, bool> callback);

		void Ignore(int numChars);
		void IgnoreUntil(Func<char, bool> callback);

		void Insert(string text);

		bool Suppress { get; set; }
	}
}
