using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DK.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DK.Modeling
{
	public class FunctionSignature
	{
		private bool _extern;
		private FunctionPrivacy _privacy;
		private DataType _returnDataType;
		private string _className;
		private string _funcName;
		private ArgumentDescriptor[] _args;
		private ProbeClassifiedString _prettySignature;
		private string _devDesc;
		private ServerContext _serverContext;

		public static readonly FunctionSignature[] EmptyArray = new FunctionSignature[0];

		private FunctionSignature()
		{ }

		public FunctionSignature(
			bool isExtern,
			FunctionPrivacy privacy,
			DataType returnDataType,
			string className,
			string funcName,
			string devDesc,
			IEnumerable<ArgumentDescriptor> args,
			ServerContext serverContext)
		{
			_extern = isExtern;
			_privacy = privacy;
			_returnDataType = returnDataType;
			_className = className;
			_funcName = funcName;
			_devDesc = devDesc;
			_args = args.ToArray();
			_serverContext = serverContext;
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
				_prettySignature = _prettySignature,
				_serverContext = _serverContext
			};
		}

		public string ClassName => _className;
		public string FunctionName => _funcName;
		public string FullName => !string.IsNullOrEmpty(_className) ? string.Concat(_className, ".", _funcName) : _funcName;
		public override string ToString() => PrettySignature;
		public ServerContext ServerContext => _serverContext;
		public FunctionPrivacy Privacy => _privacy;
		public DataType ReturnDataType => _returnDataType;

		public string Description
		{
			get { return _devDesc; }
			set { _devDesc = value; }
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
				var pcs = new ProbeClassifiedStringBuilder();
				var spaceRequired = false;

				if (_privacy != FunctionPrivacy.Public)
				{
					pcs.AddKeyword(_privacy.ToString().ToLower());
					spaceRequired = true;
				}

				if (_extern)
				{
					if (spaceRequired) pcs.AddSpace();
					pcs.AddKeyword("extern");
					spaceRequired = true;
				}

				if (_returnDataType != null)
				{
					if (spaceRequired) pcs.AddSpace();
					pcs.AddClassifiedString(_returnDataType.GetClassifiedString(shortVersion: true));
					spaceRequired = true;
				}

				if (spaceRequired) pcs.AddSpace();
				pcs.AddFunction(_funcName);
				pcs.AddOperator("(");
				spaceRequired = false;

				var firstArg = true;
				foreach (var arg in _args)
				{
					if (firstArg) firstArg = false;
					else pcs.AddDelimiter(", ");

					var argStartPos = pcs.Length;
					var argSource = arg.ClassifiedString;
					pcs.AddClassifiedString(argSource);
					arg.SignatureSpan = new CodeSpan(argStartPos, argStartPos + argSource.Length);
				}

				pcs.AddOperator(")");
				_prettySignature = pcs.ToClassifiedString();
			}
		}

		public string PrettySignature
		{
			get
			{
				CheckPrettySignature();
				return _prettySignature.ToString();
			}
		}

		public ProbeClassifiedString ClassifiedString
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

			if (_serverContext != ServerContext.Neutral)
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append(_serverContext == ServerContext.Server ? "sc" : "cc");
			}

			return sb.ToString();
		}

		public static FunctionSignature ParseFromDb(string str, DkAppSettings appSettings)
		{
			var code = new CodeParser(str);

			bool isExtern = false;
			FunctionPrivacy privacy = FunctionPrivacy.Public;
			DataType returnDataType = null;
			string className = null;
			string funcName = string.Empty;
			string devDesc = null;
			var args = new List<ArgumentDescriptor>();
			var serverContext = ServerContext.Neutral;

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
						if (!code.ReadStringLiteral()) Log.Debug("Unable to read return data type from: {0}", str);
						else
						{
							var dtString = CodeParser.StringLiteralToString(code.Text);
							returnDataType = DataType.TryParse(new DataType.ParseArgs(new CodeParser(dtString), appSettings));
							if (returnDataType == null)
							{
								Log.Debug("Unable to parse return data type from: {0}", dtString);
								returnDataType = new DataType(ValType.Unknown, null,
									new ProbeClassifiedString(ProbeClassifierType.DataType, dtString));
							}
						}
						break;
					case "cls":
						if (!code.ReadWord()) Log.Debug("Unable to read class name from: {0}", str);
						else className = code.Text;
						break;
					case "fn":
						if (!code.ReadWord()) Log.Debug("Unable to read function name from: {0}", str);
						else funcName = code.Text;
						break;
					case "desc":
						if (!code.ReadStringLiteral()) Log.Debug("Unable to read description from: {0}", str);
						else devDesc = CodeParser.StringLiteralToString(code.Text);
						break;
					case "arg":
						if (!code.ReadStringLiteral()) Log.Debug("Unable to read return data type from: {0}", str);
						else
						{
							var argString = CodeParser.StringLiteralToString(code.Text);
							var arg = ArgumentDescriptor.ParseFromDb(argString, appSettings);
							if (arg == null) Log.Debug("Unable to parse argument from: {0}", argString);
							else args.Add(arg);
						}
						break;
					case "sc":
						serverContext = ServerContext.Server;
						break;
					case "cc":
						serverContext = ServerContext.Client;
						break;
					default:
						Log.Debug("Unexpected word '{0}' in function signature: {1}", code.Text, str);
						stopParsing = true;
						break;
				}
			}

			return new FunctionSignature(isExtern, privacy, returnDataType, className, funcName, devDesc, args, serverContext);
		}

		public bool Extern
		{
			get { return _extern; }
		}

		public void ApplyDocumentation(string fileName)
		{
			var doc = SignatureDocumentor.GetDocumentation(fileName, _funcName);
			if (doc != null)
			{
				if (string.IsNullOrEmpty(_devDesc)) _devDesc = doc.Description;

				var args = doc.Arguments;
				var numArgs = _args.Length < args.Length ? _args.Length : args.Length;
				for (int a = 0; a < numArgs; a++)
				{
					if (string.IsNullOrEmpty(_args[a].Name)) _args[a].Name = args[a];
				}
			}
		}
	}
}
