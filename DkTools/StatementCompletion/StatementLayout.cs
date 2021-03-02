using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;

namespace DkTools.StatementCompletion
{
	internal class StatementLayout
	{
		public static StatementState ProcessWord(string word, StatementState state, DkAppSettings appSettings)
		{
			switch (state.Section)
			{
				case StatementSection.None:
					switch (word)
					{
						case "after": return new StatementState(StatementSection.After);
						case "alter": return new StatementState(StatementSection.Alter);
						case "before": return new StatementState(StatementSection.Before);
						case "create": return new StatementState(StatementSection.Create);
						case "format": return new StatementState(StatementSection.Format);
						case "int": return new StatementState(StatementSection.Int);
						case "interface": return new StatementState(StatementSection.Interface);
						case "numeric": return new StatementState(StatementSection.Numeric);
						case "onerror": return new StatementState(StatementSection.Onerror);
						case "order": return new StatementState(StatementSection.Order);
						case "select": return new StatementState(StatementSection.Select);
						case "signed": return new StatementState(StatementSection.Signed, StatementFlag.HasSign);
						case "unsigned": return new StatementState(StatementSection.Unsigned, StatementFlag.HasSign);
					}
					break;

				#region create
				case StatementSection.Create:
					switch (word)
					{
						case "interfacetype": return new StatementState(StatementSection.CreateInterfacetype);
						case "time": return new StatementState(StatementSection.CreateTime);
					}
					break;

				case StatementSection.CreateInterfacetype:
					return new StatementState(StatementSection.CreateInterfacetypeName);

				case StatementSection.CreateInterfacetypeName:
					switch (word)
					{
						case "path":
						case "progid":
						case "clsid":
						case "tlibid":
						case "iid":
							return new StatementState(StatementSection.CreateInterfacetypeTypeliblocator);

						case "framework":
							return new StatementState(StatementSection.CreateInterfacetypeFramework);
					}
					break;

				case StatementSection.CreateInterfacetypeTypeliblocator:
					return new StatementState(StatementSection.CreateInterfacetypeTypeliblocatorName);
				#endregion

				#region selects
				case StatementSection.SelectStar:
					if (word == "from") return new StatementState(StatementSection.SelectFrom);
					break;

				case StatementSection.SelectFrom:
					if (appSettings.Dict.IsTable(word)) return new StatementState(StatementSection.SelectFromTable);
					break;

				case StatementSection.SelectFromTable:
					if (word == "of") return new StatementState(StatementSection.SelectFromTableOf);
					if (word == "order") return new StatementState(StatementSection.Order);
					break;

				case StatementSection.SelectFromTableComma:
					if (appSettings.Dict.IsTable(word)) return new StatementState(StatementSection.SelectFromTableList);
					break;

				case StatementSection.SelectFromTableOf:
					if (appSettings.Dict.IsTable(word)) return new StatementState(StatementSection.SelectFromTableOfTable);
					break;

				case StatementSection.Order:
					if (word == "by") return new StatementState(StatementSection.OrderBy);
					break;

				case StatementSection.OrderBy:
				case StatementSection.OrderByTableFieldComma:
					if (DkEnvironment.IsValidTableName(word)) return new StatementState(StatementSection.OrderByTable);
					break;

				case StatementSection.OrderByTableDot:
					if (DkEnvironment.IsValidFieldName(word)) return new StatementState(StatementSection.OrderByTableField);
					break;

				case StatementSection.OrderByTable:
				case StatementSection.OrderByTableField:
					if (word == "asc" || word == "desc") return new StatementState(StatementSection.OrderByTableFieldAscDesc);
					break;

				case StatementSection.Before:
				case StatementSection.After:
					if (word == "group") return new StatementState(StatementSection.BeforeAfterGroup);
					break;
				#endregion

				#region format
				case StatementSection.Format:
					if (word == "rows") return new StatementState(StatementSection.FormatRows);
					if (word == "cols") return new StatementState(StatementSection.FormatCols);
					if (word == "genpages") return new StatementState(StatementSection.FormatGenpages);
					if (word == "outfile") return new StatementState(StatementSection.FormatOutfile);
					break;

				case StatementSection.FormatRowsEquals:
					return new StatementState(StatementSection.FormatRowsNumber);

				case StatementSection.FormatRowsNumber:
					if (word == "cols") return new StatementState(StatementSection.FormatCols);
					if (word == "genpages") return new StatementState(StatementSection.FormatGenpages);
					if (word == "outfile") return new StatementState(StatementSection.FormatOutfile);
					break;

				case StatementSection.FormatColsEquals:
					return new StatementState(StatementSection.FormatColsNumber);

				case StatementSection.FormatColsNumber:
					if (word == "genpages") return new StatementState(StatementSection.FormatGenpages);
					if (word == "outfile") return new StatementState(StatementSection.FormatOutfile);
					break;

				case StatementSection.FormatGenpagesEquals:
					return new StatementState(StatementSection.FormatGenpagesNumber);

				case StatementSection.FormatGenpagesNumber:
					if (word == "outfile") return new StatementState(StatementSection.FormatOutfile);
					break;
				#endregion

				#region alter
				case StatementSection.Alter:
					switch (word)
					{
						case "application": return new StatementState(StatementSection.AlterApplication);
						case "column": return new StatementState(StatementSection.AlterColumn);
						case "table": return new StatementState(StatementSection.AlterTable);
						case "stringdef": return new StatementState(StatementSection.AlterStringdef);
						case "typedef": return new StatementState(StatementSection.AlterTypedef);
					}
					break;

				case StatementSection.AlterColumn:
					if (DkEnvironment.IsValidFieldName(word)) return new StatementState(StatementSection.AlterColumnName);
					break;

				case StatementSection.AlterApplication:
					if (word == "AppIID") return new StatementState(StatementSection.AlterApplicationAppIID);
					break;

				case StatementSection.AlterApplicationAppIID:
					if (word != "prompt") return new StatementState(StatementSection.AlterApplicationAppIIDName);
					break;

				case StatementSection.AlterApplicationAppIIDName:
					if (word == "prompt") return new StatementState(StatementSection.AlterApplicationPrompt);
					break;

				case StatementSection.AlterApplicationPrompt:
					return new StatementState(StatementSection.AlterApplicationPromptString);

				case StatementSection.AlterApplicationPromptString:
					if (word == "comment") return new StatementState(StatementSection.AlterApplicationComment);
					if (word == "langid") return new StatementState(StatementSection.AlterApplicationLangId);
					if (word == "description") return new StatementState(StatementSection.AlterApplicationDescription);
					break;

				case StatementSection.AlterApplicationComment:
					return new StatementState(StatementSection.AlterApplicationCommentString);

				case StatementSection.AlterApplicationCommentString:
					if (word == "langid") return new StatementState(StatementSection.AlterApplicationLangId);
					if (word == "description") return new StatementState(StatementSection.AlterApplicationDescription);
					break;

				case StatementSection.AlterApplicationLangId:
					return new StatementState(StatementSection.AlterApplicationLangIdNumber);

				case StatementSection.AlterApplicationDescription:
					return new StatementState(StatementSection.AlterApplicationDescriptionString);

				case StatementSection.AlterApplicationBrackets:
					if (word == "extends") return new StatementState(StatementSection.AlterApplicationExtends);
					break;

				case StatementSection.AlterApplicationExtends:
					if (word == "AppIID") return new StatementState(StatementSection.AlterApplicationExtendsAppIID);
					break;

				case StatementSection.AlterApplicationExtendsAppIID:
					return new StatementState(StatementSection.AlterApplicationExtendsAppIIDString);
				#endregion

				#region Data Types
				case StatementSection.Numeric:
					switch (word)
					{
						case "unsigned": return state.SetFlag(StatementFlag.HasSign);
						case "signed": return state.SetFlag(StatementFlag.HasSign);
						case "currency": return state.SetFlag(StatementFlag.HasCurrency);
						case "local_currency": return state.SetFlag(StatementFlag.HasCurrency);
						case "LEADINGZEROS": return state.SetFlag(StatementFlag.HasLeadingZeros);
						case "PROBE": return state;
					}
					break;

				case StatementSection.Int:
					switch (word)
					{
						case "signed":
						case "unsigned":
							return state.SetFlag(StatementFlag.HasSign);
					}
					break;

				case StatementSection.Signed:
				case StatementSection.Unsigned:
					switch (word)
					{
						case "int":
							return state.SetSection(StatementSection.Int);
					}
					break;
				#endregion
			}

			return StatementState.None;
		}

