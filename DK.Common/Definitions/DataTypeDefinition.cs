using DK.AppEnvironment;
using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;

namespace DK.Definitions
{
    public class DataTypeDefinition : Definition
    {
        private DataType _dataType;

        public DataTypeDefinition(string name, FilePosition filePos, DataType dataType)
            : base(name, filePos, CreateExternalRefId(name, filePos.FileName))
        {
#if DEBUG
            if (dataType == null) throw new ArgumentNullException("dataType");
#endif
            _dataType = dataType;
        }

        public DataTypeDefinition(string name, DataType dataType)
            : base(name, FilePosition.Empty, CreateExternalRefId(name, null))
        {
            _dataType = dataType;
        }

        public override DataType DataType => _dataType;
        public override bool CompletionVisible => true;
        public override ProbeCompletionType CompletionType => ProbeCompletionType.DataType;
        public override ProbeClassifierType ClassifierType => ProbeClassifierType.DataType;
        public override string QuickInfoTextStr => !string.IsNullOrEmpty(_dataType.InfoText) ? _dataType.InfoText : _dataType.Name;
        public override QuickInfoLayout QuickInfo => new QuickInfoClassifiedString(_dataType.GetClassifiedString(shortVersion: false));
        public override string PickText => QuickInfoTextStr;
        public override bool ArgumentsRequired => false;
        public override ServerContext ServerContext => ServerContext.Neutral;

        private static string CreateExternalRefId(string name, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                return string.Concat("typedef:", name, ":", PathUtil.GetFileName(fileName));
            }
            else
            {
                return string.Concat("typedef:", name);
            }
        }
    }
}
