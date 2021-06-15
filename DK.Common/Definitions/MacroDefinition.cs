using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;

namespace DK.Definitions
{
	internal class MacroDefinition : Definition
	{
		private FunctionSignature _signature;
		private string _body;

		public MacroDefinition(string name, FilePosition filePos, FunctionSignature signature, string body)
			: base(name, filePos, null)
		{
#if DEBUG
			if (signature == null) throw new ArgumentNullException("signature");
#endif
			_signature = signature;
			_body = body;
		}

		public override ServerContext ServerContext => ServerContext.Neutral;

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Function; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.Function; }
		}

		public override string QuickInfoTextStr => string.Concat(_signature, "\r\n", _body).Trim();

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoClassifiedString(_signature.ClassifiedString),
			new QuickInfoDescription(_body.Trim())
		);

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool ArgumentsRequired
		{
			get { return true; }
		}

		public override FunctionSignature ArgumentsSignature
		{
			get
			{
				return _signature;
			}
		}
	}
}
