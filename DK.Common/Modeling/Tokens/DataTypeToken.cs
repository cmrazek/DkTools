using DK.Definitions;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class DataTypeToken : GroupToken, IDataTypeToken
	{
		private DataType _dataType;

		/// <summary>
		/// Creates a data type token.
		/// </summary>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="token">(required) Token that contains the data type text.</param>
		/// <param name="dataType">(required) Assigned data type</param>
		/// <param name="def">(optional) Definition point of the data type.  Can be null for built-in data types.</param>
		internal DataTypeToken(Scope scope, IdentifierToken token, DataType dataType, Definition def)
			: base(scope)
		{
			AddToken(token);
			if (def != null) token.SourceDefinition = def;

			_dataType = dataType;

			ClassifierType = ProbeClassifierType.DataType;
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

		public override bool IsDataTypeDeclaration
		{
			get
			{
				return true;
			}
		}
	}
}
