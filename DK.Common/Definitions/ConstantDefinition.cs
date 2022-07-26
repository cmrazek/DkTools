using DK.AppEnvironment;
using DK.Code;
using DK.Syntax;

namespace DK.Definitions
{
    public class ConstantDefinition : Definition
    {
        private string _text;

        public ConstantDefinition(string name, FilePosition filePos, string text)
            : base(name, filePos, CreateExternalRefId(name, filePos.FileName))
        {
            _text = text;
        }

        public string Text => _text;
        public override bool CompletionVisible => true;
        public override ProbeCompletionType CompletionType => ProbeCompletionType.Constant;
        public override ProbeClassifierType ClassifierType => ProbeClassifierType.Constant;
        public override string QuickInfoTextStr => _text;
        public override QuickInfoLayout QuickInfo => new QuickInfoText(ProbeClassifierType.Constant, _text);
        public override string PickText => QuickInfoTextStr;
        public override bool ArgumentsRequired => false;
        public override bool CanRead => true;
        public override ServerContext ServerContext => ServerContext.Neutral;

        private static string CreateExternalRefId(string name, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                return string.Concat("const:", name, ":", PathUtil.GetFileName(fileName));
            }
            else
            {
                return string.Concat("const:", name);
            }
        }
    }
}
