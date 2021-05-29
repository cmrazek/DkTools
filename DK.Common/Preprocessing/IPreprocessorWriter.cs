using DK.Code;

namespace DK.Preprocessing
{
	public interface IPreprocessorWriter
	{
		void Append(string text, CodeAttributes attribs);
		void Append(CodeSource source);
		void Flush();
		int Position { get; }
	}
}
