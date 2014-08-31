using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	internal class DataType
	{
		private string _name;
		private string _infoText;
		private string[] _completionOptions;
		private CompletionOptionsType _completionOptionsType;

		private enum CompletionOptionsType
		{
			None,
			List,
			Tables,
			RelInds
		}

		public static readonly DataType Int = new DataType("int", "int");
		public static readonly DataType Void = new DataType("void", "void");
		public static readonly DataType Table = new DataType("table", "table") { _completionOptionsType = CompletionOptionsType.Tables };
		public static readonly DataType IndRel = new DataType("indrel", "indrel") { _completionOptionsType = CompletionOptionsType.RelInds };

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(required) name of the data type</param>
		/// <param name="infoText">(required) Help text to be displayed to the user.</param>
		/// <param name="sourceToken">(optional) The token that created the data type.</param>
		public DataType(string name, string infoText)
		{
			_name = name;
			_infoText = infoText;
		}

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(required) Name of the data type</param>
		/// <param name="completionOptions">(optional) A list of hardcoded options for this data type.</param>
		/// <param name="infoText">(required) Help text to be displayed to the user.</param>
		/// <param name="sourceToken">(optional) The token that created the data type.</param>
		public DataType(string name, IEnumerable<string> completionOptions, string infoText)
		{
			_name = name;
			_infoText = infoText;

			if (completionOptions != null)
			{
				_completionOptions = completionOptions.ToArray();
				if (_completionOptions.Length > 0) _completionOptionsType = CompletionOptionsType.List;
			}
		}

		/// <summary>
		/// Returns a data type for a specific token.
		/// </summary>
		/// <param name="token">The token for which the data type is to be retrieved.</param>
		/// <returns>If the token already has an associated data type then, this will be returned; otherwise a new data type will be created with the token's text as the name.</returns>
		public static DataType FromToken(Token token)
		{
			if (token == null) return DataType.Int;

			// Check if this token has a data type built in.
			if (token is IDataTypeToken) return (token as IDataTypeToken).DataType;

			if (token is DataTypeToken) return (token as DataTypeToken).DataType;

			// Try to find a previously defined data type.
			var def = token.GetDefinitions<DataTypeDefinition>(token.Text).FirstOrDefault();
			if (def != null) return def.DataType;

			// Create a new data type using the token text as the definition.
			var dataType = new DataType(token.Text, token.Text);
			
			return dataType;
		}

		public static DataType FromString(string str)
		{
			return new DataType(str, str);
		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public bool HasCompletionOptions
		{
			get { return _completionOptionsType != CompletionOptionsType.None; }
		}

		public IEnumerable<string> CompletionOptions
		{
			get
			{
				switch (_completionOptionsType)
				{
					case CompletionOptionsType.List:
						return _completionOptions != null ? _completionOptions : new string[0];
					case CompletionOptionsType.Tables:
						return (from t in ProbeEnvironment.Tables select t.Name);
					case CompletionOptionsType.RelInds:
						return (from r in ProbeEnvironment.RelInds select r.Name);
					default:
						return new string[0];
				}
			}
			//set { _completionOptions = value.ToArray(); }
		}

		public static IEnumerable<string> ParseCompletionOptionsFromArgText(string source, CodeModel model)
		{
			var parser = new TokenParser.Parser(source);
			if (!parser.Read()) yield break;

			switch (parser.TokenText)
			{
				case "numeric":
				case "char":
				case "date":
				case "unsigned":
					yield break;

				case "enum":
					if (!parser.Read()) yield break;
					if (parser.TokenText == "proto" || parser.TokenText == "nowarn")
					{
						if (!parser.Read()) yield break;
					}
					if (parser.TokenText != "{") yield break;

					while (parser.Read())
					{
						if (parser.TokenText == "}") break;
						if (parser.TokenType == TokenParser.TokenType.Word || parser.TokenType == TokenParser.TokenType.StringLiteral) yield return parser.TokenText;
					}
					break;

				case "like":
					if (!parser.Read() || parser.TokenType != TokenParser.TokenType.Word) yield break;
					{
						var table = ProbeEnvironment.GetTable(parser.TokenText);
						if (table != null)
						{
							if (!parser.Read() || parser.TokenText != ".") yield break;

							if (!parser.Read() || parser.TokenType != TokenParser.TokenType.Word) yield break;
							var field = table.GetField(parser.TokenText);
							if (field != null)
							{
								foreach (var opt in field.CompletionOptions) yield return opt;
							}
						}
					}
					break;

				case "table":
					foreach (var table in ProbeEnvironment.Tables)
					{
						yield return table.Name;
					}
					break;

				case "indrel":
					foreach (var indrel in ProbeEnvironment.RelInds)
					{
						yield return indrel.Name;
					}
					break;

				default:
					if (model != null)
					{
						// This could be a name of a pre-defined data type.
						var def = model.GetDefinitions<DataTypeDefinition>(parser.TokenText).FirstOrDefault();
						if (def != null)
						{
							foreach (var opt in def.DataType.CompletionOptions) yield return opt;
						}
					}
					break;
			}
		}

		public void DumpTree(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement("dataType");
			xml.WriteAttributeString("name", _name);
			if (_completionOptions != null && _completionOptions.Length > 0)
			{
				var sb = new StringBuilder();
				foreach (var opt in _completionOptions)
				{
					if (sb.Length > 0) sb.Append(" ");
					sb.Append(opt);
				}
				xml.WriteAttributeString("completionOptions", sb.ToString());
			}
			xml.WriteEndElement();
		}

		public string InfoText
		{
			get { return string.IsNullOrWhiteSpace(_infoText) ? _name : _infoText; }
		}
	}
}
