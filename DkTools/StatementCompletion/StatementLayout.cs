using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;

namespace DkTools.StatementCompletion
{
	internal enum StatementState
	{
		None = 0x0000,

		#region create
		Create = 0x0100,
		CreateInterfacetype,
		CreateTime,
		CreateInterfacetypeName,
		CreateInterfacetypeTypeliblocator,
		CreateInterfacetypeTypeliblocatorName,
		CreateInterfacetypeFramework,
		#endregion

		#region select
		Select = 0x0200,
		SelectStar,
		SelectFrom,
		SelectFromTable,
		SelectFromTableComma,
		SelectFromTableOf,
		SelectFromTableOfTable,
		SelectFromTableList,
		Order,
		OrderBy,
		OrderByTable,
		OrderByTableDot,
		OrderByTableField,
		OrderByTableFieldAscDesc,
		OrderByTableFieldComma,
		Before,
		After,
		BeforeAfterGroup,
		#endregion

		#region format
		Format = 0x0300,
		FormatRows = 0x0301,
		FormatCols = 0x0302,
		FormatGenpages = 0x0303,
		FormatOutfile = 0x0304,
		FormatRowsEquals = 0x0305,
		FormatColsEquals = 0x0306,
		FormatGenpagesEquals = 0x0307,
		FormatRowsNumber = 0x0308,
		FormatColsNumber = 0x0309,
		FormatGenpagesNumber = 0x0310,
		#endregion

		#region interface
		Interface,
		#endregion

		#region onerror
		Onerror,
		#endregion
	}

	internal class StatementLayout
	{
		public static StatementState ProcessWord(string word, StatementState state)
		{
			switch (state)
			{
				case StatementState.None:
					switch (word)
					{
						case "after": return StatementState.After;
						case "before": return StatementState.Before;
						case "create": return StatementState.Create;
						case "format": return StatementState.Format;
						case "interface": return StatementState.Interface;
						case "onerror": return StatementState.Onerror;
						case "order": return StatementState.Order;
						case "select": return StatementState.Select;
					}
					break;

				#region create
				case StatementState.Create:
					switch (word)
					{
						case "interfacetype": return StatementState.CreateInterfacetype;
						case "time": return StatementState.CreateTime;
					}
					break;

				case StatementState.CreateInterfacetype:
					return StatementState.CreateInterfacetypeName;

				case StatementState.CreateInterfacetypeName:
					switch (word)
					{
						case "path":
						case "progid":
						case "clsid":
						case "tlibid":
						case "iid":
							return StatementState.CreateInterfacetypeTypeliblocator;

						case "framework":
							return StatementState.CreateInterfacetypeFramework;
					}
					break;

				case StatementState.CreateInterfacetypeTypeliblocator:
					return StatementState.CreateInterfacetypeTypeliblocatorName;
				#endregion

				#region selects
				case StatementState.SelectStar:
					if (word == "from") return StatementState.SelectFrom;
					break;

				case StatementState.SelectFrom:
					if (ProbeEnvironment.IsProbeTable(word)) return StatementState.SelectFromTable;
					break;

				case StatementState.SelectFromTable:
					if (word == "of") return StatementState.SelectFromTableOf;
					if (word == "order") return StatementState.Order;
					break;

				case StatementState.SelectFromTableComma:
					if (ProbeEnvironment.IsProbeTable(word)) return StatementState.SelectFromTableList;
					break;

				case StatementState.SelectFromTableOf:
					if (ProbeEnvironment.IsProbeTable(word)) return StatementState.SelectFromTableOfTable;
					break;

				case StatementState.Order:
					if (word == "by") return StatementState.OrderBy;
					break;

				case StatementState.OrderBy:
				case StatementState.OrderByTableFieldComma:
					if (ProbeEnvironment.IsValidTableName(word)) return StatementState.OrderByTable;
					break;

				case StatementState.OrderByTableDot:
					if (ProbeEnvironment.IsValidFieldName(word)) return StatementState.OrderByTableField;
					break;

				case StatementState.OrderByTableField:
					if (word == "asc" || word == "desc") return StatementState.OrderByTableFieldAscDesc;
					break;

				case StatementState.Before:
				case StatementState.After:
					if (word == "group") return StatementState.BeforeAfterGroup;
					break;
				#endregion

				#region format
				case StatementState.Format:
					if (word == "rows") return StatementState.FormatRows;
					if (word == "cols") return StatementState.FormatCols;
					if (word == "genpages") return StatementState.FormatGenpages;
					if (word == "outfile") return StatementState.FormatOutfile;
					break;

				case StatementState.FormatRowsEquals:
					return StatementState.FormatRowsNumber;

				case StatementState.FormatRowsNumber:
					if (word == "cols") return StatementState.FormatCols;
					if (word == "genpages") return StatementState.FormatGenpages;
					if (word == "outfile") return StatementState.FormatOutfile;
					break;

				case StatementState.FormatColsEquals:
					return StatementState.FormatColsNumber;

				case StatementState.FormatColsNumber:
					if (word == "genpages") return StatementState.FormatGenpages;
					if (word == "outfile") return StatementState.FormatOutfile;
					break;

				case StatementState.FormatGenpagesEquals:
					return StatementState.FormatGenpagesNumber;

				case StatementState.FormatGenpagesNumber:
					if (word == "outfile") return StatementState.FormatOutfile;
					break;
				#endregion
			}

			return StatementState.None;
		}

