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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DkTools.Dict;

namespace DkTools.ProbeExplorer
{
	public partial class ProbeExplorerControl
	{
		private void RefreshDictTree()
		{
			c_dictTree.Items.Clear();

			var tables = ProbeEnvironment.Tables.ToList();
			tables.Sort((a, b) => a.Name.CompareTo(b.Name));

			foreach (var table in tables)
			{
				c_dictTree.Items.Add(CreateTableTvi(table));
			}

			var repoDict = ProbeEnvironment.ProbeDict;
			if (repoDict != null)
			{
				c_dictTree.Items.Add(CreateDictObjExtendedItem(repoDict, "Dictionary", "Dictionary Extended Information"));
			}

			//var repo = ProbeEnvironment.ProbeRepo;
			//if (repo != null)
			//{
			//	c_dictTree.Items.Add(CreateDictObjExtendedItem(repo, "Repository", "Repository Extended Information"));
			//}
		}

		private TreeViewItem CreateStandardTvi(BitmapImage img, string title, string titleInfo, UIElement quickInfoText, bool expandable)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal
			};
			if (quickInfoText != null)
			{
				panel.ToolTip = new ToolTip
				{
					Content = quickInfoText
				};
			}
			panel.Children.Add(new Image
			{
				Source = img,
				Width = k_iconWidth,
				Height = k_iconHeight,
				Margin = new Thickness(0, 0, k_iconSpacer, 0)
			});
			panel.Children.Add(new TextBlock
			{
				Name = "Title",
				Text = title,
				Margin = new Thickness(0, 0, k_textSpacer, 0)
			});
			if (!string.IsNullOrWhiteSpace(titleInfo))
			{
				panel.Children.Add(new TextBlock
				{
					Text = string.Concat("(", titleInfo.Trim(), ")"),
					Foreground = SystemColors.GrayTextBrush,
				});
			}

			var tvi = new TreeViewItem();
			tvi.Header = panel;

			if (expandable)
			{
				tvi.IsExpanded = false;
				tvi.Items.Add(new TreeViewItem());	// Add an empty node so the '+' sign is displayed.
			}

