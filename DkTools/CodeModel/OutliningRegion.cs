using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class OutliningRegion
	{
		public Span Span { get; set; }
		public bool CollapseToDefinition { get; set; }
		public string Text { get; set; }
		public string TooltipText { get; set; }

		public OutliningRegion()
		{
			Text = Constants.DefaultOutliningText;
		}
	}
}
