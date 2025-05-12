using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DK.Definitions
{
    internal class AfsPropertyDefinition : Definition
    {
        private string _tableName;
        private string _name;
        private DataType _dataType;
        private string _devDesc;
        private string _quickInfoStr;
        private QuickInfoLayout _quickInfo;
        private bool _readOnly;

        public AfsPropertyDefinition(string tableName, string name, DataType dataType, string devDesc, bool readOnly)
            : base(name, FilePosition.Empty, null)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName)); 
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            _devDesc = devDesc; // optional
            _readOnly = readOnly;
        }

        public override bool CompletionVisible => true;
        public override ProbeCompletionType CompletionType => ProbeCompletionType.TableField;
        public override ProbeClassifierType ClassifierType => ProbeClassifierType.TableField;
        public override string PickText => $"{_tableName}.{_name}";
        public override ServerContext ServerContext => ServerContext.Neutral;
        public override bool RequiresParent(string curClassName) => true;
        public override bool CanRead => true;
        public override bool CanWrite => !_readOnly;
        public override DataType DataType => _dataType;

        public override string QuickInfoTextStr
        {
            get
            {
                if (_quickInfoStr == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(_tableName);
                    sb.Append('$');
                    sb.Append(_name);
                    sb.AppendLine();
                    sb.Append("Data Type: ");
                    sb.Append(_dataType.InfoText);
                    if (!string.IsNullOrEmpty(_devDesc))
                    {
                        sb.AppendLine();
                        sb.Append("Description: ");
                        sb.Append(_devDesc);
                    }
                    _quickInfoStr = sb.ToString();
                }
                return _quickInfoStr;
            }
        }

        public override QuickInfoLayout QuickInfo
        {
            get
            {
                if (_quickInfo == null)
                {
                    _quickInfo = new QuickInfoStack(
                        new QuickInfoClassifiedString(
                            new ProbeClassifiedString(ProbeClassifierType.TableName, _tableName),
                            new ProbeClassifiedString(ProbeClassifierType.Delimiter, "$"),
                            new ProbeClassifiedString(ProbeClassifierType.TableField, _name)
                        ),
                        new QuickInfoAttribute("Data Type", _dataType.GetClassifiedString(shortVersion: false)),
                        string.IsNullOrEmpty(_devDesc) ? null : new QuickInfoAttribute("Description", _devDesc)
                    );
                }
                return _quickInfo;
            }
        }
    }
}
