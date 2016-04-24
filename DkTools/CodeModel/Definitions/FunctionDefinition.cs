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
		private string _devDesc;

		/// <summary>
		/// Creates a function definition object.
		/// </summary>
		/// <param name="scope">Current scope</param>
		/// <param name="name">Function name</param>
		/// <param name="sourceToken">Function name token</param>
		/// <param name="dataType">Function's return type</param>
		/// <param name="signature">Signature text</param>
		/// <param name="argsEndPos">Ending position of the argument brackets</param>
		/// <param name="bodyStartPos">Position of the function's body braces (if does not match, then will be ignored)</param>
		public FunctionDefinition(FunctionSignature sig, FilePosition filePos,
			int argsStartPos, int argsEndPos, int bodyStartPos, Span entireSpan, string devDesc)
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
			_devDesc = devDesc;
		}

		/// <summary>
		/// Creates a built-in function definition.
		/// </summary>
		/// <param name="funcName">Function name</param>
		/// <param name="dataType">Data type</param>
		/// <param name="signature">Signature</param>
		/// <param name="devDesc">Developer description</param>
		public FunctionDefinition(FunctionSignature sig, string devDesc)
			: base(sig.FunctionName, FilePosition.Empty, string.Concat("func:", sig.FunctionName))
		{
#if DEBUG
			if (sig == null) throw new ArgumentNullException("sig");
#endif
			_sig = sig;
			_argsStartPos = _argsEndPos = _bodyStartPos = 0;
			_entireSpan = Span.Empty;
			_devDesc = devDesc;
		}

		public FunctionDefinition CloneAsExtern()
		{
			return new FunctionDefinition(_sig.Clone(), FilePosition, _argsStartPos, _argsEndPos, _bodyStartPos, _entireSpan, _devDesc);
		}

		public override DataType DataType
		{
			get { return _sig.ReturnDataType; }
		}

		public override IEnumerable<ArgumentDescriptor> Arguments
		{
			get { return _sig.Arguments; }
		}

		public FunctionSignature Signature
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
				if (string.IsNullOrEmpty(_devDesc)) return _sig.PrettySignature;
				return string.Concat(_sig.PrettySignature, "\r\n\r\n", _devDesc);
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfDivs(
					WpfMainLine(_sig.PrettySignature),
					string.IsNullOrEmpty(_devDesc) ? null : WpfInfoLine(_devDesc));
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

		public string DevDescription
		{
			get { return _devDesc; }
		}

		public override string PickText
		{
			get { return _sig.PrettySignature; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override bool AllowsChild
		{
			get { return false; }
		}

		public override Definition GetChildDefinition(string name)
		{
			throw new NotSupportedException();
		}

		public override bool RequiresArguments
		{
			get { return true; }
		}

		public override bool AllowsFunctionBody
		{
			get
			{
				return true;
			}
		}
	}
}
