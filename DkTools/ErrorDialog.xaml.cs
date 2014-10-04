using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DkTools
{
	/// <summary>
	/// Interaction logic for ErrorDialog.xaml
	/// </summary>
	public partial class ErrorDialog : Window
	{
		private string _message;
		private string _details;
		private bool _userSuppress;
		private bool _showUserSuppress;

		public ErrorDialog(string message = null, string details = null)
		{
			_message = message;
			_details = details;

			InitializeComponent();

			DataContext = this;
		}

		public string Message
		{
			get { return _message; }
			set { _message = value; }
		}

		public string Details
		{
			get { return _details; }
			set { _details = value; }
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				DialogResult = true;
				Close();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		public Visibility DetailsTabVisibility
		{
			get { return !string.IsNullOrEmpty(_details) ? Visibility.Visible : Visibility.Hidden; }
		}

		public bool UserSuppress
		{
			get { return _userSuppress; }
			set { _userSuppress = value; }
		}

		public bool ShowUserSuppress
		{
			get { return _showUserSuppress; }
			set { _showUserSuppress = value; }
		}

		public Visibility DontShowAgainVisibility
		{
			get { return _showUserSuppress ? Visibility.Visible : Visibility.Hidden; }
		}
	}
}
