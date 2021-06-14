using DK.AppEnvironment;
using DK.Code;
using DK.Modeling;
using DK.Schema;
using DK.Syntax;
using System.Collections.Generic;

namespace DK.Definitions
{
	public class RelIndDefinition : Definition
	{
		private RelInd _relind;
		private string _baseTableName;
		private ProbeClassifiedString _source;
		private string _devDesc;

		public static readonly RelIndDefinition Physical = new RelIndDefinition(
			new RelInd(RelIndType.Index, "physical", 0, string.Empty, FilePosition.Empty), string.Empty, ProbeClassifiedString.Empty, 
			"Index on rowno", FilePosition.Empty);

		public RelIndDefinition(RelInd relind, string baseTableName, ProbeClassifiedString source, string devDesc, FilePosition filePos)
			: base(relind.Name, filePos, GetExternalRefId(baseTableName, relind.Name))
		{
			_relind = relind;
			_source = source;
			_devDesc = devDesc;
			_baseTableName = baseTableName;
		}

		public override ServerContext ServerContext => ServerContext.Neutral;

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Table; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.TableName; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (!string.IsNullOrEmpty(_devDesc)) return string.Concat(_source, "\r\n", _devDesc);
				return _source.ToString();
			}
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoClassifiedString(_source),
			string.IsNullOrWhiteSpace(_devDesc) ? null : new QuickInfoDescription(_devDesc)
		);

		public string BaseTableName
		{
			get { return _baseTableName; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string baseTableName, string name)
		{
			return string.Concat("relind:", baseTableName, ".", name);
		}

		public override bool AllowsChild
		{
			get { return true; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override IEnumerable<Definition> GetChildDefinitions(string name, DkAppSettings appSettings)
		{
			var col = _relind.GetColumn(name);
			if (col != null) yield return col.Definition;
		}

		public override IEnumerable<Definition> GetChildDefinitions(DkAppSettings appSettings) => _relind.ColumnDefinitions;

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override DataType DataType
		{
			get { return DataType.IndRel; }
		}

		public override bool RequiresRefDataType
		{
			get { return true; }
		}
	}
}
