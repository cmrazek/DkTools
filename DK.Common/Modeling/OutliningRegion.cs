using DK.Code;

namespace DK.Modeling
{
	public class OutliningRegion
	{
		public CodeSpan Span { get; set; }
		public bool CollapseToDefinition { get; set; }
		public string Text { get; set; }
		public string TooltipText { get; set; }

		public OutliningRegion()
		{
			Text = Constants.DefaultOutliningText;
		}
	}
}
