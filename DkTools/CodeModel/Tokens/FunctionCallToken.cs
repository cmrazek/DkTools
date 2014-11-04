using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	/// <summary>
	/// A call to a previously defined function.
	/// </summary>
	internal sealed class FunctionCallToken : GroupToken
	{
		private IdentifierToken _nameToken;
		private ClassToken _classToken;
		private BracketsToken _argsToken;
		private DataType _dataType;	// Can be null

		/// <summary>
		/// Creates a function call token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="classToken">(optional) Class name token</param>
		/// <param name="dotToken">(optional) Dot delimiter between class and function name</param>
		/// <param name="nameToken">(required) Function name</param>
		/// <param name="argsToken">(required) Function args</param>
		/// <param name="def">(optional) Existing function definition</param>
		public FunctionCallToken(GroupToken parent, Scope scope, ClassToken classToken, DotToken dotToken, IdentifierToken nameToken, BracketsToken argsToken, FunctionDefinition def)
			: base(parent, scope, Token.SafeTokenList(classToken, dotToken, nameToken, argsToken))
		{
#if DEBUG
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (argsToken == null) throw new ArgumentNullException("argsToken");
#endif
			_nameToken = nameToken;
			_nameToken.SourceDefinition = def;
			_classToken = classToken;
			_argsToken = argsToken;

			_dataType = def.DataType;

			// TODO: remove?
			//// If this function is already defined, then save the data type.
			//var funcDecl = DefinitionProvider.GetGlobalFromAnywhere<FunctionDefinition>(_nameToken.Text).FirstOrDefault();
			//if (funcDecl != null) _dataType = funcDecl.DataType;
		}

		public IdentifierToken NameToken
		{
			get { return _nameToken; }
		}

		public ClassToken ClassToken
		{
			get { return _classToken; }
		}

		public BracketsToken ArgsToken
		{
			get { return _argsToken; }
		}

		public override DataType ValueDataType
		{
			get { return _dataType; }
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			base.DumpTreeInner(xml);

			if (_dataType != null)
			{
				xml.WriteStartElement("FunctionCallDataType");
				_dataType.DumpTree(xml);
				xml.WriteEndElement();
			}
		}
	}
}
