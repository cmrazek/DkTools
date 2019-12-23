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
		public const long MultiLineCommentMask = 0xff;
		public const long Disabled = 0x100;
		public const long AfterInclude = 0x200;
		public const long StringLiteral_Single = 0x400;
		public const long StringLiteral_Double = 0x800;
		public const long StringLiteral_AfterBackslash = 0x1000;
		public const long StringLiteral_Mask = 0x1C00;
		public const long SingleLineComment = 0x2000;
		public const long DeadCodeMask = MultiLineCommentMask | SingleLineComment | StringLiteral_Mask | Disabled;

		public const long StatementMask = 0x7fffffff00000000;   // Tracks the layout of a statement
		public const int StatementShift = 32;

		/// <summary>
		/// Returns true if the state is not inside a comment, string literal or disabled code.
		/// </summary>
		public static bool IsInLiveCode(long state)
		{
			return (state & DeadCodeMask) == 0;
		}

		/// <summary>
		/// Returns true if the state is inside a multi-line comment.
		/// </summary>
		public static bool IsInsideMultiLineComment(long state)
		{
			return (state & State.MultiLineCommentMask) != 0;
		}

		/// <summary>
		/// Returns the StatementState embedded inside a classifier state.
		/// </summary>
		public static StatementCompletion.StatementState ToStatement(long state)
		{
			return new StatementCompletion.StatementState((int)(state >> StatementShift));
		}
	}
}
