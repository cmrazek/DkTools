using System.Collections.Generic;

namespace DK
{
	enum TriState
	{
		False,
		Indeterminate,
		True
	}

	class TriStateUtil
	{
		public static TriState Combine(IEnumerable<TriState> values)
		{
			var gotFalse = false;
			var gotTrue = false;
			var gotIndeterminate = false;

			foreach (var value in values)
			{
				switch (value)
				{
					case TriState.False:
						gotFalse = true;
						break;
					case TriState.True:
						gotTrue = true;
						break;
					case TriState.Indeterminate:
						gotIndeterminate = true;
						break;
				}
			}

			if (gotIndeterminate) return TriState.Indeterminate;

			if (gotFalse)
			{
				if (gotTrue || gotIndeterminate) return TriState.Indeterminate;
				return TriState.False;
			}

			if (gotTrue)
			{
				if (gotFalse || gotIndeterminate) return TriState.Indeterminate;
				return TriState.True;
			}

			return TriState.False;
		}
	}
}
