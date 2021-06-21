using DK.AppEnvironment;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DkTools.Run
{
	/// <summary>
	/// Interaction logic for RunItemControl.xaml
	/// </summary>
	public partial class RunItemControl : UserControl, INotifyPropertyChanged
	{
		public RunItemControl()
		{
			InitializeComponent();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void FirePropertyChanged(string propName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		private void ExpandButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				(DataContext as RunItem)?.OnOptionsButtonClicked();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void RunButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (DataContext is RunItem runItem) runItem.Run(DkEnvironment.CurrentAppSettings);
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
	}
}
