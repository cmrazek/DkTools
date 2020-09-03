using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DkTools.CodeModel.Tokens;
using DkTools.QuickInfo;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;

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

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.Function; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Function; }
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
