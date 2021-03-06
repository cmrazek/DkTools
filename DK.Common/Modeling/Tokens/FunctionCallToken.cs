﻿using DK.Definitions;
using System;

namespace DK.Modeling.Tokens
{
	/// <summary>
	/// A call to a previously defined function.
	/// </summary>
	public sealed class FunctionCallToken : GroupToken
	{
		private IdentifierToken _nameToken;
		private ClassToken _classToken;
		private BracketsToken _argsToken;
		private DataType _dataType; // Can be null

		/// <summary>
		/// Creates a function call token.
		/// </summary>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="classToken">(optional) Class name token</param>
		/// <param name="dotToken">(optional) Dot delimiter between class and function name</param>
		/// <param name="nameToken">(required) Function name</param>
		/// <param name="argsToken">(required) Function args</param>
		/// <param name="def">(optional) Existing function definition</param>
		internal FunctionCallToken(Scope scope, ClassToken classToken, DotToken dotToken, IdentifierToken nameToken, BracketsToken argsToken, FunctionDefinition def)
			: base(scope)
		{
#if DEBUG
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (argsToken == null) throw new ArgumentNullException("argsToken");
#endif
			AddToken(_classToken = classToken);
			AddToken(dotToken);
			AddToken(_nameToken = nameToken);
			_nameToken.SourceDefinition = def;
			AddToken(_argsToken = argsToken);

			_dataType = def.DataType;
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
