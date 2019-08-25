using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class ExtractFieldDefinition : Definition
	{
		private ExtractTableDefinition _ex;
		private DataType _dataType;

		public ExtractFieldDefinition(string name, FilePosition filePos, ExtractTableDefinition tableDef)
			: base(name, filePos, tableDef.Permanent ? GetExternalRefId(tableDef.Name, name) : null)
		{ }

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.TableField; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableField; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (_ex != null) return string.Concat(_ex.Name, ".", Name);
				else return Name;
			}
		}

		public override object QuickInfoElements
		{
			get
			{
				if (_ex == null)
				{
					return QuickInfoStack(
						QuickInfoClassified(QuickInfoRun(Classifier.ProbeClassifierType.TableField, Name)),
						QuickInfoDescription(_ex.Permanent ? "Permanent extract" : "Temporary extract")
					);
				}

				return QuickInfoStack(
					QuickInfoClassified(
						QuickInfoRun(Classifier.ProbeClassifierType.TableName, _ex.Name),
						QuickInfoRun(Classifier.ProbeClassifierType.Delimiter, "."),
						QuickInfoRun(Classifier.ProbeClassifierType.TableField, Name)
					),
					QuickInfoDescription(_ex.Permanent ? "Permanent extract" : "Temporary extract"),
					_dataType != null ? _dataType.QuickInfoElements : null
				);
			}
		}

		public ExtractTableDefinition ExtractDefinition
		{
			get { return _ex; }
			set { _ex = value; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string tableName, string fieldName)
		{
			return string.Concat("permx:", tableName, ".", fieldName);
		}

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override DataType DataType
		{
			get { return _dataType; }
		}

		public void SetDataType(DataType dataType)
		{
			_dataType = dataType;
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}
	}
}
