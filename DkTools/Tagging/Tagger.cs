using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;

namespace DkTools.Tagging
{
	internal static class Tagger
	{
		public static void InsertDate()
		{
			var dte = Shell.DTE;
			var activeDoc = dte.ActiveDocument;
			if (activeDoc != null)
			{
				var options = ProbeToolsPackage.Instance.TaggingOptions;
				var dateFormat = options.DateFormat;
				if (string.IsNullOrWhiteSpace(dateFormat)) dateFormat = Constants.DefaultDateFormat;

				var sel = activeDoc.Selection as TextSelection;
				sel.Insert(DateTime.Now.ToString(options.DateFormat));
			}
		}

		public static string GetFileHeaderText(string fileName)
		{
			var options = ProbeToolsPackage.Instance.TaggingOptions;

			var workOrder = options.WorkOrder;
			if (workOrder == null) workOrder = string.Empty;

			var initials = options.Initials;
			if (initials == null) initials = string.Empty;

			var dateFormat = options.DateFormat;
			if (string.IsNullOrWhiteSpace(dateFormat)) dateFormat = Constants.DefaultDateFormat;

			var sb = new StringBuilder();
			sb.AppendLine("// -------------------------------------------------------------------------------------------------");
			sb.Append("// File Name: ");
			sb.AppendLine(Path.GetFileName(fileName));
			sb.AppendLine("//\t");
			sb.AppendLine("//");
			sb.AppendLine("// Modification History:");
			sb.AppendLine("//\tDate        Who #       Description of Changes");
			sb.AppendLine("//\t----------- --- ------- ------------------------------------------------------------------------");
			sb.Append("//\t");
			sb.Append(DateTime.Now.ToString(dateFormat));
			sb.Append("   ");
			sb.Append(initials.PadRight(4));
			sb.Append(workOrder.PadRight(8));

			var defect = options.Defect;
			if (!string.IsNullOrWhiteSpace(defect))
			{
				sb.Append(defect);
				sb.Append(" Created");
			}
			else
			{
				sb.Append("Created");
			}
			sb.AppendLine();
			sb.AppendLine("// -------------------------------------------------------------------------------------------------");
			sb.AppendLine();

			return sb.ToString();
		}

		public static void InsertFileHeader()
		{
			var dte = Shell.DTE;
			var activeDoc = dte.ActiveDocument;
			if (activeDoc != null)
			{
				var fileHeaderText = GetFileHeaderText(activeDoc.FullName);

				var sel = activeDoc.Selection as TextSelection;
				sel.StartOfDocument();
				sel.Insert(fileHeaderText);
			}
		}

		public static void InsertDiag()
		{
			var dte = Shell.DTE;
			var activeDoc = dte.ActiveDocument;
			if (activeDoc != null)
			{
				var options = ProbeToolsPackage.Instance.TaggingOptions;

				var sel = activeDoc.Selection as TextSelection;
				var selText = sel.Text.Trim();
				if (selText.IndexOf('\n') >= 0) selText = string.Empty;

				var sb = new StringBuilder();
				sb.Append("diag(\"");
				if (options.InitialsInDiags && !string.IsNullOrWhiteSpace(options.Initials))
				{
					sb.Append(options.Initials);
					sb.Append(": ");
				}

				if (options.FileNameInDiags)
				{
					sb.Append(Path.GetFileName(activeDoc.FullName));
					sb.Append(": ");
				}

				if (options.FunctionNameInDiags)
				{
					var funcName = string.Empty;
					var buf = Shell.ActiveBuffer;
					if (buf != null)
					{
						var model = CodeModelStore.GetModelForBuffer(buf, null, true);
						if (model != null)
						{
							var modelPos = model.GetPosition(Shell.ActiveView.Caret.Position.BufferPosition.Position, model.Snapshot);
							var funcToken = model.FindTokens(modelPos).LastOrDefault(t => t is CodeModel.FunctionToken) as CodeModel.FunctionToken;
							if (funcToken != null) funcName = funcToken.Name;
						}
					}

					if (!string.IsNullOrWhiteSpace(funcName))
					{
						sb.Append(funcName);
						sb.Append("(): ");
					}
				}

				if (!string.IsNullOrWhiteSpace(selText))
				{
					sb.Append(ProbeEnvironment.StringEscape(selText));
					sb.Append(" [\", ");
					sb.Append(selText);
					sb.Append(", \"]");
				}

				int lengthBefore = sb.Length;

				sb.Append("\\n\");");
				if (options.TodoAfterDiags) sb.Append("\t// TODO");

				sel.Insert(sb.ToString());

				if (string.IsNullOrWhiteSpace(selText))
				{
					sel.CharLeft(false, sb.Length - lengthBefore);
				}
			}
		}

		public static void InsertTag()
		{
			var dte = Shell.DTE;
			var activeDoc = dte.ActiveDocument;
			if (activeDoc == null) return;

			var options = ProbeToolsPackage.Instance.TaggingOptions;

			var sel = activeDoc.Selection as TextSelection;

			var sb = new StringBuilder();
			sb.Append("//");
			if (!string.IsNullOrWhiteSpace(options.Initials))
			{
				sb.Append(" ");
				sb.Append(options.Initials);
			}
			if (!string.IsNullOrWhiteSpace(options.WorkOrder))
			{
				sb.Append(" ");
				sb.Append(options.WorkOrder);
			}
			if (!string.IsNullOrWhiteSpace(options.Defect))
			{
				sb.Append(" ");
				sb.Append(options.Defect);
			}

			if (sel.TopPoint.AbsoluteCharOffset == sel.BottomPoint.AbsoluteCharOffset)
			{
				sb.Insert(0, "\t\t");
				sel.EndOfLine();
				sel.Insert(sb.ToString());
			}
			else
			{
				var topLine = sel.TopPoint.Line;
				var bottomLine = sel.BottomPoint.Line;

				sel.MoveToLineAndOffset(bottomLine, 1);
				sel.EndOfLine();
				sel.NewLine();
				sel.Insert(sb.ToString() + " End");
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstColumn, false);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, true);
				sel.Tabify();
				sel.EndOfLine();

				sel.MoveToLineAndOffset(topLine, 1);
				sel.LineUp();
				sel.EndOfLine();
				sel.NewLine();
				sel.Insert(sb.ToString() + " Start");
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstColumn, false);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, true);
				sel.Tabify();
				sel.EndOfLine();
			}
		}
	}
}
