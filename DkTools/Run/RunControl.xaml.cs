using DK.AppEnvironment;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
	/// Interaction logic for RunControl.xaml
	/// </summary>
	public partial class RunControl : UserControl, INotifyPropertyChanged
	{
		public RunControl()
		{
			DataContext = this;
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				InitializeRunItems();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		#region App Settings
		private DkAppSettings _appSettings;

		public DkAppSettings AppSettings
		{
			get => _appSettings;
			set
			{
				if (_appSettings != value)
				{
					_appSettings = value;
					PopulateRunItems();
				}
			}
		}
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		private void FirePropertyChanged(string propName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}
		#endregion

		#region Run Items
		private RunItemCatalogue _catalogue;

		private RunItem[] _runItems = new RunItem[0];

		public IEnumerable<RunItem> RunItems => _runItems;

		public void InitializeRunItems()
		{
			_catalogue = new RunItemCatalogue();
			_catalogue.Load();
			PopulateRunItems();
		}

		private void PopulateRunItems()
		{
			if (_catalogue == null) return;

			if (_appSettings != null)
			{
				_runItems = _catalogue.GetRunItemsForApp(_appSettings).ToArray();
			}
			else
			{
				_runItems = new RunItem[0];
			}

			FirePropertyChanged(nameof(RunItems));
		}
		#endregion
	}
}
