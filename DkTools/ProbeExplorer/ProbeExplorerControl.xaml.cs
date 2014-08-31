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
using System.Windows.Navigation;
using System.Windows.Shapes;
using IO = System.IO;

namespace DkTools.ProbeExplorer
{
	/// <summary>
	/// Interaction logic for ProbeExplorerControl.xaml
	/// </summary>
	public partial class ProbeExplorerControl : UserControl
	{
		#region Variables
		private BitmapImage _folderImg;
		private BitmapImage _fileImg;
		private List<string> _fileList;
		#endregion

		#region Constants
		private const int k_iconWidth = 16;
		private const int k_iconHeight = 16;
		private const int k_iconSpacer = 2;
		#endregion

		#region Events
		public event EventHandler<OpenFileEventArgs> FileOpenRequested;
		public class OpenFileEventArgs : EventArgs
		{
			public string FileName { get; set; }
		}
		#endregion

		#region Construction
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			
		}

		public ProbeExplorerControl()
		{
			InitializeComponent();

			_folderImg = BitmapToBitmapImage(Res.FolderImg);
			_fileImg = BitmapToBitmapImage(Res.FileImg);

			ProbeEnvironment.AppChanged += new EventHandler(Probe_AppChanged);
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				RefreshAppCombo();
				RefreshFileTree();

				UpdateForFilter();

#if DEBUG
				c_appLabel.MouseRightButtonUp += new MouseButtonEventHandler(AppLabel_MouseRightButtonUp);
#endif
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private BitmapImage BitmapToBitmapImage(System.Drawing.Bitmap bmp)
		{
			BitmapImage bmpImg;

			var memStream = new System.IO.MemoryStream();
			bmp.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);
			memStream.Position = 0;

			bmpImg = new BitmapImage();
			bmpImg.BeginInit();
			bmpImg.StreamSource = memStream;
			bmpImg.EndInit();

			return bmpImg;
		}
		#endregion

		#region App Combo
		private void RefreshAppCombo()
		{
			c_appCombo.Items.Clear();

			if (ProbeEnvironment.Initialized)
			{
				var currentApp = ProbeEnvironment.CurrentApp;
				string selectApp = null;

				foreach (var appName in ProbeEnvironment.AppNames)
				{
					c_appCombo.Items.Add(appName);
					if (appName == currentApp) selectApp = appName;
				}

				if (selectApp != null) c_appCombo.SelectedItem = selectApp;
			}
		}

