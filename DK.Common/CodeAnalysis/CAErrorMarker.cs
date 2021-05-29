using DK.Code;

namespace DK.CodeAnalysis
{
	public struct CAErrorMarker
	{
		public string FilePath { get; private set; }
		public CodeSpan Span { get; private set; }

		public CAErrorMarker(string filePath, CodeSpan span)
		{
			FilePath = filePath;
			Span = span;
		}
	}
}
