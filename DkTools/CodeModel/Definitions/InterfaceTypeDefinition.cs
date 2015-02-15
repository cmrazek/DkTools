using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal sealed class InterfaceTypeDefinition : Definition
	{
		private Dict.InterfaceType _intType;
		private string _devDesc;

		public InterfaceTypeDefinition(Dict.InterfaceType intType)
			: base(intType.Name, null, -1, GetExternalRefId(intType.Name))
		{
#if DEBUG
			if (intType == null) throw new ArgumentNullException("intType");
#endif
			_intType = intType;
			_devDesc = _intType.DevDescription;
		}

		public InterfaceTypeDefinition(string name)
			: base(name, null, -1, GetExternalRefId(name))
		{
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Interface; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Interface; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (string.IsNullOrEmpty(_devDesc)) return string.Concat("interface ", Name);
				return string.Concat("interface ", Name, "\r\n", _devDesc);
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				if (string.IsNullOrEmpty(_devDesc))
				{
					return WpfMainLine(string.Concat("interface ", Name));
				}
				else
				{
					return WpfDivs
					(
						WpfMainLine(string.Concat("interface ", Name)),
						WpfInfoLine(_devDesc)
					);
				}
			}
		}

		public Dict.InterfaceType DictInterfaceType
		{
			get { return _intType; }
		}

		public InterfaceMethodDefinition GetMethod(string name)
		{
			return _intType.GetMethod(name);
		}

		public InterfacePropertyDefinition GetProperty(string name)
		{
			return _intType.GetProperty(name);
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string name)
		{
			return string.Concat("interface:", name);
		}
	}
}