		private void Probe_AppChanged(object sender, EventArgs e)
		{
			try
			{
				if (!c_appCombo.Dispatcher.CheckAccess())
				{
					c_appCombo.Dispatcher.BeginInvoke(new Action(() => { Probe_AppChanged(sender, e); }));
					return;
				}

				var currentApp = ProbeEnvironment.CurrentApp;
				c_appCombo.SelectedItem = (from a in c_appCombo.Items.Cast<string>() where a == currentApp select a).FirstOrDefault();

				RefreshForEnvironment();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void AppCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				var selectedApp = c_appCombo.SelectedItem as string;
				if (!string.IsNullOrEmpty(selectedApp) && ProbeEnvironment.Initialized)
				{
					ProbeEnvironment.CurrentApp = selectedApp;
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				RefreshForEnvironment();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void RefreshForEnvironment()
		{
			ProbeEnvironment.Reload(true);
			RefreshAppCombo();
			RefreshFileTree();
		}
		#endregion

		#region File Tree
		private class FileTreeNode
		{
			public bool dir;
			public string path;
		}

		private void RefreshFileTree()
		{
			c_fileTree.Items.Clear();

			if (ProbeEnvironment.Initialized)
			{
				foreach (var dir in ProbeEnvironment.SourceDirs)
				{
					c_fileTree.Items.Add(CreateDirTreeViewItem(dir, true));
				}
			}

			RefreshFileList();
		}

		void CreateFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var menuItem = sender as MenuItem;
				if (menuItem != null)
				{
					var tag = menuItem.Tag as FileTreeNode;
					if (tag != null && tag.dir)
					{
						e.Handled = true;
						var dlg = new CreateFileDialog();
						dlg.Directory = tag.path;
						if (dlg.ShowDialog() == true)
						{
							CreateNewFile(System.IO.Path.Combine(dlg.Directory, dlg.FileName));
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private TreeViewItem CreateDirTreeViewItem(string dirPath, bool root)
		{
			var tag = new FileTreeNode { path = dirPath, dir = true };

			var node = new TreeViewItem();
			node.Header = CreateTreeViewHeader(root ? dirPath : IO.Path.GetFileName(dirPath), _folderImg);
			node.Tag = tag;
			node.IsExpanded = false;
			node.ContextMenu = new ContextMenu();

			var menuItem = new MenuItem();
			menuItem.Header = "Create File";
			menuItem.Tag = tag;
			menuItem.Click += new RoutedEventHandler(CreateFileMenuItem_Click);
			node.ContextMenu.Items.Add(menuItem);

			menuItem = new MenuItem();
			menuItem.Header = "Find in Files";
			menuItem.Tag = tag;
			menuItem.Click += new RoutedEventHandler(FindInFilesMenuItem_Click);
			node.ContextMenu.Items.Add(menuItem);

			menuItem = new MenuItem();
			menuItem.Header = "Explore Folder";
			menuItem.Tag = tag;
			menuItem.Click += ExploreDirMenuItem_Click;
			node.ContextMenu.Items.Add(menuItem);

			node.Expanded += DirNode_Expanded;

			node.Items.Add(new TreeViewItem());	// Add an empty node so the '+' sign is displayed.

			return node;
		}

		private TreeViewItem CreateFileTreeViewItem(string fileName)
		{
			var tag = new FileTreeNode { path = fileName, dir = false };

			var node = new TreeViewItem();
			node.Header = CreateTreeViewHeader(IO.Path.GetFileName(fileName), _fileImg);
			node.Tag = tag;
			node.MouseDoubleClick += FileNode_MouseDoubleClick;
			node.KeyDown += FileNode_KeyDown;
			node.ContextMenu = new ContextMenu();

			var menuItem = new MenuItem();
			menuItem.Header = "Explore File";
			menuItem.Tag = tag;
			menuItem.Click += ExploreFileMenuItem_Click;
			node.ContextMenu.Items.Add(menuItem);

			return node;
		}

		private object CreateTreeViewHeader(string text, BitmapImage img)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal
			};

			panel.Children.Add(new Image
			{
				Source = img,
				Width = k_iconWidth,
				Height = k_iconHeight,
				Margin = new Thickness(0, 0, k_iconSpacer, 0)
			});
			panel.Children.Add(new TextBlock { Text = text });

			return panel;
		}

		void DirNode_Expanded(object sender, RoutedEventArgs e)
		{
			try
			{
				var parentNode = (sender as TreeViewItem);
				if (parentNode != null)
				{
					e.Handled = true;

					parentNode.Items.Clear();
					var tag = parentNode.Tag as FileTreeNode;
					if (tag != null)
					{
						var parentDir = tag.path;
						if (!string.IsNullOrEmpty(parentDir))
						{
							foreach (var dir in System.IO.Directory.GetDirectories(parentDir))
							{
								var node = CreateDirTreeViewItem(dir, false);
								parentNode.Items.Add(node);
							}

							if (ProbeToolsPackage.Instance.ProbeExplorerOptions.ShowFilesInTree)
							{
								foreach (var file in System.IO.Directory.GetFiles(parentDir))
								{
									parentNode.Items.Add(CreateFileTreeViewItem(file));
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception when expanding directory node.");
			}
		}

		void FileNode_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var fileNode = sender as TreeViewItem;
				if (fileNode != null)
				{
					
					var tag = fileNode.Tag as FileTreeNode;
					if (tag != null)
					{
						e.Handled = true;
						ActivateFile(tag.path);
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		void FileNode_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Enter)
				{
					var fileNode = sender as TreeViewItem;
					if (fileNode != null)
					{
						var tag = fileNode.Tag as FileTreeNode;
						if (tag != null)
						{
							e.Handled = true;
							ActivateFile(tag.path);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		private void ActivateFile(string fileName)
		{
			if (!string.IsNullOrEmpty(fileName))
			{
				var pathName = fileName;

				var ev = this.FileOpenRequested;
				if (ev != null) ev(this, new OpenFileEventArgs { FileName = pathName });
			}
		}

		private void FileTreeRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				RefreshForEnvironment();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FileTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var source = e.OriginalSource as DependencyObject;
				if (source != null)
				{
					var item = source.VisualUpwardsSearch<TreeViewItem>();
					if (item != null)
					{
						item.Focus();
						e.Handled = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FindInFilesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var menuItem = sender as MenuItem;
				if (menuItem != null)
				{
					var tag = menuItem.Tag as FileTreeNode;
					if (tag != null && tag.dir)
					{
						e.Handled = true;
						Shell.ShowFindInFiles(new string[] { tag.path });
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ExploreDirMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var menuItem = sender as MenuItem;
				if (menuItem != null)
				{
					var tag = menuItem.Tag as FileTreeNode;
					if (tag != null)
					{
						e.Handled = true;
						FileUtil.OpenExplorer(tag.path);
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		void ExploreFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var menuItem = sender as MenuItem;
				if (menuItem != null)
				{
					var tag = menuItem.Tag as FileTreeNode;
					if (tag != null)
					{
						e.Handled = true;
						FileUtil.OpenExplorer(tag.path);
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
		#endregion

		#region File List
		private void RefreshFileList()
		{
			if (ProbeEnvironment.Initialized)
			{
				_fileList = ProbeEnvironment.GetAllSourceIncludeFiles().ToList();

				if (!string.IsNullOrEmpty(c_fileFilterTextBox.Text)) c_fileFilterTextBox.Text = string.Empty;
			}
		}

		private void UpdateForFilter()
		{
			var filterText = c_fileFilterTextBox.Text;
			if (string.IsNullOrWhiteSpace(filterText))
			{
				// Display the tree
				c_fileTree.Visibility = System.Windows.Visibility.Visible;
				c_fileList.Visibility = System.Windows.Visibility.Hidden;
			}
			else
			{
				// Display the filtered list
				c_fileList.Items.Clear();

				var numItems = 0;

				if (_fileList != null)
				{
					var filter = new TextFilter(filterText);
					foreach (var file in _fileList)
					{
						if (filter.Match(System.IO.Path.GetFileName(file)))
						{
							if (numItems >= Constants.FileListMaxItems)
							{
								c_fileList.Items.Add(CreateFileListOverflowItem());
								break;
							}

							c_fileList.Items.Add(CreateFileListItem(file));
							numItems++;
						}
					}
				}

				c_fileTree.Visibility = System.Windows.Visibility.Hidden;
				c_fileList.Visibility = System.Windows.Visibility.Visible;
			}
		}

		private ListBoxItem CreateFileListItem(string fileName)
		{
			var item = new ListBoxItem();

			var panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			panel.Children.Add(new Image
			{
				Source = _fileImg,
				Width = k_iconWidth,
				Height = k_iconHeight,
				Margin = new Thickness(0, 0, k_iconSpacer, 0)
			});
			panel.Children.Add(new TextBlock
			{
				Text = System.IO.Path.GetDirectoryName(fileName) + System.IO.Path.DirectorySeparatorChar,
				Foreground = SystemColors.GrayTextBrush
			});
			panel.Children.Add(new TextBlock
			{
				Text = System.IO.Path.GetFileName(fileName)
			});
			item.Content = panel;

			item.Tag = fileName;

			item.MouseDoubleClick += new MouseButtonEventHandler(FileItem_MouseDoubleClick);
			item.KeyDown += new KeyEventHandler(FileItem_KeyDown);

			return item;
		}

		private ListBoxItem CreateFileListOverflowItem()
		{
			var item = new ListBoxItem();

			item.Content = new TextBlock
			{
				Text = string.Format(Constants.FileListMaxItemsExceeded, Constants.FileListMaxItems),
				Foreground = SystemColors.GrayTextBrush
			};

			return item;
		}

		private void FileList_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Enter)
				{
					var selItem = c_fileList.SelectedItem as ListBoxItem;
					if (selItem != null)
				{	
						e.Handled = true;

						ActivateFile(selItem.Tag as string);
					}
				}
				else if (e.Key == Key.Up && c_fileList.SelectedIndex == 0)
				{
					e.Handled = true;
					c_fileFilterTextBox.Focus();
					c_fileFilterTextBox.SelectAll();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		void FileItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var fileNode = sender as ListBoxItem;
				if (fileNode != null)
				{
					e.Handled = true;
					ActivateFile(fileNode.Tag as string);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		void FileItem_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Enter)
				{
					var fileNode = sender as TreeViewItem;
					if (fileNode != null)
					{
						e.Handled = true;
						ActivateFile(fileNode.Tag as string);
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		private void SelectFileInTree(string fileName)
		{
			foreach (var node in c_fileTree.Items.Cast<TreeViewItem>())
			{
				if (SelectFileInTree_SearchDirNode(node, fileName)) break;
			}
		}

		private bool SelectFileInTree_SearchDirNode(TreeViewItem node, string fileName)
		{
			var tag = node.Tag as FileTreeNode;
			if (tag == null || tag.dir != true) return false;

			if (!fileName.StartsWith(tag.path, StringComparison.OrdinalIgnoreCase)) return false;

			if (!node.IsExpanded) node.ExpandSubtree();

			foreach (var subNode in node.Items.Cast<TreeViewItem>())
			{
				tag = subNode.Tag as FileTreeNode;
				if (tag == null) continue;

				if (tag.dir)
				{
					if (SelectFileInTree_SearchDirNode(subNode, fileName)) return true;
				}
				else
				{
					if (tag.path.Equals(fileName, StringComparison.OrdinalIgnoreCase))
					{
						subNode.IsSelected = true;
						return true;
					}
				}
			}

			return false;
		}
		#endregion

		#region File Filter Text Box
		private void FileFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				UpdateForFilter();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FileFilterTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Escape)
				{
					e.Handled = true;
					c_fileFilterTextBox.Text = string.Empty;
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FileFilterTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Down)
				{
					if (c_fileList.Visibility == System.Windows.Visibility.Visible)
					{
						e.Handled = true;

						// Select the first visible item in the list.
						var firstItem = (from i in c_fileList.Items.Cast<ListBoxItem>()
										 where i.Visibility == System.Windows.Visibility.Visible
										 select i).FirstOrDefault();
						if (firstItem != null)
						{
							firstItem.IsSelected = true;
							firstItem.Focus();
						}
						else c_fileList.Focus();
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		public void FocusFileFilter()
		{
			c_fileFilterTextBox.Focus();
			c_fileFilterTextBox.SelectAll();
		}

		private void ClearFileFilterImg_MouseEnter(object sender, MouseEventArgs e)
		{
			c_clearFileFilterImg.Opacity = 1.0;
		}

		private void ClearFileFilterImg_MouseLeave(object sender, MouseEventArgs e)
		{
			c_clearFileFilterImg.Opacity = 0.5;
		}

		private void ClearFileFilterImg_MouseUp(object sender, MouseButtonEventArgs e)
		{
			c_fileFilterTextBox.Text = string.Empty;
			c_fileFilterTextBox.Focus();
		}
		#endregion

		#region Debug
#if DEBUG
		void AppLabel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var menu = new ContextMenu();

				var menuItem = new MenuItem();
				menuItem.Header = "Show Code Model";
				menuItem.Click += ShowCodeModelDump_Click;
				menu.Items.Add(menuItem);

				menuItem = new MenuItem();
				menuItem.Header = "Show Definitions";
				menuItem.Click += ShowDefinitions_Click;
				menu.Items.Add(menuItem);

				menuItem = new MenuItem();
				menuItem.Header = "Show Preprocessor";
				menuItem.Click += ShowPreprocessor_Click;
				menu.Items.Add(menuItem);

				menuItem = new MenuItem();
				menuItem.Header = "Show Preprocessor Model";
				menuItem.Click += ShowPreprocessorModel_Click;
				menu.Items.Add(menuItem);

				menuItem = new MenuItem();
				menuItem.Header = "Show Preprocessor Segments";
				menuItem.Click += ShowPreprocessorSegments_Click;
				menu.Items.Add(menuItem);

				menu.PlacementTarget = c_appLabel;
				menu.IsOpen = true;
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ShowCodeModelDump_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Commands.DebugCommands.ShowCodeModelDump();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ShowDefinitions_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Commands.DebugCommands.ShowDefinitions();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ShowPreprocessor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Commands.DebugCommands.ShowPreprocessor();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ShowPreprocessorModel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Commands.DebugCommands.ShowPreprocessorModel();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void ShowPreprocessorSegments_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Commands.DebugCommands.ShowPreprocessorSegments();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
#endif
		#endregion

		private void CreateNewFile(string fileName)
		{
			if (System.IO.File.Exists(fileName))
			{
				this.ShowError("This file already exists.");
				Shell.OpenDocument(fileName);
				return;
			}

			var fileHeaderText = DkTools.Tagging.Tagger.GetFileHeaderText(fileName);
			System.IO.File.WriteAllText(fileName, fileHeaderText, Encoding.ASCII);

			Shell.OpenDocument(fileName);

			RefreshFileTree();
			SelectFileInTree(fileName);
		}
	}
}
