using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;

namespace DK.Definitions
{
	public class EnumOptionDefinition : Definition
	{
		private DataType _dataType;

		public EnumOptionDefinition(string text, DataType dataType)
			: base(text, FilePosition.Empty, null)
		{
			_dataType = dataType;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Constant; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.Constant; }
		}

		public override string QuickInfoTextStr => Name;
		public override QuickInfoLayout QuickInfo => new QuickInfoText(ProbeClassifierType.Constant, Name);
		public override ServerContext ServerContext => ServerContext.Neutral;

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override DataType DataType
		{
			get
			{
				return _dataType;
			}
		}

		public void SetEnumDataType(DataType dataType)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}
	}
}