		public static StatementState ProcessNumber(StatementState state)
		{
			switch (state)
			{
				case StatementState.None:
					return StatementState.None;

				#region format
				case StatementState.FormatRowsEquals:
					return StatementState.FormatRowsNumber;

				case StatementState.FormatColsEquals:
					return StatementState.FormatColsNumber;

				case StatementState.FormatGenpagesEquals:
					return StatementState.FormatGenpagesNumber;
				#endregion
			}
			return StatementState.None;
		}

		public static StatementState ProcessStringLiteral(StatementState state)
		{
			switch (state)
			{
				case StatementState.None:
					return StatementState.None;

				#region create
				case StatementState.CreateInterfacetypeTypeliblocator:
					return StatementState.CreateInterfacetypeTypeliblocatorName;
				#endregion

				#region selects
				case StatementState.Select:	// Optional name for select
					return StatementState.Select;
				#endregion
			}

			return StatementState.None;
		}

		public static StatementState ProcessSymbol(char ch, StatementState state)
		{
			switch (ch)
			{
				case '*':
					if (state == StatementState.Select) return StatementState.SelectStar;
					break;

				case ',':
					switch (state)
					{
						case StatementState.SelectFromTable:
							return StatementState.SelectFromTableComma;
						case StatementState.OrderByTableField:
						case StatementState.OrderByTableFieldAscDesc:
							return StatementState.OrderByTableFieldComma;
					}
					break;

				case '.':
					if (state == StatementState.OrderByTable) return StatementState.OrderByTableDot;
					break;

				case '=':
					if (state == StatementState.FormatRows) return StatementState.FormatRowsEquals;
					if (state == StatementState.FormatCols) return StatementState.FormatColsEquals;
					if (state == StatementState.FormatGenpages) return StatementState.FormatGenpagesEquals;
					break;
			}

			return StatementState.None;
		}

		public static IEnumerable<string> GetNextPossibleKeywords(StatementState state)
		{
			switch (state)
			{
				case StatementState.None:
					break;

				#region create
				case StatementState.Create:
					yield return "index";
					yield return "interfacetype";
					yield return "relationship";
					yield return "stringdef";
					yield return "table";
					yield return "time";
					yield return "typedef";
					yield return "workspace";
					break;

				case StatementState.CreateTime:
					yield return "relationship";
					break;

				case StatementState.CreateInterfacetypeName:
					yield return "path";
					yield return "framework";
					yield return "progid";
					yield return "clsid";
					yield return "tlibid";
					yield return "iid";
					break;

				case StatementState.CreateInterfacetypeTypeliblocatorName:
					yield return "default";
					yield return "defaultevent";
					yield return "interface";
					break;

				case StatementState.CreateInterfacetypeFramework:
					yield return "interface";
					break;
				#endregion

				#region selects
				case StatementState.SelectStar:
					yield return "from";
					break;

				case StatementState.SelectFromTable:
					yield return "of";
					yield return "where";
					break;

				case StatementState.SelectFromTableOf:
				case StatementState.SelectFromTableList:
				case StatementState.SelectFromTableOfTable:
					yield return "where";
					break;

				case StatementState.Order:
					yield return "by";
					break;

				case StatementState.OrderByTableField:
					yield return "asc";
					yield return "desc";
					break;

				case StatementState.Before:
				case StatementState.After:
					yield return "group";
					break;

				case StatementState.BeforeAfterGroup:
					yield return "all";
					break;
				#endregion

				#region format
				case StatementState.Format:
					yield return "rows";
					yield return "cols";
					yield return "genpages";
					yield return "outfile";
					break;

				case StatementState.FormatRowsNumber:
					yield return "cols";
					yield return "genpages";
					yield return "outfile";
					break;

				case StatementState.FormatColsNumber:
					yield return "genpages";
					yield return "outfile";
					break;

				case StatementState.FormatGenpagesNumber:
					yield return "outfile";
					break;
				#endregion

				#region onerror
				case StatementState.Onerror:
					yield return "goto";
					yield return "resume";
					break;
				#endregion
			}
		}

		public static IEnumerable<Completion> GetCompletionsAfterToken(StatementState state)
		{
			foreach (var keyword in GetNextPossibleKeywords(state))
			{
				yield return ProbeCompletionSource.CreateCompletion(keyword, CompletionType.Keyword);
			}

			switch (state)
			{
				case StatementState.None:
					break;

				#region selects
				case StatementState.Select:
					yield return ProbeCompletionSource.CreateCompletion("*", CompletionType.Keyword);
					break;

				case StatementState.SelectFrom:
				case StatementState.SelectFromTableComma:
				case StatementState.SelectFromTableOf:
					foreach (var table in ProbeEnvironment.Tables)
					{
						foreach (var def in table.Definitions)
						{
							yield return ProbeCompletionSource.CreateCompletion(def);
						}
					}
					break;
				#endregion

				#region format
				case StatementState.FormatRows:
				case StatementState.FormatCols:
				case StatementState.FormatGenpages:
				case StatementState.FormatOutfile:
					yield return ProbeCompletionSource.CreateCompletion("=", CompletionType.Keyword);
					break;
				#endregion

				#region interface
				case StatementState.Interface:
					foreach (var intf in ProbeEnvironment.InterfaceTypes)
					{
						yield return ProbeCompletionSource.CreateCompletion(intf.Definition);
					}
					break;
				#endregion
			}
		}


	}
}
