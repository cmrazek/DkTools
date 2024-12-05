using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows;

namespace DkTools.LanguageSvc
{
    internal class ProbeTextViewMargin : IWpfTextViewMargin
    {
        private FrameworkElement _element;
        private IWpfTextView _textView;

        public const string Name = "DkMargin";

        public ProbeTextViewMargin(IWpfTextView textView)
        {
            _textView = textView;
            _element = new FunctionDropDown(textView);
        }

        public void Dispose() { }

        public FrameworkElement VisualElement { get => _element; }

        public double MarginSize { get => 100.0; }

        public bool Enabled { get => true; }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            if (string.Compare(marginName, Name, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return this;
            }
            return null;
        }
    }
}
