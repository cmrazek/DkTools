using DkTools.Classifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text.Adornments;
using System.Windows;
using DkTools.QuickInfo;
using DkTools.GlobalData;

namespace DkTools.CodeModel.Definitions
{
	internal class FunctionDefinition : Definition
	{
		private FunctionSignature _sig;
		private int _bodyStartPos;
		private int _argsStartPos;
		private int _argsEndPos;
		private Span _entireSpan;

		public FunctionDefinition(
			FunctionSignature signature,
			FilePosition filePos,
			int argsStartPos,
			int argsEndPos,
			int bodyStartPos,
			Span entireSpan)
			: base(signature.FunctionName, filePos, MakeExtRefId(signature.ClassName, signature.FunctionName))
		{
			_sig = signature ?? throw new ArgumentNullException(nameof(signature));
			_argsStartPos = argsStartPos;
			_argsEndPos = argsEndPos;
			_bodyStartPos = bodyStartPos;
			_entireSpan = entireSpan;
		}

		public FunctionDefinition(FunctionSignature signature, FilePosition filePos)
			: base(signature.FunctionName, filePos, MakeExtRefId(signature.ClassName, signature.FunctionName))
		{
			_sig = signature ?? throw new ArgumentNullException(nameof(signature));
			_argsStartPos = _argsEndPos = _bodyStartPos = 0;
			_entireSpan = Span.Empty;
		}

		public FunctionDefinition(FunctionSignature signature)
			: base(signature.FunctionName, FilePosition.Empty, string.Concat("func:", signature.FunctionName))
		{
			_sig = signature ?? throw new ArgumentNullException(nameof(signature));
			_argsStartPos = _argsEndPos = _bodyStartPos = 0;
			_entireSpan = Span.Empty;
		}

		public FunctionDefinition CloneAsExtern()
		{
			return new FunctionDefinition(_sig.Clone(), FilePosition, _argsStartPos, _argsEndPos, _bodyStartPos, _entireSpan);
		}

		/// <summary>
		/// Gets the full name "className.funcName" or "funcName" of this function.
		/// </summary>
		public string FullName => _sig.FullName;

		public override DataType DataType
		{
			get { return _sig.ReturnDataType; }
		}

		public override IEnumerable<ArgumentDescriptor> Arguments
		{
			get { return _sig.Arguments; }
		}

		public override FunctionSignature Signature
		{
			get { return _sig; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.Function; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Function; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (string.IsNullOrEmpty(_sig.Description)) return _sig.PrettySignature;
				return string.Concat(_sig.PrettySignature, "\r\n\r\n", _sig.Description);
			}
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoClassifiedString(_sig.ClassifiedString),
			string.IsNullOrWhiteSpace(_sig.Description) ? null : new QuickInfoDescription(_sig.Description)
		);

		public int BodyStartPosition
		{
			get { return _bodyStartPos; }
		}

		public int ArgsStartPosition
		{
			get { return _argsStartPos; }
		}

		public int ArgsEndPosition
		{
			get { return _argsEndPos; }
		}

		public FunctionPrivacy Privacy
		{
			get { return _sig.Privacy; }
		}

		public bool Extern
		{
			get { return _sig.Extern; }
		}

		public override void DumpTreeAttribs(System.Xml.XmlWriter xml)
		{
			base.DumpTreeAttribs(xml);

			xml.WriteAttributeString("signature", _sig.PrettySignature);
			xml.WriteAttributeString("bodyStartPos", _bodyStartPos.ToString());
		}

		public string ClassName
		{
			get { return _sig.ClassName; }
		}

		/// <summary>
		/// Span of the function, from start of the return data type to the end of the closing brace, in primary file coordinates.
		/// </summary>
		public Span EntireSpan
		{
			get { return _entireSpan; }
		}

		public override string PickText
		{
			get { return _sig.PrettySignature; }
		}

		public override bool ArgumentsRequired
		{
			get { return true; }
		}

		public override FunctionSignature ArgumentsSignature
		{
			get
			{
				return _sig;
			}
		}

		public override bool AllowsFunctionBody
		{
			get
			{
				return true;
			}
		}

		public override bool CanRead
		{
			get
			{
				return _sig.ReturnDataType != null && !_sig.ReturnDataType.IsVoid;
			}
		}

		public override bool RequiresParent(string curClassName)
		{
			if (string.IsNullOrEmpty(_sig.ClassName)) return false;

			if (_sig.ClassName.Equals(curClassName, StringComparison.OrdinalIgnoreCase))
			{
				// Processing the same class file, so can call methods without specifying the class name first
				return false;
			}
			else
			{
				// In a different class file, so class name is required
				return true;
			}
		}

		#region External Reference ID
		private static readonly Regex _rxExtRefId = new Regex(@"^(?:class\:(\w+)\.)?func\:(\w+)$");

		public static string MakeExtRefId(string className, string funcName)
		{
			return !string.IsNullOrEmpty(className)
				? string.Concat("class:", className, ".func:", funcName)
				: string.Concat("func:", funcName);
		}

		public static string MakeFullName(string className, string funcName)
		{
			return !string.IsNullOrEmpty(className)
				? string.Concat(className, ".", funcName)
				: funcName;
		}

		public static string ParseFullNameFromExtRefId(string extRefId)
		{
			var match = _rxExtRefId.Match(extRefId);
			if (!match.Success) return null;

			var className = match.Groups[1].Value;
			var funcName = match.Groups[2].Value;
			if (string.IsNullOrEmpty(className)) return funcName;
			return string.Concat(className, funcName);
		}

		public static string ParseClassNameFromExtRefId(string extRefId)
		{
			var match = _rxExtRefId.Match(extRefId);
			if (!match.Success) return null;
			return match.Groups[1].Value;
		}

		public static string ParseFunctionNameFromExtRefId(string extRefId)
		{
			var match = _rxExtRefId.Match(extRefId);
			if (!match.Success) return null;
			return match.Groups[2].Value;
		}
		#endregion
	}
}
