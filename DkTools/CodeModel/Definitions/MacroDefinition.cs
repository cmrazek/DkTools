using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
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

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Function; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Function; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				return string.Concat(_signature, "\r\n", _body).Trim();
			}
		}

		public override object QuickInfoElements => QuickInfoStack(
			_signature.QuickInfoElements,
			QuickInfoDescription(_body.Trim())
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
