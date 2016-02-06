using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal sealed class InterfacePropertyDefinition : Definition
	{
		private InterfaceTypeDefinition _intTypeDef;
		private DataType _dataType;

		public InterfacePropertyDefinition(InterfaceTypeDefinition intTypeDef, string name, DataType dataType)
			: base(name, null, -1, GetExternalRefId(intTypeDef.Name, name))
		{
#if DEBUG
			if (intTypeDef == null) throw new ArgumentNullException("intTypeDef");
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_intTypeDef = intTypeDef;
			_dataType = dataType;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Variable; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableField; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				var sb = new StringBuilder();
				sb.Append(_dataType.Name);
				sb.Append(' ');
				sb.Append(Name);
				sb.AppendLine();

				sb.Append("Interface: ");
				sb.Append(_intTypeDef.Name);

				return sb.ToString();
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfDivs
				(
					WpfAttribute("Name", Name),
					WpfAttribute("Data Type", _dataType.Name),
					WpfAttribute("Interface", _intTypeDef.Name)
				);
			}
		}

		public override DataType DataType
		{
			get { return _dataType; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string intfName, string propName)
		{
			return string.Concat("interface:", intfName, ".prop:", propName);
		}

		public override bool AllowsChild
		{
			get { return false; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override Definition GetChildDefinition(string name)
		{
			throw new NotSupportedException();
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}
	}
}
