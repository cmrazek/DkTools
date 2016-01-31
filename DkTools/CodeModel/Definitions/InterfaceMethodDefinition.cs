using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal sealed class InterfaceMethodDefinition : Definition
	{
		private InterfaceTypeDefinition _intType;
		private string _signature;
		private DataType _returnDataType;

		public InterfaceMethodDefinition(InterfaceTypeDefinition intType, string name, string signature, DataType returnDataType)
			: base(name, null, -1, string.Concat("interface:", intType.Name, ".method:", name))
		{
#if DEBUG
			if (intType == null) throw new ArgumentNullException("intType");
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
			if (string.IsNullOrEmpty(signature)) throw new ArgumentNullException("signature");
			if (returnDataType == null) throw new ArgumentNullException("returnDataType");
#endif
			_intType = intType;
			_signature = signature;
			_returnDataType = returnDataType;
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
				return _signature;	// TODO: include interface name
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfMainLine(_signature);	// TODO: include interface name
			}
		}

		public string Signature
		{
			get { return _signature; }
		}

		public string DevDescription
		{
			get { return null; }
		}

		public DataType ReturnDataType
		{
			get { return _returnDataType; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
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
	}
}