		public static StatementState ProcessNumber(StatementState state)
		{
			switch (state.Section)
			{
				case StatementSection.None:
					return StatementState.None;

				#region format
				case StatementSection.FormatRowsEquals:
					return new StatementState(StatementSection.FormatRowsNumber);

				case StatementSection.FormatColsEquals:
					return new StatementState(StatementSection.FormatColsNumber);

				case StatementSection.FormatGenpagesEquals:
					return new StatementState(StatementSection.FormatGenpagesNumber);
				#endregion

				#region alter
				case StatementSection.AlterApplicationLangId:
					return new StatementState(StatementSection.AlterApplicationLangIdNumber);
				#endregion

				#region Data Types
				case StatementSection.Numeric:
					return state.SetFlag(StatementFlag.HasWidth);
				case StatementSection.NumericBracket:
					return state.SetSection(StatementSection.NumericWithinWidth);
				case StatementSection.Int:
				case StatementSection.Signed:
				case StatementSection.Unsigned:
					return new StatementState(StatementSection.Int, state.Flags | StatementFlag.HasWidth);
				#endregion
			}
			return StatementState.None;
		}

		public static StatementState ProcessStringLiteral(StatementState state)
		{
			switch (state.Section)
			{
				case StatementSection.None:
					return StatementState.None;

				#region create
				case StatementSection.CreateInterfacetypeTypeliblocator:
					return new StatementState(StatementSection.CreateInterfacetypeTypeliblocatorName);
				#endregion

				#region selects
				case StatementSection.Select:	// Optional name for select
					return new StatementState(StatementSection.Select);
				#endregion

				#region alter
				case StatementSection.AlterApplicationAppIID:
					return new StatementState(StatementSection.AlterApplicationAppIIDName);

				case StatementSection.AlterApplicationPrompt:
					return new StatementState(StatementSection.AlterApplicationPromptString);

				case StatementSection.AlterApplicationComment:
					return new StatementState(StatementSection.AlterApplicationCommentString);

				case StatementSection.AlterApplicationDescription:
				case StatementSection.AlterApplicationDescriptionString:
					return new StatementState(StatementSection.AlterApplicationDescriptionString);

				case StatementSection.AlterApplicationExtendsAppIID:
					return new StatementState(StatementSection.AlterApplicationExtendsAppIIDString);
				#endregion

				#region Date Types
				case StatementSection.Numeric:
				case StatementSection.Int:
					return state.SetFlag(StatementFlag.HasMask);
				case StatementSection.Signed:
				case StatementSection.Unsigned:
					return state.SetSection(StatementSection.Int).SetFlag(StatementFlag.HasMask);
				#endregion
			}

			return StatementState.None;
		}

