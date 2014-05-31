using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace DkTools.TokenParser
{
	/// <summary>
	/// This class is designed to determine if a position is inside a comment.
	/// It can be difficult for text functions to determine if they're inside a multi-line comment.
	/// </summary>
	internal class CommentTracker
	{
		private string _source;
		private bool _sourceStartsInsideMultiLineComment;

		private static Regex _rxStart;

		/// <summary>
		/// Creates the comment tracker.
		/// </summary>
		/// <param name="source">Source code to be analyzed.</param>
		public CommentTracker(string source, bool sourceStartsInsideMultiLineComment = false)
		{
			if (source == null) throw new ArgumentNullException("source");

			_source = source;
			_sourceStartsInsideMultiLineComment = sourceStartsInsideMultiLineComment;
		}

		/// <summary>
		/// Determines if the position is inside a comment.
		/// </summary>
		/// <param name="testPos">The position to be tested.</param>
		/// <returns>True if it is inside a comment; otherwise false.</returns>
		public bool PositionInsideComment(int testPos)
		{
			var pos = 0;
			var length = _source.Length < testPos ? _source.Length : testPos;
			int end;
			char startCh, ch;

			if (_sourceStartsInsideMultiLineComment)
			{
				end = _source.IndexOf("*/");
				if (end < 0 || end >= testPos) return true;
				pos = end + 2;
			}

			if (_rxStart == null) _rxStart = new Regex(@"(?://|/\*|""|')");

			while (pos < length)
			{
				var match = _rxStart.Match(_source, pos);
				if (match.Success)
				{
					if (match.Index >= testPos) return false;

					switch (match.Value)
					{
						case "//":
							end = _source.IndexOf('\n', match.Index);
							if (end < 0 || end >= testPos) return true;
							pos = end + 1;
							break;

						case "/*":
							end = _source.IndexOf("*/", match.Index);
							if (end < 0 || end >= testPos) return true;
							pos = end + 2;
							break;

						case "\"":
						case "\'":
							startCh = match.Value[0];
							pos = match.Index + 1;
							while (pos < length)
							{
								ch = _source[pos];
								if (ch == '\\')
								{
									pos += 2;
								}
								else if (ch == startCh)
								{
									pos++;
									break;
								}
								else
								{
									pos++;
								}
							}
							if (pos >= testPos) return false;
							break;

						default:
							// Should never happen
							pos++;
							break;
					}
				}
				else return false;
			}

			return false;
		}
	}
}
