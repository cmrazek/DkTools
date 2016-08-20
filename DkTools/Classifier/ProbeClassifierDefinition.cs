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
		[Name("DK.Normal.Light")]
		internal static ClassificationTypeDefinition NormalLight = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Normal.Light")]
		[Name("DK.Normal.Light")]
		[UserVisible(true)]
		internal sealed class NormalFormatLight : ClassificationFormatDefinition
		{
			public NormalFormatLight()
			{
				ForegroundColor = Colors.Black;
			}
		}

		[Export]
		[Name("DK.Normal.Dark")]
		internal static ClassificationTypeDefinition NormalDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Normal.Dark")]
		[Name("DK.Normal.Dark")]
		[UserVisible(true)]
		internal sealed class NormalFormatDark : ClassificationFormatDefinition
		{
			public NormalFormatDark()
			{
				ForegroundColor = Colors.Azure;
			}
		}
		#endregion

		#region Comment
		[Export]
		[Name("DK.Comment.Light")]
		internal static ClassificationTypeDefinition Comment = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Comment.Light")]
		[Name("DK.Comment.Light")]
		[UserVisible(true)]
		internal sealed class CommentFormatLight : ClassificationFormatDefinition
		{
			public CommentFormatLight()
			{
				ForegroundColor = Colors.DarkGreen;
			}
		}

		[Export]
		[Name("DK.Comment.Dark")]
		internal static ClassificationTypeDefinition CommentDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Comment.Dark")]
		[Name("DK.Comment.Dark")]
		[UserVisible(true)]
		internal sealed class CommentFormatDark : ClassificationFormatDefinition
		{
			public CommentFormatDark()
			{
				ForegroundColor = Colors.LimeGreen;
			}
		}
		#endregion

		#region Keyword
		[Export]
		[Name("DK.Keyword.Light")]
		internal static ClassificationTypeDefinition KeywordLight = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Keyword.Light")]
		[Name("DK.Keyword.Light")]
		[UserVisible(true)]
		internal sealed class KeywordFormatLight : ClassificationFormatDefinition
		{
			public KeywordFormatLight()
			{
				ForegroundColor = Colors.Blue;
			}
		}

		[Export]
		[Name("DK.Keyword.Dark")]
		internal static ClassificationTypeDefinition KeywordDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Keyword.Dark")]
		[Name("DK.Keyword.Dark")]
		[UserVisible(true)]
		internal sealed class KeywordFormatDark : ClassificationFormatDefinition
		{
			public KeywordFormatDark()
			{
				ForegroundColor = Colors.DodgerBlue;
			}
		}
		#endregion

		#region Number
		[Export]
		[Name("DK.Number.Light")]
		internal static ClassificationTypeDefinition Number = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Number.Light")]
		[Name("DK.Number.Light")]
		[UserVisible(true)]
		internal sealed class NumberFormat : ClassificationFormatDefinition
		{
			public NumberFormat()
			{
				ForegroundColor = Colors.DarkRed;
			}
		}

		[Export]
		[Name("DK.Number.Dark")]
		internal static ClassificationTypeDefinition NumberDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Number.Dark")]
		[Name("DK.Number.Dark")]
		[UserVisible(true)]
		internal sealed class NumberFormatDark : ClassificationFormatDefinition
		{
			public NumberFormatDark()
			{
				ForegroundColor = Colors.OrangeRed;
			}
		}
		#endregion

		#region StringLiteral
		[Export]
		[Name("DK.StringLiteral.Light")]
		internal static ClassificationTypeDefinition StringLiteral = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.StringLiteral.Light")]
		[Name("DK.StringLiteral.Light")]
		[UserVisible(true)]
		internal sealed class StringLiteralFormat : ClassificationFormatDefinition
		{
			public StringLiteralFormat()
			{
				ForegroundColor = Colors.DarkRed;
			}
		}

		[Export]
		[Name("DK.StringLiteral.Dark")]
		internal static ClassificationTypeDefinition StringLiteralDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.StringLiteral.Dark")]
		[Name("DK.StringLiteral.Dark")]
		[UserVisible(true)]
		internal sealed class StringLiteralFormatDark : ClassificationFormatDefinition
		{
			public StringLiteralFormatDark()
			{
				ForegroundColor = Colors.OrangeRed;
			}
		}
		#endregion

		#region Preprocessor
		[Export]
		[Name("DK.Preprocessor.Light")]
		internal static ClassificationTypeDefinition Preprocessor = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Preprocessor.Light")]
		[Name("DK.Preprocessor.Light")]
		[UserVisible(true)]
		internal sealed class PreprocessorFormat : ClassificationFormatDefinition
		{
			public PreprocessorFormat()
			{
				ForegroundColor = Colors.Gray;
			}
		}

		[Export]
		[Name("DK.Preprocessor.Dark")]
		internal static ClassificationTypeDefinition PreprocessorDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Preprocessor.Dark")]
		[Name("DK.Preprocessor.Dark")]
		[UserVisible(true)]
		internal sealed class PreprocessorFormatDark : ClassificationFormatDefinition
		{
			public PreprocessorFormatDark()
			{
				ForegroundColor = Colors.Gray;
			}
		}
		#endregion

		#region Inactive
		[Export]
		[Name("DK.Disabled.Light")]
		internal static ClassificationTypeDefinition DisabledLight = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Disabled.Light")]
		[Name("DK.Disabled.Light")]
		[UserVisible(true)]
		internal sealed class DisabledFormatLight : ClassificationFormatDefinition
		{
			public DisabledFormatLight()
			{
				ForegroundColor = Colors.LightGray;
			}
		}

		[Export]
		[Name("DK.Disabled.Dark")]
		internal static ClassificationTypeDefinition DisabledDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Disabled.Dark")]
		[Name("DK.Disabled.Dark")]
		[UserVisible(true)]
		internal sealed class DisabledFormatDark : ClassificationFormatDefinition
		{
			public DisabledFormatDark()
			{
				ForegroundColor = Colors.DimGray;
			}
		}
		#endregion

		#region TableName
		[Export]
		[Name("DK.TableName.Light")]
		internal static ClassificationTypeDefinition TableName = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.TableName.Light")]
		[Name("DK.TableName.Light")]
		[UserVisible(true)]
		internal sealed class TableNameFormat : ClassificationFormatDefinition
		{
			public TableNameFormat()
			{
				ForegroundColor = Colors.SteelBlue;
			}
		}

		[Export]
		[Name("DK.TableName.Dark")]
		internal static ClassificationTypeDefinition TableNameDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.TableName.Dark")]
		[Name("DK.TableName.Dark")]
		[UserVisible(true)]
		internal sealed class TableNameFormatDark : ClassificationFormatDefinition
		{
			public TableNameFormatDark()
			{
				ForegroundColor = Colors.SkyBlue;
			}
		}
		#endregion

		#region TableField
		[Export]
		[Name("DK.TableField.Light")]
		internal static ClassificationTypeDefinition TableField = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.TableField.Light")]
		[Name("DK.TableField.Light")]
		[UserVisible(true)]
		internal sealed class TableFieldFormat : ClassificationFormatDefinition
		{
			public TableFieldFormat()
			{
				ForegroundColor = Colors.SteelBlue;
			}
		}

		[Export]
		[Name("DK.TableField.Dark")]
		internal static ClassificationTypeDefinition TableFieldDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.TableField.Dark")]
		[Name("DK.TableField.Dark")]
		[UserVisible(true)]
		internal sealed class TableFieldFormatDark : ClassificationFormatDefinition
		{
			public TableFieldFormatDark()
			{
				ForegroundColor = Colors.SkyBlue;
			}
		}
		#endregion

		#region Constant
		[Export]
		[Name("DK.Constant.Light")]
		internal static ClassificationTypeDefinition Constant = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Constant.Light")]
		[Name("DK.Constant.Light")]
		[UserVisible(true)]
		internal sealed class ConstantFormat : ClassificationFormatDefinition
		{
			public ConstantFormat()
			{
				ForegroundColor = Colors.DarkBlue;
			}
		}

		[Export]
		[Name("DK.Constant.Dark")]
		internal static ClassificationTypeDefinition ConstantDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Constant.Dark")]
		[Name("DK.Constant.Dark")]
		[UserVisible(true)]
		internal sealed class ConstantFormatDark : ClassificationFormatDefinition
		{
			public ConstantFormatDark()
			{
				ForegroundColor = Colors.RoyalBlue;
			}
		}
		#endregion

		#region DataType
		[Export]
		[Name("DK.DataType.Light")]
		internal static ClassificationTypeDefinition DataType = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.DataType.Light")]
		[Name("DK.DataType.Light")]
		[UserVisible(true)]
		internal sealed class DataTypeFormat : ClassificationFormatDefinition
		{
			public DataTypeFormat()
			{
				ForegroundColor = Colors.Teal;
			}
		}

		[Export]
		[Name("DK.DataType.Dark")]
		internal static ClassificationTypeDefinition DataTypeDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.DataType.Dark")]
		[Name("DK.DataType.Dark")]
		[UserVisible(true)]
		internal sealed class DataTypeFormatDark : ClassificationFormatDefinition
		{
			public DataTypeFormatDark()
			{
				ForegroundColor = Colors.LightSeaGreen;
			}
		}
		#endregion

		#region Function
		[Export]
		[Name("DK.Function.Light")]
		internal static ClassificationTypeDefinition Function = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Function.Light")]
		[Name("DK.Function.Light")]
		[UserVisible(true)]
		internal sealed class FunctionFormat : ClassificationFormatDefinition
		{
			public FunctionFormat()
			{
				ForegroundColor = Colors.DarkMagenta;
			}
		}

		[Export]
		[Name("DK.Function.Dark")]
		internal static ClassificationTypeDefinition FunctionDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Function.Dark")]
		[Name("DK.Function.Dark")]
		[UserVisible(true)]
		internal sealed class FunctionFormatDark : ClassificationFormatDefinition
		{
			public FunctionFormatDark()
			{
				ForegroundColor = Colors.MediumOrchid;
			}
		}
		#endregion

		#region Delimiter
		[Export]
		[Name("DK.Delimiter.Light")]
		internal static ClassificationTypeDefinition Delimiter = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Delimiter.Light")]
		[Name("DK.Delimiter.Light")]
		[UserVisible(true)]
		internal sealed class DelimiterFormat : ClassificationFormatDefinition
		{
			public DelimiterFormat()
			{
				ForegroundColor = Colors.DimGray;
			}
		}

		[Export]
		[Name("DK.Delimiter.Dark")]
		internal static ClassificationTypeDefinition DelimiterDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Delimiter.Dark")]
		[Name("DK.Delimiter.Dark")]
		[UserVisible(true)]
		internal sealed class DelimiterFormatDark : ClassificationFormatDefinition
		{
			public DelimiterFormatDark()
			{
				ForegroundColor = Colors.LightGray;
			}
		}
		#endregion

		#region Operator
		[Export]
		[Name("DK.Operator.Light")]
		internal static ClassificationTypeDefinition Operator = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Operator.Light")]
		[Name("DK.Operator.Light")]
		[UserVisible(true)]
		internal sealed class OperatorFormat : ClassificationFormatDefinition
		{
			public OperatorFormat()
			{
				ForegroundColor = Colors.DimGray;
			}
		}

		[Export]
		[Name("DK.Operator.Dark")]
		internal static ClassificationTypeDefinition OperatorDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Operator.Dark")]
		[Name("DK.Operator.Dark")]
		[UserVisible(true)]
		internal sealed class OperatorFormatDark : ClassificationFormatDefinition
		{
			public OperatorFormatDark()
			{
				ForegroundColor = Colors.LightGray;
			}
		}
		#endregion

		#region Variable
		[Export]
		[Name("DK.Variable.Light")]
		internal static ClassificationTypeDefinition Variable = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Variable.Light")]
		[Name("DK.Variable.Light")]
		[UserVisible(true)]
		internal sealed class VariableFormat : ClassificationFormatDefinition
		{
			public VariableFormat()
			{
				ForegroundColor = Colors.DarkSlateGray;
			}
		}

		[Export]
		[Name("DK.Variable.Dark")]
		internal static ClassificationTypeDefinition VariableDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Variable.Dark")]
		[Name("DK.Variable.Dark")]
		[UserVisible(true)]
		internal sealed class VariableFormatDark : ClassificationFormatDefinition
		{
			public VariableFormatDark()
			{
				ForegroundColor = Colors.LightSlateGray;
			}
		}
		#endregion

		#region Interface
		[Export]
		[Name("DK.Interface.Light")]
		internal static ClassificationTypeDefinition Interface = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Interface.Light")]
		[Name("DK.Interface.Light")]
		[UserVisible(true)]
		internal sealed class InterfaceFormat : ClassificationFormatDefinition
		{
			public InterfaceFormat()
			{
				ForegroundColor = Colors.DarkOrange;
			}
		}

		[Export]
		[Name("DK.Interface.Dark")]
		internal static ClassificationTypeDefinition InterfaceDark = null;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "DK.Interface.Dark")]
		[Name("DK.Interface.Dark")]
		[UserVisible(true)]
		internal sealed class InterfaceFormatDark : ClassificationFormatDefinition
		{
			public InterfaceFormatDark()
			{
				ForegroundColor = Colors.DarkOrange;
			}
		}
		#endregion
	}
}
