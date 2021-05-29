using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;
using System.Text;

namespace DK.Definitions
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

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Variable; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.TableField; }
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

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoAttribute("Name", Name),
			new QuickInfoAttribute("Data Type", _dataType.GetClassifiedString(shortVersion: true)),
			new QuickInfoAttribute("Interface", _intTypeDef.Name)
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
