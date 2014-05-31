using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Text;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.Classifier
{
	internal static class ProbeClassificationDefinitions
	{
		#region Normal
		[Export]
		[Name("DK.Normal")]
		internal static ClassificationTypeDefinition Normal = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Normal")]
		[Name("DK.Normal")]
		[UserVisible(true)]
		internal sealed class NormalFormat : ClassificationFormatDefinition
		{
			public NormalFormat()
			{
				ForegroundColor = Colors.Black;
			}
		}
		#endregion

		#region Comment
		[Export]
		[Name("DK.Comment")]
		internal static ClassificationTypeDefinition Comment = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Comment")]
		[Name("DK.Comment")]
		[UserVisible(true)]
		internal sealed class CommentFormat : ClassificationFormatDefinition
		{
			public CommentFormat()
			{
				ForegroundColor = Colors.DarkGreen;
			}
		}
		#endregion

		#region Keyword
		[Export]
		[Name("DK.Keyword")]
		internal static ClassificationTypeDefinition Keyword = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Keyword")]
		[Name("DK.Keyword")]
		[UserVisible(true)]
		internal sealed class KeywordFormat : ClassificationFormatDefinition
		{
			public KeywordFormat()
			{
				ForegroundColor = Colors.Blue;
			}
		}
		#endregion

		#region Number
		[Export]
		[Name("DK.Number")]
		internal static ClassificationTypeDefinition Number = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Number")]
		[Name("DK.Number")]
		[UserVisible(true)]
		internal sealed class NumberFormat : ClassificationFormatDefinition
		{
			public NumberFormat()
			{
				ForegroundColor = Colors.DarkRed;
			}
		}
		#endregion

		#region StringLiteral
		[Export]
		[Name("DK.StringLiteral")]
		internal static ClassificationTypeDefinition StringLiteral = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.StringLiteral")]
		[Name("DK.StringLiteral")]
		[UserVisible(true)]
		internal sealed class StringLiteralFormat : ClassificationFormatDefinition
		{
			public StringLiteralFormat()
			{
				ForegroundColor = Colors.DarkRed;
			}
		}
		#endregion

		#region Preprocessor
		[Export]
		[Name("DK.Preprocessor")]
		internal static ClassificationTypeDefinition Preprocessor = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Preprocessor")]
		[Name("DK.Preprocessor")]
		[UserVisible(true)]
		internal sealed class PreprocessorFormat : ClassificationFormatDefinition
		{
			public PreprocessorFormat()
			{
				ForegroundColor = Colors.Gray;
			}
		}
		#endregion

		#region Inactive
		[Export]
		[Name("DK.Inactive")]
		internal static ClassificationTypeDefinition Inactive = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Inactive")]
		[Name("DK.Inactive")]
		[UserVisible(true)]
		internal sealed class InactiveFormat : ClassificationFormatDefinition
		{
			public InactiveFormat()
			{
				ForegroundColor = Colors.LightGray;
			}
		}
		#endregion

		#region TableName
		[Export]
		[Name("DK.TableName")]
		internal static ClassificationTypeDefinition TableName = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.TableName")]
		[Name("DK.TableName")]
		[UserVisible(true)]
		internal sealed class TableNameFormat : ClassificationFormatDefinition
		{
			public TableNameFormat()
			{
				ForegroundColor = Colors.SteelBlue;
			}
		}
		#endregion

		#region TableField
		[Export]
		[Name("DK.TableField")]
		internal static ClassificationTypeDefinition TableField = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.TableField")]
		[Name("DK.TableField")]
		[UserVisible(true)]
		internal sealed class TableFieldFormat : ClassificationFormatDefinition
		{
			public TableFieldFormat()
			{
				ForegroundColor = Colors.SteelBlue;
			}
		}
		#endregion

		#region Constant
		[Export]
		[Name("DK.Constant")]
		internal static ClassificationTypeDefinition Constant = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Constant")]
		[Name("DK.Constant")]
		[UserVisible(true)]
		internal sealed class ConstantFormat : ClassificationFormatDefinition
		{
			public ConstantFormat()
			{
				ForegroundColor = Colors.DarkBlue;
			}
		}
		#endregion

		#region DataType
		[Export]
		[Name("DK.DataType")]
		internal static ClassificationTypeDefinition DataType = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.DataType")]
		[Name("DK.DataType")]
		[UserVisible(true)]
		internal sealed class DataTypeFormat : ClassificationFormatDefinition
		{
			public DataTypeFormat()
			{
				ForegroundColor = Colors.Teal;
			}
		}
		#endregion

		#region Function
		[Export]
		[Name("DK.Function")]
		internal static ClassificationTypeDefinition Function = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Function")]
		[Name("DK.Function")]
		[UserVisible(true)]
		internal sealed class FunctionFormat : ClassificationFormatDefinition
		{
			public FunctionFormat()
			{
				ForegroundColor = Colors.DarkMagenta;
			}
		}
		#endregion

		#region Delimiter
		[Export]
		[Name("DK.Delimiter")]
		internal static ClassificationTypeDefinition Delimiter = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Delimiter")]
		[Name("DK.Delimiter")]
		[UserVisible(true)]
		internal sealed class DelimiterFormat : ClassificationFormatDefinition
		{
			public DelimiterFormat()
			{
				ForegroundColor = Colors.DimGray;
			}
		}
		#endregion

		#region Operator
		[Export]
		[Name("DK.Operator")]
		internal static ClassificationTypeDefinition Operator = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Operator")]
		[Name("DK.Operator")]
		[UserVisible(true)]
		internal sealed class OperatorFormat : ClassificationFormatDefinition
		{
			public OperatorFormat()
			{
				ForegroundColor = Colors.DimGray;
			}
		}
		#endregion
	}
}