			return tvi;
		}

		private TreeViewItem CreateTableTvi(DictTable table)
		{
			var tvi = CreateStandardTvi(_tableImg, table.Name, table.Prompt, table.BaseDefinition.QuickInfoTextWpf, true);
			tvi.Expanded += TableTvi_Expanded;
			tvi.Tag = table;
			return tvi;
		}

		private void TableTvi_Expanded(object sender, RoutedEventArgs e)
		{
			try
			{
				var tableNode = (sender as TreeViewItem);
				if (tableNode != null)
				{
					var table = tableNode.Tag as DictTable;
					if (table != null)
					{
						tableNode.Items.Clear();

						tableNode.Items.Add(CreateDictObjExtendedItem(table.RepoTable, string.Concat("Table: ", table.Name)));

						foreach (var field in table.Fields)
						{
							if (_dictFilter.Match(field.Name) || _dictFilter.Match(field.Prompt))
							{
								tableNode.Items.Add(CreateFieldTvi(field));
							}
						}

						foreach (var relind in table.RelInds)
						{
							if (_dictFilter.Match(relind.Name))
							{
								tableNode.Items.Add(CreateRelIndTreeViewItem(relind));
							}
						}

						e.Handled = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private TreeViewItem CreateFieldTvi(DictField field)
		{
			var tvi = CreateStandardTvi(_fieldImg, field.Name, field.Prompt, field.Definition.QuickInfoTextWpf, true);
			tvi.Tag = field;
			tvi.Expanded += FieldTvi_Expanded;
			return tvi;
		}

		private void FieldTvi_Expanded(object sender, RoutedEventArgs e)
		{
			try
			{
				var fieldItem = (sender as TreeViewItem);
				if (fieldItem != null)
				{
					var field = fieldItem.Tag as DictField;
					if (field != null)
					{
						fieldItem.Items.Clear();
						CreateFieldInfoItems(fieldItem, field);
						fieldItem.Items.Add(CreateDictObjExtendedItem(field.RepoColumn, string.Concat("Column: ", field.Name)));
						e.Handled = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void CreateFieldInfoItems(TreeViewItem fieldItem, DictField field)
		{
			fieldItem.Items.Add(CreateInfoTvi("Name", field.Name));
			if (!string.IsNullOrEmpty(field.Prompt)) fieldItem.Items.Add(CreateInfoTvi("Prompt", field.Prompt));
			if (!string.IsNullOrEmpty(field.Comment)) fieldItem.Items.Add(CreateInfoTvi("Comment", field.Comment));
			fieldItem.Items.Add(CreateInfoTvi("Data Type", field.DataType.Name));
		}

		private TreeViewItem CreateRelIndTreeViewItem(DictRelInd relind)
		{
			var tvi = CreateStandardTvi(relind.Type == DictRelIndType.Index ? _indexImg : _relationshipImg, relind.Name, relind.Prompt, relind.Definition.QuickInfoTextWpf, true);
			tvi.Tag = relind;
			tvi.Expanded += RelIndTvi_Expanded;
			return tvi;
		}

		private void RelIndTvi_Expanded(object sender, RoutedEventArgs e)
		{
			try
			{
				var relindItem = (sender as TreeViewItem);
				if (relindItem != null)
				{
					var relind = relindItem.Tag as DictRelInd;
					if (relind != null)
					{
						relindItem.Items.Clear();
						CreateRelIndInfoItems(relindItem, relindItem.Tag as DictRelInd);
						relindItem.Items.Add(CreateDictObjExtendedItem(relind.RepoObj, string.Concat("Index/Relationship: ", relind.Name)));
						e.Handled = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void CreateRelIndInfoItems(TreeViewItem item, DictRelInd relind)
		{
			item.Items.Add(CreateInfoTvi("Name", relind.Name));
			if (!string.IsNullOrWhiteSpace(relind.Prompt)) item.Items.Add(CreateInfoTvi("Prompt", relind.Prompt));
			if (!string.IsNullOrWhiteSpace(relind.Comment)) item.Items.Add(CreateInfoTvi("Comment", relind.Comment));
			if (!string.IsNullOrWhiteSpace(relind.Description)) item.Items.Add(CreateInfoTvi("Description", relind.Description));
			if (!string.IsNullOrWhiteSpace(relind.Columns)) item.Items.Add(CreateInfoTvi("Columns", relind.Columns));
		}

		private TreeViewItem CreateInfoTvi(string label, string text)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal
			};
			panel.Children.Add(new TextBlock
			{
				Text = label + ": ",
				MinWidth = k_fieldInfoLabelWidth,
				FontWeight = FontWeights.Bold,
				Margin = new Thickness(0, 0, k_textSpacer, 0)
			});
			panel.Children.Add(new TextBlock
			{
				Text = text,
				ToolTip = new ToolTip
				{
					Content = text,
					Visibility = System.Windows.Visibility.Visible
				}
			});

			var tvi = new TreeViewItem();
			tvi.Header = panel;
			return tvi;
		}

		private TreeViewItem CreateInfoTvi(string label, object value)
		{
			var text = value == null ? string.Empty : value.ToString();
			return CreateInfoTvi(label, value);
		}

		public void FocusTable(string tableName)
		{
			c_dictTab.IsSelected = true;

			foreach (TreeViewItem tableNode in c_dictTree.Items)
			{
				var table = tableNode.Tag as DictTable;
				if (table.Name == tableName)
				{
					tableNode.IsSelected = true;
					tableNode.BringIntoView();
					tableNode.Focus();
					return;
				}
			}

			// If we got here, then the table wasn't found.
			c_dictTree.Focus();
		}

		public void FocusTableField(string tableName, string fieldName)
		{
			c_dictTab.IsSelected = true;
			this.UpdateLayout();

			foreach (TreeViewItem tableNode in c_dictTree.Items)
			{
				var table = tableNode.Tag as DictTable;
				if (table.Name == tableName)
				{
					tableNode.IsExpanded = true;
					foreach (TreeViewItem fieldNode in tableNode.Items)
					{
						var field = fieldNode.Tag as DictField;
						if (field != null && field.Name == fieldName)
						{
							fieldNode.IsSelected = true;
							fieldNode.BringIntoView();
							fieldNode.Focus();
							return;
						}
					}
				}
			}

			// If we got here, then the table/field wasn't found.
			c_dictTree.Focus();
		}

		public void FocusTableRelInd(string tableName, string relIndName)
		{
			c_dictTab.IsSelected = true;
			this.UpdateLayout();

			foreach (TreeViewItem tableNode in c_dictTree.Items)
			{
				var table = tableNode.Tag as DictTable;
				if (table.Name == tableName)
				{
					tableNode.IsExpanded = true;
					foreach (TreeViewItem relIndNode in tableNode.Items)
					{
						var relInd = relIndNode.Tag as DictRelInd;
						if (relInd != null && relInd.Name == relIndName)
						{
							relIndNode.IsSelected = true;
							relIndNode.BringIntoView();
							relIndNode.Focus();
							return;
						}
					}
				}
			}

			// If we got here, then the table/field wasn't found.
			c_dictTree.Focus();
		}

		private void ApplyDictTreeFilter()
		{
			if (!c_dictTree.Dispatcher.CheckAccess())
			{
				c_dictTree.Dispatcher.BeginInvoke(new Action(() => { ApplyDictTreeFilter(); }));
				return;
			}

			_dictFilter.Filter = c_dictTreeFilter.Text;
			var empty = _dictFilter.IsEmpty;

			foreach (TreeViewItem tableNode in c_dictTree.Items)
			{
				var table = tableNode.Tag as DictTable;
				if (table == null) continue;

				if (empty)
				{
					tableNode.Visibility = System.Windows.Visibility.Visible;
					tableNode.IsExpanded = false;
				}
				else
				{
					var showTable = false;
					var expandTable = false;
					if (_dictFilter.Match(table.Name) || _dictFilter.Match(table.Prompt))
					{
						showTable = true;
					}

					foreach (var field in table.Fields)
					{
						if (_dictFilter.Match(field.Name) || _dictFilter.Match(field.Prompt))
						{
							showTable = true;
							expandTable = true;
						}
					}

					tableNode.Visibility = showTable ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

					if (expandTable)
					{
						if (tableNode.IsExpanded) tableNode.IsExpanded = false;
						tableNode.IsExpanded = true;
					}
				}
			}

			var selItem = c_dictTree.SelectedItem as TreeViewItem;
			if (selItem != null) selItem.BringIntoView();
		}

		private void DictTreeFilter_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				_dictTreeDeferrer.OnActivity();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		void DictTreeDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			try
			{
				ApplyDictTreeFilter();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void DictTreeFilterClear_MouseUp_1(object sender, MouseButtonEventArgs e)
		{
			try
			{
				c_dictTreeFilter.Text = string.Empty;
				c_dictTreeFilter.Focus();
				_dictTreeDeferrer.ExecuteNowIfPending();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void DictTreeFilterClear_MouseEnter_1(object sender, MouseEventArgs e)
		{
			try
			{
				c_dictTreeFilterClear.Opacity = 1.0;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void DictTreeFilterClear_MouseLeave_1(object sender, MouseEventArgs e)
		{
			try
			{
				c_dictTreeFilterClear.Opacity = .5;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void DictTreeFilter_KeyDown_1(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Enter)
				{
					_dictTreeDeferrer.ExecuteNowIfPending();
					e.Handled = true;
				}
				else if (e.Key == Key.Escape)
				{
					c_dictTreeFilter.Text = string.Empty;
					_dictTreeDeferrer.ExecuteNowIfPending();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private class ExtendedItemInfo
		{
			public object DictObject { get; set; }
			public string WindowTitle { get; set; }
		}

		private TreeViewItem CreateDictObjExtendedItem(object obj, string windowTitle, string headerText = "extended information")
		{
			var tvi = CreateStandardTvi(_extendedImg, string.Empty, headerText, null, false);
			tvi.Tag = new ExtendedItemInfo { DictObject = obj, WindowTitle = windowTitle };
			tvi.MouseDoubleClick += ExtendedItem_MouseDoubleClick;
			return tvi;
		}

		private void ExtendedItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var item = sender as TreeViewItem;
				if (item == null) return;

				var info = item.Tag as ExtendedItemInfo;
				if (info == null) return;

				var dlg = new RepoInfoWindow(info.DictObject);
				dlg.Title = info.WindowTitle;
				dlg.Owner = System.Windows.Application.Current.MainWindow;
				dlg.Show();

				e.Handled = true;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
	}
}
