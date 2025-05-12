using DK.Code;
using DK.Modeling;
using DK.Syntax;
using System;
using System.Collections.Generic;

namespace DK.Definitions
{
    internal class AfsMethodDefinition : Definition
    {
        private string _tableName;
        private string _methodName;
        private FunctionSignature _signature;

        public AfsMethodDefinition(string tableName, string methodName, FunctionSignature signature)
            : base(methodName, FilePosition.Empty, null)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _methodName = tableName ?? throw new ArgumentNullException(nameof(methodName));
            _signature = signature ?? throw new ArgumentNullException(nameof(signature));
        }

        public override string ToString() => _signature.PrettySignature;

        public override bool CompletionVisible => true;
        public override ProbeCompletionType CompletionType => ProbeCompletionType.Function;
        public override ProbeClassifierType ClassifierType => ProbeClassifierType.Function;
        public override string PickText => _signature.PrettySignature;
        public override ServerContext ServerContext => _signature.ServerContext;
        public override DataType DataType => _signature.ReturnDataType;
        public override bool ArgumentsRequired => true;
        public override IEnumerable<ArgumentDescriptor> Arguments => _signature.Arguments;
        public override FunctionSignature ArgumentsSignature => _signature;
        public override FunctionSignature Signature => _signature;
        public override bool HasVariableArgumentCount => false;
        public override bool CanRead => true;

        public override string QuickInfoTextStr
        {
            get
            {
                if (string.IsNullOrEmpty(_signature.Description)) return _signature.PrettySignature;
                return string.Concat(_signature.PrettySignature, "\r\n\r\n", _signature.Description);
            }
        }

        public override QuickInfoLayout QuickInfo => new QuickInfoStack(
            new QuickInfoClassifiedString(_signature.ClassifiedString),
            string.IsNullOrWhiteSpace(_signature.Description) ? null : new QuickInfoDescription(_signature.Description)
        );
    }
}
