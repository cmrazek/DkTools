using DK.Code;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace DkTools
{
	static class CodeSpanUtil
	{
		public static TextSpan ToVsTextInteropSpan(this CodeSpan span, IVsTextView view)
		{
			if (span.End > span.Start)
			{
				int startLine, startPos, endLine, endPos;
				view.GetLineAndColumn(span.Start, out startLine, out startPos);
				view.GetLineAndColumn(span.End, out endLine, out endPos);
				return new TextSpan()
				{
					iStartLine = startLine,
					iStartIndex = startPos,
					iEndLine = endLine,
					iEndIndex = endPos
				};
			}
			else
			{
				int startLine, startPos;
				view.GetLineAndColumn(span.Start, out startLine, out startPos);
				return new TextSpan()
				{
					iStartLine = startLine,
					iStartIndex = startPos,
					iEndLine = startLine,
					iEndIndex = startPos
				};
			}
		}

		public static Span ToVsTextSpan(this CodeSpan span)
		{
			return new Span(span.Start, span.Length);
		}

		public static SnapshotSpan ToVsTextSnapshotSpan(this CodeSpan span, ITextSnapshot snapshot)
		{
			return new SnapshotSpan(snapshot, new Span(span.Start, span.Length));
		}
	}
}
