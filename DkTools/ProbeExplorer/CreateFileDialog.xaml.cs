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
using DkTools;

namespace DkTools.ProbeExplorer
{
	/// <summary>
	/// Interaction logic for CreateFileDialog.xaml
	/// </summary>
	public partial class CreateFileDialog : Window
	{
		public CreateFileDialog()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				EnableControls(this, EventArgs.Empty);

				c_fileNameTextBox.Focus();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void c_okButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (ValidateForm())
				{
					DialogResult = true;
					Close();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void c_cancelButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				DialogResult = false;
				Close();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		public string Directory
		{
			get { return c_dirTextBox.Text; }
			set { c_dirTextBox.Text = value; }
		}

		public string FileName
		{
			get { return c_fileNameTextBox.Text; }
			set { c_fileNameTextBox.Text = value; }
		}

		private void EnableControls(object sender, EventArgs e)
		{
			try
			{
				c_okButton.IsEnabled = ValidateForm();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private bool ValidateForm()
		{
			var fileName = c_fileNameTextBox.Text;

			if (string.IsNullOrWhiteSpace(fileName)) return false;
			if (fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) return false;

			return true;
		}

		private void FileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			EnableControls(sender, e);
		}
	}
}
