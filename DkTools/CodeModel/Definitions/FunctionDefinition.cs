using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class FunctionDefinition : Definition
	{
		private DataType _dataType;
		private string _signature;
		private Position _bodyStartPos;

		/// <summary>
		/// Creates a function definition object.
		/// </summary>
		/// <param name="scope">Current scope</param>
		/// <param name="name">Function name</param>
		/// <param name="sourceToken">Function name token</param>
		/// <param name="dataType">Function's return type</param>
		/// <param name="signature">Signature text</param>
		/// <param name="bodyStart">Position of the function's body braces (if does not match, then will be ignored)</param>
		public FunctionDefinition(Scope scope, string name, Token sourceToken, DataType dataType, string signature, Position bodyStart)
			: base(scope, name, sourceToken, true)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentNullException("signature");
#endif
			_dataType = dataType != null ? dataType : DataType.Int;
			_signature = signature;
			_bodyStartPos = bodyStart;
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

		public override bool MoveFromPreprocessorToVisibleModel(CodeFile visibleFile, CodeSource visibleSource)
		{
			if (SourceFile != null)
			{
				var localPos = SourceFile.CodeSource.GetFilePosition(_bodyStartPos.Offset);
				if (localPos.PrimaryFile) _bodyStartPos = localPos.Position;
				else _bodyStartPos = Position.Start;
			}

			return base.MoveFromPreprocessorToVisibleModel(visibleFile, visibleSource);
		}

		public override void DumpTreeAttribs(System.Xml.XmlWriter xml)
		{
			base.DumpTreeAttribs(xml);

			xml.WriteAttributeString("dataType", _dataType.ToString());
			xml.WriteAttributeString("signature", _signature);
			xml.WriteAttributeString("bodyStartPos", _bodyStartPos.ToString());
		}
	}
}
