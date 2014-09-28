using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DkTools.CodeModel.Definitions;

namespace DkTools.Navigation
{
	/// <summary>
	/// Interaction logic for DefinitionPickerWindow.xaml
	/// </summary>
	public partial class DefinitionPickerWindow : Window
	{
		private List<CodeModel.Definitions.Definition> _defs = new List<CodeModel.Definitions.Definition>();
		internal CodeModel.Definitions.Definition SelectedItem { get; private set; }

		public DefinitionPickerWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			c_defList.ItemsSource = _defs;

			if (c_defList.Items.Count > 0) c_defList.SelectedIndex = 0;
			c_defList.Focus();
		}

		internal IEnumerable<CodeModel.Definitions.Definition> Definitions
		{
			get { return _defs; }
			set
			{
				_defs.Clear();
				_defs.AddRange(value);
			}
		}

		private bool CheckItemActivated()
		{
			var selItem = c_defList.SelectedItem as CodeModel.Definitions.Definition;
			SelectedItem = selItem;
			return selItem != null;
		}

		private void c_okButton_Click(object sender, RoutedEventArgs e)
		{
			if (CheckItemActivated())
			{
				DialogResult = true;
				Close();
			}
		}

		private void c_cancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void c_defList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (CheckItemActivated())
			{
				DialogResult = true;
				Close();
			}
		}
	}
}
