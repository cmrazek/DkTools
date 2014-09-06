﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class MacroDefinition : Definition
	{
		private string _signature;
		private string _body;

		public MacroDefinition(Scope scope, string name, Token sourceToken, string signature, string body)
			: base(scope, name, sourceToken, true)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentNullException("signature");
#endif
			_signature = signature;
			_body = body;
		}

		public string Signature
		{
			get { return _signature; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Function; }
		}

		public override string CompletionDescription
		{
			get { return _signature; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Function; }
		}

		public override string QuickInfoText
		{
			get
			{
				return string.Concat(_signature, " ", _body).Trim();
			}
		}
	}
}
