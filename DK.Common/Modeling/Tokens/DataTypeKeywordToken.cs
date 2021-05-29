using DK.Code;
using DK.Definitions;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class DataTypeKeywordToken : WordToken
	{
		private DataType _inferredDataType;

		internal DataTypeKeywordToken(Scope scope, CodeSpan span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = ProbeClassifierType.DataType;
			if (def != null) SourceDefinition = def;
		}

		public override bool IsDataTypeDeclaration
		{
			get
			{
				return _inferredDataType != null;
			}
		}

		public DataType InferredDataType
		{
			get { return _inferredDataType; }
			set { _inferredDataType = value; }
		}

		public override DataType ValueDataType
		{
			get
			{
				return _inferredDataType;
			}
		}
	}
}