		public static StatementState ProcessSymbol(char ch, StatementState state)
		{
			switch (ch)
			{
				case '*':
					if (state.Section == StatementSection.Select) return new StatementState(StatementSection.SelectStar);
					break;

				case ',':
					switch (state.Section)
					{
						case StatementSection.SelectFromTable:
							return new StatementState(StatementSection.SelectFromTableComma);
						case StatementSection.OrderByTableField:
						case StatementSection.OrderByTableFieldAscDesc:
							return new StatementState(StatementSection.OrderByTableFieldComma);
						case StatementSection.NumericWithinWidth:
							return state.SetSection(StatementSection.NumericBracket);
					}
					break;

				case '.':
					if (state.Section == StatementSection.OrderByTable) return new StatementState(StatementSection.OrderByTableDot);
					break;

				case '=':
					if (state.Section == StatementSection.FormatRows) return new StatementState(StatementSection.FormatRowsEquals);
					if (state.Section == StatementSection.FormatCols) return new StatementState(StatementSection.FormatColsEquals);
					if (state.Section == StatementSection.FormatGenpages) return new StatementState(StatementSection.FormatGenpagesEquals);
					break;

				case '(':
					switch (state.Section)
					{
						case StatementSection.AlterApplicationPromptString:
						case StatementSection.AlterApplicationCommentString:
						case StatementSection.AlterApplicationLangIdNumber:
						case StatementSection.AlterApplicationDescriptionString:
							return new StatementState(StatementSection.AlterApplicationBrackets);
						case StatementSection.Numeric:
							return state.SetSection(StatementSection.NumericBracket);
					}
					break;

				case ')':
					switch (state.Section)
					{
						case StatementSection.NumericWithinWidth:
							return state.SetSection(StatementSection.Numeric).SetFlag(StatementFlag.HasWidth);
					}
					break;
			}

			return StatementState.None;
		}

