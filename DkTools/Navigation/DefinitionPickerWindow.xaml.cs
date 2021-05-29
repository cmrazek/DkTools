using DK.Definitions;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace DkTools.Navigation
{
	/// <summary>
	/// Interaction logic for DefinitionPickerWindow.xaml
	/// </summary>
	public partial class DefinitionPickerWindow : Window
	{
		private List<Definition> _defs = new List<Definition>();
		internal Definition SelectedItem { get; private set; }

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

		internal IEnumerable<Definition> Definitions
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
			var selItem = c_defList.SelectedItem as Definition;
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
