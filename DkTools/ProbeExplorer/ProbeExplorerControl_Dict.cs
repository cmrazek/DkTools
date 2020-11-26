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
using DkTools.Classifier;
using DkTools.QuickInfo;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;

namespace DkTools.ProbeExplorer
{
	public sealed partial class ProbeExplorerControl
	{
		public void FocusDictFilter()
		{
			c_dictTab.IsSelected = true;
			this.UpdateLayout();
			c_dictTreeFilter.Focus();
			c_dictTreeFilter.SelectAll();
		}

		private void RefreshDictTree()
		{
			Log.Write(LogLevel.Info, "Refreshing DK Explorer dictionary view...");
			var startTime = DateTime.Now;

			c_dictTree.Items.Clear();

			var tables = DkDict.Dict.Tables.ToList();
			tables.Sort((a, b) => a.Name.CompareTo(b.Name));

			foreach (var table in tables)
			{
				c_dictTree.Items.Add(CreateTableTvi(table));
			}

			var elapsed = DateTime.Now.Subtract(startTime);
			Log.Write(LogLevel.Info, "Finished refreshing DK Explorer dictionary view. (elapsed: {0})", elapsed);
		}

		private TreeViewItem CreateStandardTvi(BitmapImage img, string title, string titleInfo, QuickInfoLayout quickInfo, bool expandable)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal
			};
			if (quickInfo != null)
			{
				panel.ToolTip = new ToolTip
				{
					Content = quickInfo.GenerateElements_WPF(),
					Background = ToolTipBackgroundBrush
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

		#region Table
		private TreeViewItem CreateTableTvi(DkDict.Table table)
		{
			var tvi = CreateStandardTvi(_tableImg, table.Name, table.Prompt, table.Definition.QuickInfo, true);
			tvi.Expanded += TableTvi_Expanded;
			tvi.Tag = table;
			tvi.MouseRightButtonDown += TableTvi_MouseRightButtonDown;

			var menu = new ContextMenu();

			var menuItem = new MenuItem();
			menuItem.Header = "Go To Definition";
			menuItem.Click += TableGoToDefinition_Click;
			menuItem.Tag = table;
			menu.Items.Add(menuItem);

			menuItem = new MenuItem();
			menuItem.Header = "Find All References";
			menuItem.Click += TableFindAllReferences_Click;
			menuItem.Tag = table;
			menu.Items.Add(menuItem);

			tvi.ContextMenu = menu;

			return tvi;
		}

		private void TableTvi_Expanded(object sender, RoutedEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				var tableNode = (sender as TreeViewItem);
				if (tableNode != null)
				{
					var table = tableNode.Tag as DkDict.Table;
					if (table != null)
					{
						tableNode.Items.Clear();

						foreach (var field in table.Columns)
						{
							if (_dictFilter.Match(field.FullName))
							{
								tableNode.Items.Add(CreateFieldTvi(field));
							}
						}

						foreach (var relind in table.RelInds.OrderBy(x => x.Name))
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

		private void TableTvi_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var tableNode = (sender as TreeViewItem);
				if (tableNode != null)
				{
					tableNode.IsSelected = true;
					e.Handled = true;
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void TableFindAllReferences_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					var menuItem = (sender as MenuItem);
					if (menuItem != null)
					{
						var table = menuItem.Tag as DkDict.Table;
						if (table != null)
						{
							Navigation.GoToDefinitionHelper.TriggerFindReferences(DkDict.Table.GetExternalRefId(table.Name), table.Name);
							e.Handled = true;
						}
					}
				}
				catch (Exception ex)
				{
					this.ShowError(ex);
				}
			});
		}

		void TableGoToDefinition_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					var menuItem = (sender as MenuItem);
					if (menuItem != null)
					{
						var table = menuItem.Tag as DkDict.Table;
						if (table != null)
						{
							Navigation.GoToDefinitionHelper.BrowseToDefinition(table.Definition);
							e.Handled = true;
						}
					}
				}
				catch (Exception ex)
				{
					this.ShowError(ex);
				}
			});
		}
		#endregion

		#region Field
		private TreeViewItem CreateFieldTvi(DkDict.Column field)
		{
			var tvi = CreateStandardTvi(_fieldImg, field.Name, field.Prompt, field.Definition.QuickInfo, true);
			tvi.Tag = field;
			tvi.Expanded += FieldTvi_Expanded;
			tvi.MouseRightButtonDown += FieldTvi_MouseRightButtonDown;

			var menu = new ContextMenu();

			var menuItem = new MenuItem();
			menuItem.Header = "Go To Definition";
			menuItem.Click += FieldGoToDefinition_Click;
			menuItem.Tag = field;
			menu.Items.Add(menuItem);

			menuItem = new MenuItem();
			menuItem.Header = "Find All References";
			menuItem.Click += FieldFindAllReferences_Click;
			menuItem.Tag = field;
			menu.Items.Add(menuItem);

			tvi.ContextMenu = menu;

			return tvi;
		}

		private void FieldTvi_Expanded(object sender, RoutedEventArgs e)
		{
			try
			{
				var fieldItem = (sender as TreeViewItem);
				if (fieldItem != null)
				{
					var field = fieldItem.Tag as DkDict.Column;
					if (field != null)
					{
						fieldItem.Items.Clear();
						CreateFieldInfoItems(fieldItem, field);
						e.Handled = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void CreateFieldInfoItems(TreeViewItem fieldItem, DkDict.Column field)
		{
			fieldItem.Items.Add(CreatePlainTextTvi("Name", field.Name));
			if (!string.IsNullOrEmpty(field.Prompt)) fieldItem.Items.Add(CreatePlainTextTvi("Prompt", field.Prompt));
			if (!string.IsNullOrEmpty(field.Comment)) fieldItem.Items.Add(CreatePlainTextTvi("Comment", field.Comment));
			fieldItem.Items.Add(CreateClassifiedStringTvi("Data Type", field.DataType.GetClassifiedString(shortVersion: false)));
		}

		private void FieldTvi_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var tvi = (sender as TreeViewItem);
				if (tvi != null)
				{
					tvi.IsSelected = true;
					e.Handled = true;
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FieldFindAllReferences_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					var menuItem = (sender as MenuItem);
					if (menuItem != null)
					{
						var field = menuItem.Tag as DkDict.Column;
						if (field != null)
						{
							Navigation.GoToDefinitionHelper.TriggerFindReferences(DkDict.Column.GetTableFieldExternalRefId(field.TableName, field.Name), field.Name);
							e.Handled = true;
						}
					}
				}
				catch (Exception ex)
				{
					this.ShowError(ex);
				}
			});
		}

		private void FieldGoToDefinition_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					var menuItem = (sender as MenuItem);
					if (menuItem != null)
					{
						var field = menuItem.Tag as DkDict.Column;
						if (field != null)
						{
							Navigation.GoToDefinitionHelper.BrowseToDefinition(field.Definition);
							e.Handled = true;
						}
					}
				}
				catch (Exception ex)
				{
					this.ShowError(ex);
				}
			});
		}
		#endregion

		#region RelInd
		private TreeViewItem CreateRelIndTreeViewItem(DkDict.RelInd relind)
		{
			var tvi = CreateStandardTvi(relind.Type == DkDict.RelIndType.Index ? _indexImg : _relationshipImg, relind.Name, relind.Prompt,
				relind.Definition.QuickInfo, true);
			tvi.Tag = relind;
			tvi.Expanded += RelIndTvi_Expanded;
			tvi.MouseRightButtonDown += RelIndTvi_MouseRightButtonDown;

			var menu = new ContextMenu();

			var menuItem = new MenuItem();
			menuItem.Header = "Go To Definition";
			menuItem.Click += RelIndGoToDefinition_Click;
			menuItem.Tag = relind;
			menu.Items.Add(menuItem);

			menuItem = new MenuItem();
			menuItem.Header = "Find All References";
			menuItem.Click += RelIndFindAllReferences_Click;
			menuItem.Tag = relind;
			menu.Items.Add(menuItem);

			tvi.ContextMenu = menu;

			return tvi;
		}

		private void RelIndTvi_Expanded(object sender, RoutedEventArgs e)
		{
			try
			{
				var relindItem = (sender as TreeViewItem);
				if (relindItem != null)
				{
					var relind = relindItem.Tag as DkDict.RelInd;
					if (relind != null)
					{
						relindItem.Items.Clear();
						CreateRelIndInfoItems(relindItem, relindItem.Tag as DkDict.RelInd);
						e.Handled = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void CreateRelIndInfoItems(TreeViewItem item, DkDict.RelInd relind)
		{
			item.Items.Add(CreatePlainTextTvi("Name", relind.Name));
			if (!string.IsNullOrWhiteSpace(relind.Prompt)) item.Items.Add(CreatePlainTextTvi("Prompt", relind.Prompt));
			if (!string.IsNullOrWhiteSpace(relind.Comment)) item.Items.Add(CreatePlainTextTvi("Comment", relind.Comment));
			if (!string.IsNullOrWhiteSpace(relind.Description)) item.Items.Add(CreatePlainTextTvi("Description", relind.Description));
			if (!string.IsNullOrWhiteSpace(relind.SortedColumnString)) item.Items.Add(CreatePlainTextTvi("Columns", relind.SortedColumnString));
		}

		private void RelIndFindAllReferences_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					var menuItem = (sender as MenuItem);
					if (menuItem != null)
					{
						var relInd = menuItem.Tag as DkDict.RelInd;
						if (relInd != null)
						{
							Navigation.GoToDefinitionHelper.TriggerFindReferences(CodeModel.Definitions.RelIndDefinition.GetExternalRefId(relInd.TableName, relInd.Name), relInd.Name);
							e.Handled = true;
						}
					}
				}
				catch (Exception ex)
				{
					this.ShowError(ex);
				}
			});
		}

		private void RelIndTvi_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var tvi = (sender as TreeViewItem);
				if (tvi != null)
				{
					tvi.IsSelected = true;
					e.Handled = true;
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void RelIndGoToDefinition_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					var menuItem = (sender as MenuItem);
					if (menuItem != null)
					{
						var relInd = menuItem.Tag as DkDict.RelInd;
						if (relInd != null)
						{
							Navigation.GoToDefinitionHelper.BrowseToDefinition(relInd.Definition);
							e.Handled = true;
						}
					}
				}
				catch (Exception ex)
				{
					this.ShowError(ex);
				}
			});
		}
		#endregion

		private TreeViewItem CreatePlainTextTvi(string label, string text)
		{
			if (text == null) text = string.Empty;

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
					Visibility = System.Windows.Visibility.Visible,
					Background = ToolTipBackgroundBrush,
					Foreground = ToolTipForegroundBrush
				}
			});

			var tvi = new TreeViewItem();
			tvi.Header = panel;
			return tvi;
		}

		private TreeViewItem CreateClassifiedStringTvi(string label, ProbeClassifiedString text)
		{
			if (text == null) text = ProbeClassifiedString.Empty;

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

			var tb = text.ToWpfTextBlock();
			tb.ToolTip = new ToolTip
			{
				Content = text,
				Visibility = System.Windows.Visibility.Visible,
				Background = ToolTipBackgroundBrush,
				Foreground = ToolTipForegroundBrush
			};
			panel.Children.Add(tb);

			var tvi = new TreeViewItem();
			tvi.Header = panel;
			return tvi;
		}

		public void FocusTable(string tableName)
		{
			c_dictTab.IsSelected = true;

			foreach (TreeViewItem tableNode in c_dictTree.Items)
			{
				var table = tableNode.Tag as DkDict.Table;
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
				var table = tableNode.Tag as DkDict.Table;
				if (table.Name == tableName)
				{
					tableNode.IsExpanded = true;
					foreach (TreeViewItem fieldNode in tableNode.Items)
					{
						var field = fieldNode.Tag as DkDict.Column;
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
				var table = tableNode.Tag as DkDict.Table;
				if (table.Name == tableName)
				{
					tableNode.IsExpanded = true;
					foreach (TreeViewItem relIndNode in tableNode.Items)
					{
						var relInd = relIndNode.Tag as DkDict.RelInd;
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

		#region Filter
		private void ApplyDictTreeFilter()
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				_dictFilter.Filter = c_dictTreeFilter.Text;
				var empty = _dictFilter.IsEmpty;

				foreach (TreeViewItem tableNode in c_dictTree.Items)
				{
					var table = tableNode.Tag as DkDict.Table;
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

						if (!showTable)
						{
							foreach (var field in table.Columns)
							{
								if (_dictFilter.Match(field.FullName))
								{
									showTable = true;
									expandTable = true;
									break;
								}
							}

							if (!showTable)
							{
								foreach (var relind in table.RelInds)
								{
									if (_dictFilter.Match(relind.Name))
									{
										showTable = true;
										expandTable = true;
									}
								}
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
			});
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
		#endregion

		#region Keyboard Input
		private StringBuilder _keyBuf;
		private DateTime _keyLastInput = DateTime.MinValue;
		private TreeViewItem _keyStartSelItem;

		private void DictTree_TextInput(object sender, TextCompositionEventArgs e)
		{
			try
			{
				var text = e.Text;
				if (string.IsNullOrEmpty(text)) return;

				// Check if the keyboard timeout has expired.
				var now = DateTime.Now;
				if (_keyBuf != null)
				{
					var elapsed = now.Subtract(_keyLastInput);
					if (elapsed.TotalMilliseconds > Constants.KeyTimeout)
					{
						_keyBuf = null;
					}
				}

				if (_keyBuf == null)
				{
					_keyBuf = new StringBuilder();
					_keyStartSelItem = c_dictTree.SelectedItem as TreeViewItem;
				}
				_keyBuf.Append(text);

				var tvi = FindKeySelectTvi(_keyBuf.ToString(), _keyStartSelItem);
				if (tvi != null) tvi.IsSelected = true;

				_keyLastInput = now;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private TreeViewItem FindKeySelectTvi(string typedText, TreeViewItem selItem)
		{
			if (selItem != null)
			{
				IEnumerable<TreeViewItem> siblings;
				var parentTvi = selItem.Parent.VisualUpwardsSearch<TreeViewItem>();
				if (parentTvi != null) siblings = parentTvi.Items.OfType<TreeViewItem>();
				else siblings = c_dictTree.Items.OfType<TreeViewItem>();

				return FindSiblingKeySelectTvi(typedText, siblings);
			}
			else
			{
				return FindSiblingKeySelectTvi(typedText, c_dictTree.Items.OfType<TreeViewItem>().ToList());
			}
		}

		private TreeViewItem FindSiblingKeySelectTvi(string typedText, IEnumerable<TreeViewItem> siblings)
		{
			foreach (var tvi in siblings)
			{
				if (tvi == null) continue;
				var tag = tvi.Tag;
				if (tag == null) continue;

				if (tag is DkDict.Table)
				{
					if ((tag as DkDict.Table).Name.StartsWith(typedText, StringComparison.CurrentCultureIgnoreCase)) return tvi;
				}
				else if (tag is DkDict.Column)
				{
					if ((tag as DkDict.Column).Name.StartsWith(typedText, StringComparison.CurrentCultureIgnoreCase)) return tvi;
				}
				else if (tag is DkDict.RelInd)
				{
					if ((tag as DkDict.RelInd).Name.StartsWith(typedText, StringComparison.CurrentCultureIgnoreCase)) return tvi;
				}

				if (tvi.IsExpanded && tvi.Items.Count > 0)
				{
					FindSiblingKeySelectTvi(typedText, tvi.Items.OfType<TreeViewItem>());
				}
			}

			return null;
		}
		#endregion
	}
}
