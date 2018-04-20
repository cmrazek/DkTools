using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class FunctionDefinition : Definition
	{
		private FunctionSignature _sig;
		private int _bodyStartPos;
		private int _argsStartPos;
		private int _argsEndPos;
		private Span _entireSpan;

		public FunctionDefinition(FunctionSignature sig, FilePosition filePos,
			int argsStartPos, int argsEndPos, int bodyStartPos, Span entireSpan)
			: base(sig.FunctionName, filePos, !string.IsNullOrEmpty(sig.ClassName) ? string.Concat("class:", sig.ClassName, ".func:", sig.FunctionName) : string.Concat("func:", sig.FunctionName))
		{
#if DEBUG
			if (sig == null) throw new ArgumentNullException("sig");
#endif
			_sig = sig;
			_argsStartPos = argsStartPos;
			_argsEndPos = argsEndPos;
			_bodyStartPos = bodyStartPos;
			_entireSpan = entireSpan;
		}

		public FunctionDefinition(FunctionSignature sig)
			: base(sig.FunctionName, FilePosition.Empty, string.Concat("func:", sig.FunctionName))
		{
#if DEBUG
			if (sig == null) throw new ArgumentNullException("sig");
#endif
			_sig = sig;
			_argsStartPos = _argsEndPos = _bodyStartPos = 0;
			_entireSpan = Span.Empty;
		}

		public FunctionDefinition CloneAsExtern()
		{
			return new FunctionDefinition(_sig.Clone(), FilePosition, _argsStartPos, _argsEndPos, _bodyStartPos, _entireSpan);
		}

		public override DataType DataType
		{
			get { return _sig.ReturnDataType; }
		}

		public override IEnumerable<ArgumentDescriptor> Arguments
		{
			get { return _sig.Arguments; }
		}

		public override FunctionSignature Signature
		{
			get { return _sig; }
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
				if (string.IsNullOrEmpty(_sig.Description)) return _sig.PrettySignature;
				return string.Concat(_sig.PrettySignature, "\r\n\r\n", _sig.Description);
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfDivs(
					WpfMainLine(_sig.PrettySignature),
					string.IsNullOrEmpty(_sig.Description) ? null : WpfInfoLine(_sig.Description));
			}
		}

		public int BodyStartPosition
		{
			get { return _bodyStartPos; }
		}

		public int ArgsStartPosition
		{
			get { return _argsStartPos; }
		}

		public int ArgsEndPosition
		{
			get { return _argsEndPos; }
		}

		public FunctionPrivacy Privacy
		{
			get { return _sig.Privacy; }
		}

		public bool Extern
		{
			get { return _sig.Extern; }
		}

		public override void DumpTreeAttribs(System.Xml.XmlWriter xml)
		{
			base.DumpTreeAttribs(xml);

			xml.WriteAttributeString("signature", _sig.PrettySignature);
			xml.WriteAttributeString("bodyStartPos", _bodyStartPos.ToString());
		}

		public string ClassName
		{
			get { return _sig.ClassName; }
		}

		public Span EntireSpan
		{
			get { return _entireSpan; }
		}

		public override string PickText
		{
			get { return _sig.PrettySignature; }
		}

		public override bool ArgumentsRequired
		{
			get { return true; }
		}

		public override FunctionSignature ArgumentsSignature
		{
			get
			{
				return _sig;
			}
		}

		public override bool AllowsFunctionBody
		{
			get
			{
				return true;
			}
		}

		public override bool CanRead
		{
			get
			{
				return _sig.ReturnDataType != null && !_sig.ReturnDataType.IsVoid;
			}
		}

		public override bool RequiresParent
		{
			get
			{
				return !string.IsNullOrEmpty(_sig.ClassName);
			}
		}
	}
}
