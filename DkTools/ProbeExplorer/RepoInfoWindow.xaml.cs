using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace DkTools.ProbeExplorer
{
	/// <summary>
	/// Interaction logic for RepoInfoWindow.xaml
	/// </summary>
	public partial class RepoInfoWindow : Window, INotifyPropertyChanged
	{
		private object _repoObj;
		//private List<RepoInfoItem> _items;	TODO: remove
		private ObservableCollection<RepoInfoItem> _items;
		//private Dict.Dict _dict;		TODO: remove

		public event PropertyChangedEventHandler PropertyChanged;

		internal RepoInfoWindow(object repoObj)
		{
			_repoObj = repoObj;
			_items = new ObservableCollection<RepoInfoItem>(RepoInfo.GenerateInfoItems(_repoObj, 0));

			InitializeComponent();
			DataContext = this;
		}

		public IEnumerable<RepoInfoItem> RepoInfoItems
		{
			get { return _items; }
		}

		public object RepoInfoObject
		{
			get { return _repoObj; }
			// TODO: remove
			//set
			//{
			//	if (_repoObj != value)
			//	{
			//		_repoObj = value;
			//		_items.Clear();
			//		foreach (var item in GenerateInfoItems(_repoObj, 0)) _items.Add(item);
			//		//FirePropertyChanged("RepoInfoItems");
			//	}
			//}
		}

		private void FirePropertyChanged(string propName)
		{
			var ev = PropertyChanged;
			if (ev != null) ev(this, new PropertyChangedEventArgs(propName));
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var item = sender as ListViewItem;
				if (item == null) return;

				var info = item.Content as RepoInfoItem;
				if (info == null) return;

				if (!info.IsExpanded)
				{
					var insertIndex = -1;
					var index = 0;
					foreach (var i in _items)
					{
						if (i == info)
						{
							insertIndex = index;
							break;
						}
						index++;
					}
					if (index < 0) return;

					var moreInfo = info.Value as MoreInfoItem;
					if (moreInfo != null)
					{
						var subItems = RepoInfo.GenerateInfoItems(moreInfo.Object, info.Indent + 1).ToArray();
						foreach (var subItem in subItems)
						{
							_items.Insert(++insertIndex, subItem);
						}

						info.OnExpanded(subItems);
						ForceColumnResize();
					}
				}
				else
				{
					foreach (var subItem in info.SubItems) _items.Remove(subItem);
					info.OnCollapsed();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ForceColumnResize()
		{
			var gridView = c_mainListView.View as GridView;
			if (gridView != null)
			{
				foreach (var col in gridView.Columns)
				{
					col.Width = 0;
					col.Width = double.NaN;
				}
			}
		}

		private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				ApplyTextFilter();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ApplyTextFilter()
		{
			var filter = new TextFilter(c_filterTextBox.Text);

			foreach (var info in _items)
			{
				if (filter.Match(info.Interface) || filter.Match(info.PropertyName))
				{
					info.Visibility = System.Windows.Visibility.Visible;
				}
				else
				{
					info.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}

		private void FilterClear_MouseUp_1(object sender, MouseButtonEventArgs e)
		{
			try
			{
				c_filterTextBox.Text = string.Empty;
				c_filterTextBox.Focus();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FilterClear_MouseEnter_1(object sender, MouseEventArgs e)
		{
			try
			{
				c_filterTextBox.Opacity = 1.0;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FilterClear_MouseLeave_1(object sender, MouseEventArgs e)
		{
			try
			{
				c_filterTextBox.Opacity = .5;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
	}
}
