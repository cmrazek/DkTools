using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal sealed class Field : IDictObj
	{
		public string Name { get; private set; }
		public string Prompt { get; private set; }
		public string Comment { get; private set; }
		public CodeModel.DataType DataType { get; private set; }

		private CodeModel.Definitions.TableFieldDefinition _definition;
		private FieldParentType _parentType;
		private string _parentName;
		private string _fullName;

		public Field(FieldParentType parentType, string parentName, DICTSRVRLib.IPColumn repoCol)
		{
			Name = repoCol.Name;
			//_repoCol = repoCol;
			_parentType = parentType;
			_parentName = parentName;

			var desc = repoCol as DICTSRVRLib.IPDObjDesc;
			Prompt = desc.Prompt[0];
			Comment = desc.Comment[0];

			var data = repoCol as DICTSRVRLib.IPDataDef;

			var dataTypeText = data.TypeText[0];
			var parser = new TokenParser.Parser(dataTypeText);
			DataType = CodeModel.DataType.Parse(parser, flags: CodeModel.DataType.ParseFlag.FromRepo);
			if (DataType == null)
			{
				DataType = new CodeModel.DataType(dataTypeText);
				Log.WriteDebug("DataType.Parse was unable to parse [{0}]", dataTypeText);
			}
			else if (parser.Read())
			{
				DataType = new CodeModel.DataType(dataTypeText);
				Log.WriteDebug("DataType.Parse stopped before end of text [{0}] got [{1}]", dataTypeText, DataType.Name);
			}

			var dictObj = repoCol as DICTSRVRLib.IPDictObj;
			string description = null;
			if (dictObj != null)
			{
				description = dictObj.DevInfo;
				if (description != null && description.Length == 0) description = null;
			}

			_definition = new CodeModel.Definitions.TableFieldDefinition(parentName, Name, Prompt, Comment, DataType, description);

			_fullName = string.Concat(_parentName, ".", Name);
		}

		public IEnumerable<Definition> CompletionOptions
		{
			get { return DataType.CompletionOptions; }
		}

		public CodeModel.Definitions.TableFieldDefinition Definition
		{
			get { return _definition; }
		}

		public object CreateRepoObject(Dict dict)
		{
			switch (_parentType)
			{
				case FieldParentType.Table:
					{
						var table = dict.GetTable(_parentName);
						if (table != null)
						{
							return table.Columns[Name];
						}
					}
					return null;

				case FieldParentType.Relationship:
					{
						var rel = dict.GetRelationship(_parentName);
						if (rel != null)
						{
							var table = rel.Child;
							if (table != null)
							{
								return table.Columns[Name];
							}
						}
					}
					return null;

				default:
					return null;
			}
		}

		public string FullName
		{
			get { return _fullName; }
		}

		public static string GetTableFieldExternalRefId(string tableName, string fieldName)
		{
			return string.Concat("tableField:", tableName, ".", fieldName);
		}
	}

	internal enum FieldParentType
	{
		Table,
		Relationship
	}
}
