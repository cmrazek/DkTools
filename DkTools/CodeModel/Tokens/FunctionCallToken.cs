﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	/// <summary>
	/// A call to a previously defined function.
	/// </summary>
	internal sealed class FunctionCallToken : GroupToken
	{
		private IdentifierToken _nameToken;
		private BracketsToken _argsToken;
		private DataType _dataType;	// Can be null

		/// <summary>
		/// Creates a function call token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="nameToken">(required) Function name</param>
		/// <param name="argsToken">(required) Function args</param>
		/// <param name="def">(optional) Existing function definition</param>
		public FunctionCallToken(GroupToken parent, Scope scope, IdentifierToken nameToken, BracketsToken argsToken, FunctionDefinition def)
			: base(parent, scope, new Token[] { nameToken, argsToken })
		{
#if DEBUG
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (argsToken == null) throw new ArgumentNullException("argsToken");
#endif

			_nameToken = nameToken;
			_nameToken.SourceDefinition = def;

			_argsToken = argsToken;

			// If this function is already defined, then save the data type.
			var funcDecl = this.GetDefinitions<FunctionDefinition>(_nameToken.Text).FirstOrDefault();
			if (funcDecl != null) _dataType = funcDecl.DataType;
		}

		public IdentifierToken NameToken
		{
			get { return _nameToken; }
		}

		public BracketsToken ArgsToken
		{
			get { return _argsToken; }
		}

		public override DataType ValueDataType
		{
			get { return _dataType; }
		}
	}
}