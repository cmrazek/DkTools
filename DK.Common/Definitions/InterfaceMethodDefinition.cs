using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;
using System.Collections.Generic;

namespace DK.Definitions
{
	public sealed class InterfaceMethodDefinition : Definition
	{
		private InterfaceTypeDefinition _intType;
		private FunctionSignature _sig;
		private DataType _returnDataType;

		public InterfaceMethodDefinition(InterfaceTypeDefinition intType, string name, FunctionSignature sig, DataType returnDataType)
			: base(name, FilePosition.Empty, string.Concat("interface:", intType.Name, ".method:", name))
		{
#if DEBUG
			if (intType == null) throw new ArgumentNullException("intType");
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
			if (sig == null) throw new ArgumentNullException("signature");
			if (returnDataType == null) throw new ArgumentNullException("returnDataType");
#endif
			_intType = intType;
			_sig = sig;
			_returnDataType = returnDataType;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Function; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.Function; }
		}

		public override string QuickInfoTextStr => _sig.PrettySignature;

		public override QuickInfoLayout QuickInfo => new QuickInfoClassifiedString(_sig.ClassifiedString);

		public override FunctionSignature Signature
		{
			get { return _sig; }
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

		public override bool ArgumentsRequired
		{
			get { return true; }
		}

		public override IEnumerable<ArgumentDescriptor> Arguments
		{
			get
			{
				return _sig.Arguments;
			}
		}

		public override FunctionSignature ArgumentsSignature
		{
			get
			{
				return _sig;
			}
		}

		public override DataType DataType
		{
			get
			{
				return _returnDataType;
			}
		}

		public override bool CanRead
		{
			get
			{
				return _returnDataType != null && !_returnDataType.IsVoid;
			}
		}

		public override bool RequiresParent(string curClassName)
		{
			return true;
		}
	}
}
