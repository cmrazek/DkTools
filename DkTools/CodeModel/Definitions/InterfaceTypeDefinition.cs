using DkTools.QuickInfo;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.Interface; }
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

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoClassifiedString(_intType.DataType.GetClassifiedString(shortVersion: true)),
			string.IsNullOrWhiteSpace(_devDesc) ? null : new QuickInfoDescription(_devDesc)
		);

		public DkDict.Interface DictInterfaceType
		{
			get { return _intType; }
		}

		public IEnumerable<InterfaceMethodDefinition> GetMethods(string name)
		{
			foreach (var meth in _intType.GetMethods(name)) yield return meth;
		}

		public IEnumerable<InterfacePropertyDefinition> GetProperties(string name)
		{
			foreach (var prop in _intType.GetProperties(name)) yield return prop;
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

		public override IEnumerable<Definition> GetChildDefinitions(string name)
		{
			foreach (var def in _intType.GetMethods(name)) yield return def;
			foreach (var def in _intType.GetProperties(name)) yield return def;
		}

		public override IEnumerable<Definition> ChildDefinitions
		{
			get
			{
				foreach (var def in _intType.MethodDefinitions) yield return def;
				foreach (var def in _intType.PropertyDefinitions) yield return def;
			}
		}

		public override bool ArgumentsRequired
		{
			get { return false; }
		}
	}
}