		public static IEnumerable<string> GetNextPossibleKeywords(StatementState state)
		{
			switch (state.Section)
			{
				case StatementSection.None:
					break;

				#region create
				case StatementSection.Create:
					yield return "index";
					yield return "interfacetype";
					yield return "relationship";
					yield return "stringdef";
					yield return "table";
					yield return "time";
					yield return "typedef";
					yield return "workspace";
					break;

				case StatementSection.CreateTime:
					yield return "relationship";
					break;

				case StatementSection.CreateInterfacetypeName:
					yield return "path";
					yield return "framework";
					yield return "progid";
					yield return "clsid";
					yield return "tlibid";
					yield return "iid";
					break;

				case StatementSection.CreateInterfacetypeTypeliblocatorName:
					yield return "default";
					yield return "defaultevent";
					yield return "interface";
					break;

				case StatementSection.CreateInterfacetypeFramework:
					yield return "interface";
					break;
				#endregion

				#region selects
				case StatementSection.SelectStar:
					yield return "from";
					break;

				case StatementSection.SelectFromTable:
					yield return "of";
					yield return "where";
					break;

				case StatementSection.SelectFromTableOf:
				case StatementSection.SelectFromTableList:
				case StatementSection.SelectFromTableOfTable:
					yield return "where";
					break;

				case StatementSection.Order:
					yield return "by";
					break;

				case StatementSection.OrderByTable:
				case StatementSection.OrderByTableField:
					yield return "asc";
					yield return "desc";
					break;

				case StatementSection.Before:
				case StatementSection.After:
					yield return "group";
					break;

				case StatementSection.BeforeAfterGroup:
					yield return "all";
					break;
				#endregion

				#region format
				case StatementSection.Format:
					yield return "rows";
					yield return "cols";
					yield return "genpages";
					yield return "outfile";
					break;

				case StatementSection.FormatRowsNumber:
					yield return "cols";
					yield return "genpages";
					yield return "outfile";
					break;

				case StatementSection.FormatColsNumber:
					yield return "genpages";
					yield return "outfile";
					break;

				case StatementSection.FormatGenpagesNumber:
					yield return "outfile";
					break;
				#endregion

				#region onerror
				case StatementSection.Onerror:
					yield return "goto";
					yield return "resume";
					break;
				#endregion

				#region alter
				case StatementSection.Alter:
					yield return "application";
					yield return "column";
					yield return "stringdef";
					yield return "table";
					yield return "typedef";
					yield return "workspace";
					break;

				case StatementSection.AlterColumnName:
					yield return "sametype";
					break;

				case StatementSection.AlterApplication:
					yield return "AppIID";
					break;

				case StatementSection.AlterApplicationAppIIDName:
					yield return "prompt";
					break;

				case StatementSection.AlterApplicationPromptString:
					yield return "comment";
					yield return "langid";
					yield return "description";
					yield return "(";
					break;

				case StatementSection.AlterApplicationCommentString:
					yield return "langid";
					yield return "description";
					yield return "(";
					break;

				case StatementSection.AlterApplicationLangIdNumber:
					yield return "langid";
					yield return "description";
					yield return "(";
					break;

				case StatementSection.AlterApplicationDescriptionString:
					yield return "(";
					break;

				case StatementSection.AlterApplicationBrackets:
					yield return "extends";
					break;

				case StatementSection.AlterApplicationExtends:
					yield return "AppIID";
					break;

				case StatementSection.AlterApplicationExtendsAppIIDString:
					yield return ")";
					break;
				#endregion

				#region Data Types
				case StatementSection.Numeric:
					if (!state.HasFlag(StatementFlag.HasSign)) yield return "unsigned";
					if (!state.HasFlag(StatementFlag.HasCurrency)) yield return "currency";
					if (!state.HasFlag(StatementFlag.HasCurrency)) yield return "local_currency";
					if (!state.HasFlag(StatementFlag.HasLeadingZeros)) yield return "LEADINGZEROS";
					break;

				case StatementSection.Signed:
				case StatementSection.Unsigned:
					yield return "int";
					break;
				#endregion
			}
		}

		public static void GetCompletionsAfterToken(StatementState state, ProbeAsyncCompletionSource completionSource)
		{
			foreach (var keyword in GetNextPossibleKeywords(state))
			{
				completionSource.CreateCompletion(keyword, ProbeCompletionType.Keyword, null);
			}

			switch (state.Section)
			{
				case StatementSection.None:
					break;

				#region selects
				case StatementSection.Select:
					completionSource.CreateCompletion("*", ProbeCompletionType.Keyword, null);
					break;

				case StatementSection.SelectFrom:
				case StatementSection.SelectFromTableComma:
				case StatementSection.SelectFromTableOf:
					foreach (var table in completionSource.AppSettings.Dict.Tables)
					{
						foreach (var def in table.Definitions)
						{
							completionSource.CreateCompletion(def);
						}
					}
					break;
				#endregion

				#region format
				case StatementSection.FormatRows:
				case StatementSection.FormatCols:
				case StatementSection.FormatGenpages:
				case StatementSection.FormatOutfile:
					completionSource.CreateCompletion("=", ProbeCompletionType.Keyword, null);
					break;
				#endregion

				#region interface
				case StatementSection.Interface:
					foreach (var intf in completionSource.AppSettings.Dict.Interfaces)
					{
						completionSource.CreateCompletion(intf.Definition);
					}
					break;
				#endregion

				#region alter
				case StatementSection.AlterTable:
					foreach (var table in completionSource.AppSettings.Dict.Tables)
					{
						completionSource.CreateCompletion(table.Definition);
					}
					break;

				case StatementSection.AlterStringdef:
					foreach (var sd in completionSource.AppSettings.Dict.Stringdefs)
					{
						completionSource.CreateCompletion(sd.Definition);
					}
					break;

				case StatementSection.AlterTypedef:
					foreach (var td in completionSource.AppSettings.Dict.Typedefs)
					{
						completionSource.CreateCompletion(td.Definition);
					}
					break;
				#endregion
			}
		}
	}
}
