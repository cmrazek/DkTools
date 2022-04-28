using System;

namespace DK.Code
{
    public struct CodeItem
    {
        private CodeType _type;
        private CodeSpan _span;
        private string _text;

        public CodeItem(CodeType type, CodeSpan span, string text)
        {
            _type = type;
            _span = span;
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public CodeType Type => _type;
        public CodeSpan Span => _span;
        public string Text => _text;

        public CodeItem AdjustOffset(int delta) => new CodeItem(_type, _span + delta, _text);

        public override string ToString() => $"{_type} '{_text}' {_span}";

        public bool IsEmpty => _type == default && _span == default && _text == default;
    }
}
