using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class DataTypeKeywordToken : WordToken
	{
		private DataType _inferredDataType;

		public DataTypeKeywordToken(Scope scope, Span span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.DataType;
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
