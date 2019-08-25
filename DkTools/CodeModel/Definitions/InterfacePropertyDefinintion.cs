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
			: base(name, FilePosition.Empty, GetExternalRefId(intTypeDef.Name, name))
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

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.Variable; }
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
				sb.Append("Name: ");
				sb.AppendLine(Name);
				if (_dataType != null)
				{
					sb.Append("Data Type: ");
					sb.Append(_dataType);
				}
				sb.Append("Interface: ");
				sb.Append(_intTypeDef.Name);

				return sb.ToString();
			}
		}

		public override object QuickInfoElements => QuickInfoStack(
			QuickInfoAttributeString("Name", Name),
			QuickInfoAttributeElement("Data Type", _dataType.QuickInfoElements),
			QuickInfoAttributeString("Interface", _intTypeDef.Name)
		);

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

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override bool CanRead
		{
			get
			{
				return _dataType != null;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return _dataType != null;
			}
		}

		public override bool RequiresParent(string curClassName)
		{
			return true;
		}
	}
}
