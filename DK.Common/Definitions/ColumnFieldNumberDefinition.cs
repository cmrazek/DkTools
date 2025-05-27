using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;
using System.Text;

namespace DK.Definitions
{
    public class ColumnFieldNumberDefinition : Definition
    {
        private string _tableName;

        public ColumnFieldNumberDefinition(string tableName, string columnName)
            : base(columnName, DK.Code.FilePosition.Empty, null)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        }

        public override string ToString() => $"{_tableName}$${Name}";
        public override bool CompletionVisible => true;
        public override ProbeCompletionType CompletionType => ProbeCompletionType.TableField;
        public override ProbeClassifierType ClassifierType => ProbeClassifierType.TableField;
        public override string PickText => $"{_tableName}$${Name}";
        public override ServerContext ServerContext => ServerContext.Neutral;
        public override DataType DataType => DataType.Int;
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override QuickInfoLayout QuickInfo => new QuickInfoStack
        (
            new QuickInfoClassifiedString
            (
                new ProbeClassifiedString(ProbeClassifierType.TableName, _tableName),
                new ProbeClassifiedString(ProbeClassifierType.Delimiter, "$$"),
                new ProbeClassifiedString(ProbeClassifierType.TableField, Name)
            ),
            new QuickInfoDescription("A read-only property returning the field number of the column."),
            new QuickInfoAttribute("Data Type", DataType.Int.GetClassifiedString(shortVersion: false))
        );

        public override string QuickInfoTextStr
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendFormat("{0}$${1}", _tableName, Name);
                sb.AppendLine();
                sb.AppendLine("A read-only property returning the field number of the column.");
                sb.Append("Data Type: int");
                return sb.ToString();
            }
        }
    }
}
