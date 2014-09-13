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
		private Position _bodyStartPos;
		private Position _argsEndPos;
		private FunctionPrivacy _privacy;
		private bool _extern;

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
		public FunctionDefinition(Scope scope, string name, Token sourceToken, DataType dataType, string signature, Position argsEndPos, Position bodyStartPos, FunctionPrivacy privacy, bool isExtern)
			: base(scope, name, sourceToken, true)
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
		}

		public FunctionDefinition CloneAsExtern()
		{
			return new FunctionDefinition(Scope, Name, SourceToken, _dataType, _signature, _argsEndPos, _bodyStartPos, _privacy, true);
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
			get { return _signature; }
		}

		public Position BodyStartPosition
		{
			get { return _bodyStartPos; }
		}

		public Position ArgsEndPosition
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

		public override bool MoveFromPreprocessorToVisibleModel(CodeFile visibleFile, CodeSource visibleSource)
		{
			if (SourceFile != null)
			{
				var localPos = SourceFile.CodeSource.GetFilePosition(_bodyStartPos.Offset);
				if (localPos.PrimaryFile) _bodyStartPos = localPos.Position;
				else _bodyStartPos = Position.Start;

				localPos = SourceFile.CodeSource.GetFilePosition(_argsEndPos.Offset);
				if (localPos.PrimaryFile) _argsEndPos = localPos.Position;
				else _argsEndPos = Position.Start;
			}

			return base.MoveFromPreprocessorToVisibleModel(visibleFile, visibleSource);
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
	}
}
