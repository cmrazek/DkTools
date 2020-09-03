﻿using DkTools.QuickInfo;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class EnumOptionDefinition : Definition
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

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.Constant; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoTextStr => Name;

		public override QuickInfoLayout QuickInfo => new QuickInfoText(Classifier.ProbeClassifierType.Constant, Name);

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
