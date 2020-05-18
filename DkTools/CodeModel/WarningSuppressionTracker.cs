using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	class WarningSuppressionTracker
	{
		public Dictionary<int, List<Span>> _warnings = new Dictionary<int, List<Span>>();

		public const int WarningPrefix = 990000;

		public void OnWarnAdd(int number, int pos)
		{
			if (number - WarningPrefix < 0) return;

			if (_warnings.TryGetValue(number, out var supps))
			{
				// Close off a suppression range.

				var supp = supps.Last();
				if (supp.End == int.MaxValue)
				{
					// Close off the open suppression
					supps[supps.Count - 1] = new Span(supp.Start, pos);
				}
				else
				{
					// It's already closed, so don't need to re-close it.
				}
			}
			else // No suppression lists found for this number, yet.
			{
				// Wasn't suppressed so no need to close anything off.
			}
		}

		public void OnWarnDel(int number, int pos)
		{
			if (number - WarningPrefix < 0) return;

			if (_warnings.TryGetValue(number, out var supps))
			{
				// Start a suppression range.

				var supp = supps.Last();
				if (supp.End == int.MaxValue)
				{
					// Last range is not closed, so don't need to start a new one.
				}
				else
				{
					// Add a new suppression range with no closing position.
					supps.Add(new Span(pos, int.MaxValue));
				}
			}
			else // No suppression lists found for this number, yet.
			{
				// Start a new suppression range.
				supps = new List<Span>();
				supps.Add(new Span(pos, int.MaxValue));
				_warnings[number] = supps;
			}
		}

		public bool IsWarningSuppressed(int number, int pos)
		{
			if (_warnings.TryGetValue(number + WarningPrefix, out var supps))
			{
				foreach (var supp in supps)
				{
					if (supp.Contains(pos)) return true;
				}
			}

			return false;
		}
	}
}
