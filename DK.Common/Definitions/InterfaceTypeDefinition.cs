using DK.Code;
using DK.Schema;
using DK.Syntax;
using System;

namespace DK.Definitions
{
	public class InterfaceTypeDefinition : Definition
	{
		private Interface _intType;
		private string _devDesc;

		public InterfaceTypeDefinition(Interface intType, FilePosition filePos)
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

		public override ServerContext ServerContext => ServerContext.Neutral;

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Interface; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.Interface; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (string.IsNullOrEmpty(_devDesc)) return string.Concat("interface ", Name);
				return string.Concat("interface ", Name, "\r\n", _devDesc);
			}
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoClassifiedString(_intType.DataType.GetClassifiedString(shortVersion: true)),
			string.IsNullOrWhiteSpace(_devDesc) ? null : new QuickInfoDescription(_devDesc)
		);

		public Interface DictInterfaceType
		{
			get { return _intType; }
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

		public override bool ArgumentsRequired
		{
			get { return false; }
		}
	}
}
