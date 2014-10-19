﻿using System;
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
		private int _argsStartPos;
		private int _argsEndPos;
		private FunctionPrivacy _privacy;
		private bool _extern;
		private string _className;
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
		public FunctionDefinition(string className, string funcName, string fileName, int nameStartPos, DataType dataType, string signature,
			int argsStartPos, int argsEndPos, int bodyStartPos, Span entireSpan, FunctionPrivacy privacy, bool isExtern, string devDesc)
			: base(funcName, fileName, nameStartPos)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentNullException("signature");
#endif
			_dataType = dataType != null ? dataType : DataType.Int;
			_signature = signature;
			_argsStartPos = argsStartPos;
			_argsEndPos = argsEndPos;
			_bodyStartPos = bodyStartPos;
			_privacy = privacy;
			_extern = isExtern;
			_className = className;
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
		public FunctionDefinition(string funcName, DataType dataType, string signature, string devDesc)
			: base(funcName, null, 0)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentNullException("signature");
#endif
			_dataType = dataType != null ? dataType : DataType.Int;
			_signature = signature;
			_argsStartPos = _argsEndPos = _bodyStartPos = 0;
			_privacy = FunctionPrivacy.Public;
			_extern = true;
			_className = null;
			_entireSpan = Span.Empty;
			_devDesc = devDesc;
		}

		public FunctionDefinition CloneAsExtern()
		{
			return new FunctionDefinition(_className, Name, SourceFileName, SourceStartPos, _dataType, _signature, _argsStartPos, _argsEndPos, _bodyStartPos, _entireSpan, _privacy, true, _devDesc);
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

		public override string QuickInfoTextStr
		{
			get
			{
				if (string.IsNullOrEmpty(_devDesc)) return _signature;
				return string.Concat(_signature, "\r\n\r\n", _devDesc);
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfDivs(
					WpfMainLine(_signature),
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

		public string DevDescription
		{
			get { return _devDesc; }
		}

		public override string PickText
		{
			get { return Signature; }
		}
	}
}
