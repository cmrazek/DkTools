using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.StatementCompletion
{
	internal struct StatementState
	{
		public StatementSection Section { get; set; }
		public StatementFlag Flags { get; set; }

		public static readonly StatementState None = new StatementState(StatementSection.None, StatementFlag.None);

		public StatementState(StatementSection section, StatementFlag flags)
		{
			Section = section;
			Flags = flags;
		}

		public StatementState(StatementSection section)
		{
			Section = section;
			Flags = StatementFlag.None;
		}

		public StatementState(int iState)
		{
			Section = (StatementSection)(iState & 0x0000FFFF);
			Flags = (StatementFlag)(iState & 0xFFFF0000);
		}

		public int IValue => ((int)Section) | (((int)Flags) << 16);

		/// <summary>
		/// Changes the section without modifying the flags.
		/// </summary>
		/// <param name="section">The new section</param>
		/// <returns>A new state struct with the different section</returns>
		public StatementState SetSection(StatementSection section)
		{
			return new StatementState(section, Flags);
		}

		/// <summary>
		/// Sets one or more flags without modifying the section.
		/// </summary>
		/// <param name="flag">The flags to be set.</param>
		/// <returns>A new state struct with the flags set.</returns>
		public StatementState SetFlag(StatementFlag flag)
		{
			return new StatementState(Section, Flags | flag);
		}

		public bool HasFlag(StatementFlag flag)
		{
			return (Flags & flag) != 0;
		}
	}

	internal enum StatementSection
	{
		None = 0x0000,

		#region create
		Create = 0x0100,
		CreateInterfacetype = 0x0101,
		CreateTime = 0x0102,
		CreateInterfacetypeName = 0x0103,
		CreateInterfacetypeTypeliblocator = 0x0104,
		CreateInterfacetypeTypeliblocatorName = 0x0105,
		CreateInterfacetypeFramework = 0x0106,
		#endregion

		#region select
		Select = 0x0200,
		SelectStar = 0x0201,
		SelectFrom = 0x0202,
		SelectFromTable = 0x0203,
		SelectFromTableComma = 0x0204,
		SelectFromTableOf = 0x0205,
		SelectFromTableOfTable = 0x0206,
		SelectFromTableList = 0x0207,
		Order = 0x0208,
		OrderBy = 0x0209,
		OrderByTable = 0x020A,
		OrderByTableDot = 0x020B,
		OrderByTableField = 0x020C,
		OrderByTableFieldAscDesc = 0x020D,
		OrderByTableFieldComma = 0x020E,
		Before = 0x020F,
		After = 0x0210,
		BeforeAfterGroup = 0x0211,
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
		Interface = 0x0400,
		#endregion

		#region onerror
		Onerror = 0x0500,
		#endregion

		#region alter
		Alter = 0x0600,
		AlterColumn = 0x0601,
		AlterColumnName = 0x0602,
		AlterTable = 0x0603,
		AlterApplication = 0x0604,
		AlterApplicationAppIID = 0x0605,
		AlterApplicationAppIIDName = 0x0606,
		AlterApplicationPrompt = 0x0607,
		AlterApplicationPromptString = 0x0608,
		AlterApplicationComment = 0x0609,
		AlterApplicationCommentString = 0x060A,
		AlterApplicationLangId = 0x060B,
		AlterApplicationLangIdNumber = 0x060C,
		AlterApplicationDescription = 0x060D,
		AlterApplicationDescriptionString = 0x060E,
		AlterApplicationBrackets = 0x060F,
		AlterApplicationExtends = 0x0610,
		AlterApplicationExtendsAppIID = 0x0611,
		AlterApplicationExtendsAppIIDString = 0x0612,
		AlterStringdef = 0x0613,
		AlterTypedef = 0x0614,
		#endregion

		#region Data Types (0x0700-0x07FF)
		/// <summary>
		/// User typed numeric keyword
		/// </summary>
		Numeric = 0x0700,
		/// <summary>
		/// User typed 'numeric(' and next is the width or precision numbers
		/// </summary>
		NumericBracket = 0x0701,
		/// <summary>
		/// User typed width or precision number and next is ')' or ','
		/// </summary>
		NumericWithinWidth = 0x0702,
		Int = 0x703,
		Signed = 0x0704,
		Unsigned = 0x0705,
		#endregion
	}

	[Flags]
	internal enum StatementFlag
	{
		None = 0x00,
		HasSign = 0x01,
		HasCurrency = 0x02,
		HasLeadingZeros = 0x04,
		HasWidth = 0x08,
		HasMask = 0x10,
	}
}
