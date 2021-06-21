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

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is RunItem runItem)
			{
				runItem.Changed += RunItem_Changed;
			}
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is RunItem runItem)
			{
				runItem.Changed -= RunItem_Changed;
			}
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

		private void MoveRunItemUp_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(DataContext is RunItem runItem)) return;
				this.VisualUpwardsSearch<RunControl>()?.OnMoveRunItemUp(runItem);
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void MoveRunItemDown_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(DataContext is RunItem runItem)) return;
				this.VisualUpwardsSearch<RunControl>()?.OnMoveRunItemDown(runItem);
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void DeleteRunItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(DataContext is RunItem runItem)) return;
				this.VisualUpwardsSearch<RunControl>()?.OnDeleteRunItem(runItem);
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void RunItem_Changed(object sender, EventArgs e)
		{
			if (!(DataContext is RunItem runItem)) return;
			this.VisualUpwardsSearch<RunControl>()?.OnRunItemChanged(runItem);
		}
	}
}
