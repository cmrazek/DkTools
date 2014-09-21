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
		private DataType _dataType;
		private string _signature;
		private int _bodyStartPos;
		private int _argsEndPos;
		private FunctionPrivacy _privacy;
		private bool _extern;
		private string _className;
		private Span _entireSpan;

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
		public FunctionDefinition(string className, string funcName, string fileName, int nameStartPos, DataType dataType, string signature, int argsEndPos, int bodyStartPos, Span entireSpan, FunctionPrivacy privacy, bool isExtern)
			: base(funcName, fileName, nameStartPos, true)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentNullException("signature");
#endif
			_dataType = dataType != null ? dataType : DataType.Int;
			_signature = signature;
			_argsEndPos = argsEndPos;
			_bodyStartPos = bodyStartPos;
			_privacy = privacy;
			_extern = isExtern;
			_className = className;
			_entireSpan = entireSpan;
		}

		public FunctionDefinition CloneAsExtern()
		{
			return new FunctionDefinition(_className, Name, SourceFileName, SourceStartPos, _dataType, _signature, _argsEndPos, _bodyStartPos, _entireSpan, _privacy, true);
		}

		public DataType DataType
		{
			get { return _dataType; }
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

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Function; }
		}

		public override string QuickInfoText
		{
			get { return _signature; }
		}

		public int BodyStartPosition
		{
			get { return _bodyStartPos; }
		}

		public int ArgsEndPosition
		{
			get { return _argsEndPos; }
		}

		public FunctionPrivacy Privacy
		{
			get { return _privacy; }
		}

		public bool Extern
		{
			get { return _extern; }
		}

		public override void DumpTreeAttribs(System.Xml.XmlWriter xml)
		{
			base.DumpTreeAttribs(xml);

			xml.WriteAttributeString("signature", _signature);
			xml.WriteAttributeString("bodyStartPos", _bodyStartPos.ToString());
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement("FunctionDataType");
			_dataType.DumpTree(xml);
			xml.WriteEndElement();

			base.DumpTreeInner(xml);
		}

		public string ClassName
		{
			get { return _className; }
		}

		public Span EntireSpan
		{
			get { return _entireSpan; }
		}
	}
}
