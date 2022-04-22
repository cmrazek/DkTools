using System;
using System.ComponentModel;
using System.Windows;

namespace DkTools.ProbeExplorer
{
	/// <summary>
	/// Interaction logic for CreateFileDialog.xaml
	/// </summary>
	public partial class CreateFileDialog : Window, INotifyPropertyChanged
	{
		private string _directory;
		private string _fileName;

		public CreateFileDialog()
		{
			InitializeComponent();
			DataContext = this;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				FileNameTextBox.Focus();
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
			get => _directory;
			set
			{
				if (_directory != value)
                {
					_directory = value;
					FirePropertyChanged(nameof(Directory));
                }
			}
		}

		public string FileName
		{
			get => _fileName;
			set
            {
				if (_fileName != value)
                {
					_fileName = value;
					FirePropertyChanged(nameof(FileName));
                }
            }
		}

		public string FileNameLengthText => $"({_fileName?.Length ?? 0} char{(_fileName?.Length == 1 ? "" : "s")})";

		public bool OkButtonEnabled => ValidateForm();

		private bool ValidateForm()
		{
			if (string.IsNullOrWhiteSpace(_fileName)) return false;
			if (_fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) return false;

			return true;
		}

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		private void FirePropertyChanged(string propName)
        {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

			switch (propName)
            {
				case nameof(FileName):
					FirePropertyChanged(nameof(FileNameLengthText));
					FirePropertyChanged(nameof(OkButtonEnabled));
					break;
            }
        }
		#endregion
	}
}
