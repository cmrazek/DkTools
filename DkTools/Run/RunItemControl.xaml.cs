using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					if (DataContext is RunItem runItem)
					{
						RunThread.Run(new RunItem[] { runItem }, ProbeToolsPackage.Instance.App.Settings);
					}
				});
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

		private void BrowseFilePath_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(DataContext is RunItem runItem)) return;

				var dlg = new OpenFileDialog();
				if (!string.IsNullOrEmpty(runItem.FilePath) && System.IO.File.Exists(runItem.FilePath))
				{
					dlg.FileName = runItem.FilePath;
					dlg.InitialDirectory = System.IO.Path.GetDirectoryName(runItem.FilePath);
				}
				dlg.Multiselect = false;

				if (dlg.ShowDialog() == true)
				{
					runItem.FilePath = dlg.FileName;
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void BrowseWorkingDir_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(DataContext is RunItem runItem)) return;

				var dlg = new System.Windows.Forms.FolderBrowserDialog();
				if (!string.IsNullOrEmpty(runItem.WorkingDirectory) && System.IO.Directory.Exists(runItem.WorkingDirectory))
				{
					dlg.SelectedPath = runItem.WorkingDirectory;
				}
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					runItem.WorkingDirectory = dlg.SelectedPath;
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
	}
}
