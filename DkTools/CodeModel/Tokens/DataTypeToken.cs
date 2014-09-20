using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class DataTypeToken : GroupToken, IDataTypeToken
	{
		private DataType _dataType;

		/// <summary>
		/// Creates a data type token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="token">(required) Token that contains the data type text.</param>
		/// <param name="dataType">(required) Assigned data type</param>
		/// <param name="def">(optional) Definition point of the data type.  Can be null for built-in data types.</param>
		public DataTypeToken(GroupToken parent, Scope scope, IdentifierToken token, DataType dataType, Definition def)
			: base(parent, scope, new Token[] { token })
		{
			_dataType = dataType;

			if (def != null) token.SourceDefinition = def;

			ClassifierType = Classifier.ProbeClassifierType.DataType;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			base.DumpTreeInner(xml);
			_dataType.DumpTree(xml);
		}
	}
}
