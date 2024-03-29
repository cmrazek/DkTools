﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace DkTools.Tagging
{
	[Guid(GuidList.strTaggingOptions)]
	class TaggingOptions : DialogPage
	{
		[Category("Tagging")]
		[DisplayName("Initials")]
		[Description("Developer initials to appear in tags (e.g. \"ABC\")")]
		public string Initials { get; set; }

		[Category("Tagging")]
		[DisplayName("Work Item Number")]
		[Description("Assigned work item number (e.g. \"123456\")")]
		public string WorkOrder { get; set; }

		[Category("Tagging")]
		[DisplayName("Bug / Defect Number")]
		[Description("Assigned bug / defect number (e.g. \"Bug 123456\")")]
		public string Defect { get; set; }

		[Category("Dates")]
		[DisplayName("Date Format")]
		[Description("Date format for tags (e.g. \"mm/dd/yyyy\"")]
		public string DateFormat { get; set; }

		[Category("Diags")]
		[DisplayName("Initials in Diags")]
		[Description("Include the user's initials in the diag text?")]
		public bool InitialsInDiags { get; set; }

		[Category("Diags")]
		[DisplayName("File Name in Diags")]
		[Description("Include the file name in the diag text?")]
		public bool FileNameInDiags { get; set; }

		[Category("Diags")]
		[DisplayName("Function Name in Diags")]
		[Description("Include the function name in the diag text?")]
		public bool FunctionNameInDiags { get; set; }

		[Category("Diags")]
		[DisplayName("Todo After Diags")]
		[Description("Add a 'TODO' comment after each diag?")]
		public bool TodoAfterDiags { get; set; }

		public TaggingOptions()
		{
			this.DateFormat = Constants.DefaultDateFormat;
			this.InitialsInDiags = true;
			this.FileNameInDiags = true;
			this.FunctionNameInDiags = true;
			this.TodoAfterDiags = false;
		}
	}

    internal static class TaggingOptionsUtil
    {
		public static string GetDateFormatOrDefault(this TaggingOptions options)
        {
			var fmt = options.DateFormat;
			if (string.IsNullOrWhiteSpace(fmt)) return Constants.DefaultDateFormat;
			return fmt;
        }
	}
}
