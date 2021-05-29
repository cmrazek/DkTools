using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis
{
	public class CodeAnalysisResults
	{
		public CAErrorTask[] Tasks { get; private set; }
		public CAErrorMarker[] Markers { get; private set; }

		public CodeAnalysisResults(IEnumerable<CAErrorTask> tasks, IEnumerable<CAErrorMarker> markers)
		{
			Tasks = tasks.ToArray();
			Markers = markers.ToArray();
		}
	}
}
