using DkTools.CodeModeling;
using DkTools.Helpers;
using DkTools.Navigation;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DkTools.LanguageSvc
{
    /// <summary>
    /// Interaction logic for FunctionDropDown.xaml
    /// </summary>
    public partial class FunctionDropDown : UserControl
    {
        private IWpfTextView _textView;
        private DkTextBufferNotifier _textBufferNotifier;
        private List<FunctionDropDownItem> _functions = new List<FunctionDropDownItem>();
        private bool _suppressJumpToFunction = false;

        public FunctionDropDown(IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _textView = textView ?? throw new ArgumentNullException(nameof(textView));

            _textBufferNotifier = DkTextBufferNotifier.GetOrCreate(_textView.TextBuffer);
            _textBufferNotifier.NewModelAvailable += TextBufferNotifier_NewModelAvailable;

            _textView.Caret.PositionChanged += Caret_PositionChanged;

            DataContext = this;
            InitializeComponent();
        }

        private void TextBufferNotifier_NewModelAvailable(object sender, DkTextBufferNotifier.NewModelAvailableEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var functions = e.CodeModel.FileStore.GetFunctionDropDownList(e.CodeModel)
                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!functions.HasSameContent(_functions))
                {
                    Functions = functions;
                }
            });
        }

        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var caretPos = _textView.Caret.Position.BufferPosition.Position;
                var insideFunc = _functions.Where(f => f.EntireFunctionSpan.Contains(caretPos)).FirstOrDefault();
                if (insideFunc == null) return;

                var selectedItem = FunctionCombo.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    var selectedFunc = selectedItem.Tag as FunctionDropDownItem;
                    if (selectedFunc != null && selectedFunc.Name == insideFunc.Name)
                    {
                        // This is already selected in the ComboBox. No change required
                        return;
                    }
                }

                var itemToSelect = FunctionCombo.Items.Cast<ComboBoxItem>()
                    .Where(i => (i.Tag as FunctionDropDownItem).Name == insideFunc.Name)
                    .FirstOrDefault();
                if (itemToSelect == null) return;

                _suppressJumpToFunction = true;
                FunctionCombo.SelectedItem = itemToSelect;
                _suppressJumpToFunction = false;
            });
        }

        public IEnumerable<FunctionDropDownItem> Functions
        {
            get => _functions;
            set
            {
                _functions = (value ?? throw new ArgumentNullException()).ToList();

                FunctionCombo.Items.Clear();
                foreach (var f in _functions)
                {
                    var cbi = new ComboBoxItem();
                    cbi.Content = f.ClassifiedSignatureElement;
                    cbi.Visibility = Visibility.Visible;
                    cbi.Tag = f;
                    FunctionCombo.Items.Add(cbi);
                }
            }
        }

        private void FunctionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressJumpToFunction) return;

            var cbi = FunctionCombo.SelectedItem as ComboBoxItem;
            if (cbi == null) return;

            var function = cbi.Tag as FunctionDropDownItem;
            if (function == null) return;

            var nav = Navigator.TryGetForView(_textView);
            if (nav == null) return;

            FilterTextBox.Text = string.Empty;

            nav.MoveTo(new SnapshotPoint(_textView.TextSnapshot, function.EntireFunctionSpan.Start));
            _textView.VisualElement.Focus();
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = FilterTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                foreach (var cbi in FunctionCombo.Items.Cast<ComboBoxItem>())
                {
                    cbi.Visibility = Visibility.Visible;
                }
                FunctionCombo.IsDropDownOpen = false;
            }
            else
            {
                var filter = new TextFilter(searchText);
                foreach (var cbi in FunctionCombo.Items.Cast<ComboBoxItem>())
                {
                    var func = cbi.Tag as FunctionDropDownItem;
                    cbi.Visibility = filter.Match(func.Name) ? Visibility.Visible : Visibility.Collapsed;
                }
                if (!FunctionCombo.IsDropDownOpen) FunctionCombo.IsDropDownOpen = true;
                if (!FilterTextBox.IsFocused) FilterTextBox.Focus();
            }
        }

        private void FilterTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Down:
                    FunctionCombo.Focus();
                    FunctionCombo.IsDropDownOpen = true;
                    e.Handled = true;
                    break;
                case Key.Escape:
                    FilterTextBox.Text = string.Empty;
                    e.Handled = true;
                    break;
            }
        }
    }
}
