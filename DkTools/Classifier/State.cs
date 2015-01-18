using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace DkTools.Classifier
{
	internal static class State
	{
		public const int MultiLineCommentMask = 0xff;
		public const int Disabled = 0x100;
		public const int AfterInclude = 0x200;
		public const int StringLiteral_Single = 0x400;
		public const int StringLiteral_Double = 0x800;
		public const int StringLiteral_AfterBackslash = 0x1000;
		public const int StringLiteral_Mask = 0x1C00;
		public const int SingleLineComment = 0x2000;
		public const int DeadCodeMask = MultiLineCommentMask | SingleLineComment | StringLiteral_Mask | Disabled;

		public const int StatementMask = 0x7fff0000;	// Tracks the layout of a statement

		/// <summary>
		/// Returns true if the state is not inside a comment, string literal or disabled code.
		/// </summary>
		public static bool IsInLiveCode(int state)
		{
			return (state & DeadCodeMask) == 0;
		}

		/// <summary>
		/// Returns true if the state is inside a multi-line comment.
		/// </summary>
		public static bool IsInsideMultiLineComment(int state)
		{
			return (state & State.MultiLineCommentMask) != 0;
		}

		/// <summary>
		/// Returns the StatementState embedded inside a classifier state.
		/// </summary>
		public static StatementCompletion.StatementState ToStatement(int state)
		{
			return (StatementCompletion.StatementState)(state >> 16);
		}
	}
}
