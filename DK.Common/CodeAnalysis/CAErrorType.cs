namespace DK.CodeAnalysis
{
	public enum CAErrorType
	{
		Error,
		Warning,
		ReportOutputTag,
	}

	public static class CAErrorTypeHelper
	{
		public static CAErrorType? Combine(this CAErrorType? a, CAErrorType? b)
		{
			if (!a.HasValue && !b.HasValue) return null;
			if (a.HasValue && !b.HasValue) return a.Value;
			if (!a.HasValue && b.HasValue) return b.Value;
			if (a.Value == CAErrorType.Error || b.Value == CAErrorType.Error) return CAErrorType.Error;
			return a.Value;
		}
	}
}
