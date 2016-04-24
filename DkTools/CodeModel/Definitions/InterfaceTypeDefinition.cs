using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal sealed class InterfaceTypeDefinition : Definition
	{
		private DkDict.Interface _intType;
		private string _devDesc;

		public InterfaceTypeDefinition(DkDict.Interface intType, FilePosition filePos)
			: base(intType.Name, filePos, GetExternalRefId(intType.Name))
		{
#if DEBUG
			if (intType == null) throw new ArgumentNullException("intType");
#endif
			_intType = intType;
			_devDesc = _intType.Description;
		}

		public InterfaceTypeDefinition(string name, FilePosition filePos)
			: base(name, filePos, GetExternalRefId(name))
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

		public DkDict.Interface DictInterfaceType
		{
			get { return _intType; }
		}

		public InterfaceMethodDefinition GetMethod(string name)
		{
			// TODO: Add support for methods
			//return _intType.GetMethod(name);
			return null;
		}

		public InterfacePropertyDefinition GetProperty(string name)
		{
			// TODO: Add support for properties
			//return _intType.GetProperty(name);
			return null;
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string name)
		{
			return string.Concat("interface:", name);
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override bool AllowsChild
		{
			get { return true; }
		}

		public override Definition GetChildDefinition(string name)
		{
			Definition def = _intType.GetMethod(name);
			if (def != null) return def;

			def = _intType.GetProperty(name);
			if (def != null) return def;

			return null;
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}
	}
}
