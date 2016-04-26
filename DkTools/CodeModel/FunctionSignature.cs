using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	class FunctionSignature
	{
		private bool _extern;
		private FunctionPrivacy _privacy;
		private DataType _returnDataType;
		private string _className;
		private string _funcName;
		private ArgumentDescriptor[] _args;
		private string _prettySignature;
		private string _devDesc;

		private FunctionSignature()
		{ }

		public FunctionSignature(bool isExtern, FunctionPrivacy privacy, DataType returnDataType, string className, string funcName, string devDesc, IEnumerable<ArgumentDescriptor> args)
		{
			_extern = isExtern;
			_privacy = privacy;
			_returnDataType = returnDataType;
			_className = className;
			_funcName = funcName;
			_devDesc = devDesc;
			_args = args.ToArray();
		}

		public FunctionSignature Clone()
		{
			return new FunctionSignature
			{
				_extern = _extern,
				_privacy = _privacy,
				_returnDataType = _returnDataType,
				_className = _className,
				_funcName = _funcName,
				_args = _args.ToArray(),
				_devDesc = _devDesc,
				_prettySignature = _prettySignature
			};
		}

		public string ClassName
		{
			get { return _className; }
		}

		public string FunctionName
		{
			get { return _funcName; }
		}

		public string Description
		{
			get { return _devDesc; }
			set { _devDesc = value; }
		}

		public FunctionPrivacy Privacy
		{
			get { return _privacy; }
		}

		public DataType ReturnDataType
		{
			get { return _returnDataType; }
		}

		public IEnumerable<ArgumentDescriptor> Arguments
		{
			get
			{
				CheckPrettySignature();
				return _args;
			}
		}

		public ArgumentDescriptor TryGetArgument(int index)
		{
			if (index < 0 || index >= _args.Length) return null;
			CheckPrettySignature();
			return _args[index];
		}

		private void CheckPrettySignature()
		{
			if (_prettySignature == null)
			{
				var sb = new StringBuilder();
				var spaceRequired = false;

				if (_privacy != FunctionPrivacy.Public)
				{
					sb.Append(_privacy.ToString().ToLower());
					spaceRequired = true;
				}

				if (_extern)
				{
					if (spaceRequired) sb.Append(' ');
					sb.Append("extern");
					spaceRequired = true;
				}

				if (_returnDataType != null)
				{
					if (spaceRequired) sb.Append(' ');
					sb.Append(_returnDataType.ToPrettyString());
					spaceRequired = true;
				}

				if (spaceRequired) sb.Append(' ');
				sb.Append(_funcName);
				sb.Append('(');
				spaceRequired = false;

				var firstArg = true;
				foreach (var arg in _args)
				{
					if (firstArg) firstArg = false;
					else sb.Append(", ");

					var argStartPos = sb.Length;
					sb.Append(arg.SignatureText);
					arg.SignatureSpan = new Span(argStartPos, sb.Length);
				}

				sb.Append(')');
				_prettySignature = sb.ToString();
			}
		}

		public string PrettySignature
		{
			get
			{
				CheckPrettySignature();
				return _prettySignature;
			}
		}

		public string ToDbString()
		{
			var sb = new StringBuilder();

			if (_extern)
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append("ext");
			}

			switch (_privacy)
			{
				case FunctionPrivacy.Private:
					if (sb.Length > 0) sb.Append(' ');
					sb.Append("priv");
					break;
				case FunctionPrivacy.Protected:
					if (sb.Length > 0) sb.Append(' ');
					sb.Append("prot");
					break;
			}

			if (_returnDataType != null)
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append("rdt ");
				sb.Append(CodeParser.StringToStringLiteral(_returnDataType.ToCodeString()));
			}

			if (!string.IsNullOrEmpty(_className))
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append("cls ");
				sb.Append(_className);
			}

			if (!string.IsNullOrEmpty(_funcName))
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append("fn ");
				sb.Append(_funcName);
			}

			if (!string.IsNullOrEmpty(_devDesc))
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append("desc ");
				sb.Append(CodeParser.StringToStringLiteral(_devDesc));
			}

			foreach (var arg in _args)
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append("arg ");
				sb.Append(CodeParser.StringToStringLiteral(arg.ToDbString()));
			}

			return sb.ToString();
		}

		public static FunctionSignature ParseFromDb(string str)
		{
			var code = new CodeParser(str);

			bool isExtern = false;
			FunctionPrivacy privacy = FunctionPrivacy.Public;
			DataType returnDataType = null;
			string className = null;
			string funcName = string.Empty;
			string devDesc = null;
			var args = new List<ArgumentDescriptor>();

			var stopParsing = false;
			while (code.ReadWord() && !stopParsing)
			{
				switch (code.Text)
				{
					case "ext":
						isExtern = true;
						break;
					case "priv":
						privacy = FunctionPrivacy.Private;
						break;
					case "prot":
						privacy = FunctionPrivacy.Protected;
						break;
					case "rdt":
						if (!code.ReadStringLiteral()) Log.WriteDebug("Unable to read return data type from: {0}", str);
						else
						{
							var dtString = CodeParser.StringLiteralToString(code.Text);
							returnDataType = DataType.TryParse(new DataType.ParseArgs { Code = new CodeParser(dtString) });
							if (returnDataType == null)
							{
								Log.WriteDebug("Unable to parse return data type from: {0}", dtString);
								returnDataType = new DataType(ValType.Unknown, null, dtString);
							}
						}
						break;
					case "cls":
						if (!code.ReadWord()) Log.WriteDebug("Unable to read class name from: {0}", str);
						else className = code.Text;
						break;
					case "fn":
						if (!code.ReadWord()) Log.WriteDebug("Unable to read function name from: {0}", str);
						else funcName = code.Text;
						break;
					case "desc":
						if (!code.ReadStringLiteral()) Log.WriteDebug("Unable to read description from: {0}", str);
						else devDesc = CodeParser.StringLiteralToString(code.Text);
						break;
					case "arg":
						if (!code.ReadStringLiteral()) Log.WriteDebug("Unable to read return data type from: {0}", str);
						else
						{
							var argString = CodeParser.StringLiteralToString(code.Text);
							var arg = ArgumentDescriptor.ParseFromDb(argString);
							if (arg == null) Log.WriteDebug("Unable to parse argument from: {0}", argString);
							else args.Add(arg);
						}
						break;
					default:
						Log.WriteDebug("Unexpected word '{0}' in function signature: {1}", code.Text, str);
						stopParsing = true;
						break;
				}
			}

			return new FunctionSignature(isExtern, privacy, returnDataType, className, funcName, devDesc, args);
		}

		public bool Extern
		{
			get { return _extern; }
		}

		
	}
}
