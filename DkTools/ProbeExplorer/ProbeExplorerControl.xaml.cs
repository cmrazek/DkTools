using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Diagnostics;
using DkTools.CodeModeling;
using DkTools.Navigation;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IO = System.IO;
using VsText=Microsoft.VisualStudio.Text;
using VsTextEditor=Microsoft.VisualStudio.Text.Editor;

namespace DkTools.ProbeExplorer
{
	/// <summary>
	/// Interaction logic for ProbeExplorerControl.xaml
	/// </summary>
	public partial class ProbeExplorerControl : UserControl, INotifyPropertyChanged
	{
		#region Variables
		private BackgroundDeferrer _dictTreeDeferrer = new BackgroundDeferrer();
		private TextFilter _dictFilter = new TextFilter();

		private BitmapImage _folderImg;
		private BitmapImage _tableImg;
		private BitmapImage _fieldImg;
		private BitmapImage _indexImg;
		private BitmapImage _relationshipImg;

		private static BitmapImage _functionImg;

		private bool _suppressAppChange = false;
		#endregion

		#region Constants
		private const int k_iconWidth = 16;
		private const int k_iconHeight = 16;
		private const int k_iconSpacer = 2;
		private const int k_minFieldWidth = 150;
		private const int k_fieldInfoLabelWidth = 150;
		private const int k_textSpacer = 4;
		#endregion

		#region Events
		public event EventHandler<OpenFileEventArgs> FileOpenRequested;
		public class OpenFileEventArgs : EventArgs
		{
			public string FileName { get; set; }
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void FirePropertyChanged(string propName)
		{
			var ev = PropertyChanged;
			if (ev != null) ev(this, new PropertyChangedEventArgs(propName));
		}
		#endregion

		#region Construction
		public ProbeExplorerControl()
		{
			DataContext = this;
			InitializeComponent();

			_folderImg = Res.FolderImg.ToBitmapImage();
			_tableImg = Res.TableImg.ToBitmapImage();
			_fieldImg = Res.FieldImg.ToBitmapImage();
			_indexImg = Res.IndexImg.ToBitmapImage();
			_relationshipImg = Res.RelationshipImg.ToBitmapImage();
			if (_functionImg == null) _functionImg = Res.FunctionImg.ToBitmapImage();

			GlobalEvents.AppChanged += new EventHandler(Probe_AppChanged);
			_dictTreeDeferrer.Idle += DictTreeDeferrer_Idle;
		}

		~ProbeExplorerControl()
		{
			if (_dictTreeDeferrer != null)
			{
				_dictTreeDeferrer.Idle -= DictTreeDeferrer_Idle;
				_dictTreeDeferrer.Dispose();
				_dictTreeDeferrer = null;
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			VSTheme.ThemeChanged += VSTheme_ThemeChanged;
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				var appSettings = DkEnvironment.CurrentAppSettings;
				RefreshAppCombo(appSettings);
				RefreshFileTree(appSettings);
				RefreshDictTree();
				c_run.AppSettings = appSettings;

				UpdateForFileFilter();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		public void OnDocumentActivated(VsTextEditor.IWpfTextView view)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			RefreshFunctionList(view);
		}
		#endregion

		#region App Combo
		private void RefreshAppCombo(DkAppSettings appSettings)
		{
			_suppressAppChange = true;
			try
			{
				c_appCombo.Items.Clear();

				if (appSettings.Initialized)
				{
					var currentApp = appSettings.AppName;
					string selectApp = null;

					foreach (var appName in appSettings.AllAppNames)
					{
						c_appCombo.Items.Add(appName);
						if (appName == currentApp) selectApp = appName;
					}

					if (selectApp != null) c_appCombo.SelectedItem = selectApp;
				}
			}
			finally
			{
				_suppressAppChange = false;
			}
		}

		private void Probe_AppChanged(object sender, EventArgs e)
		{
			try
			{
				ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					var appSettings = DkEnvironment.CurrentAppSettings;
					c_appCombo.SelectedItem = (from a in c_appCombo.Items.Cast<string>() where a == appSettings.AppName select a).FirstOrDefault();

					RefreshAppCombo(appSettings);
					RefreshFileTree(appSettings);
					RefreshDictTree();
					c_run.AppSettings = appSettings;
					GlobalEvents.OnRefreshAllDocumentsRequired();
				});
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
				if (_suppressAppChange) return;

				var selectedApp = c_appCombo.SelectedItem as string;
				if (!string.IsNullOrEmpty(selectedApp) && DkEnvironment.CurrentAppSettings.Initialized)
				{
					try
					{
						DkEnvironment.CurrentAppSettings.TryUpdateDefaultCurrentApp();
					}
					catch (System.Security.SecurityException ex)
					{
						var options = ProbeToolsPackage.Instance.ErrorSuppressionOptions;
						if (!options.DkAppChangeAdminFailure)
						{
							var msg = "The system-wide default DK application can't be changed because access was denied. To resolve this problem, run Visual Studio as an administrator.";
							var dlg = new ErrorDialog(msg, ex.ToString())
							{
								ShowUserSuppress = true,
								Owner = System.Windows.Application.Current.MainWindow
							};
							dlg.ShowDialog();

							if (options.DkAppChangeAdminFailure != dlg.UserSuppress)
							{
								options.DkAppChangeAdminFailure = dlg.UserSuppress;
								options.SaveSettingsToStorage();
							}
						}
					}

					DkEnvironment.Reload(selectedApp);
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
				DkEnvironment.Reload(null);
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
		#endregion

		#region File Tree
		private class FileTreeNode
		{
			public bool dir;
			public string path;
		}

		private void RefreshFileTree(DkAppSettings appSettings)
		{
			c_fileTree.Items.Clear();

			if (appSettings.Initialized)
			{
				foreach (var dir in appSettings.SourceDirs)
				{
					c_fileTree.Items.Add(CreateDirTreeViewItem(dir, true));
				}
			}

			if (!string.IsNullOrEmpty(c_fileFilterTextBox.Text))
			{
				c_fileFilterTextBox.Text = string.Empty;
			}
		}

		void CreateFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (sender is MenuItem menuItem
					&& menuItem.Tag is FileTreeNode tag && tag.dir)
				{
					var appSettings = DkEnvironment.CurrentAppSettings;
					if (appSettings == null) return;

					e.Handled = true;
					var dlg = new CreateFileDialog();
					dlg.Owner = System.Windows.Application.Current.MainWindow;
					dlg.Directory = tag.path;
					if (dlg.ShowDialog() == true)
					{
						CreateNewFile(System.IO.Path.Combine(dlg.Directory, dlg.FileName), appSettings);
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

			var displayText = root ? dirPath : IO.Path.GetFileName(dirPath);
			if (string.IsNullOrEmpty(displayText)) Log.Write(LogLevel.Warning, "File tree display text is blank for path '{0}' (root: {1})", dirPath, root);

			var node = new TreeViewItem();
			node.Header = CreateFileTreeViewHeader(displayText, _folderImg);
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
			node.Header = CreateFileTreeViewHeader(IO.Path.GetFileName(fileName), FileImage);
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

		private object CreateFileTreeViewHeader(string text, BitmapImage img)
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
				Log.Error(ex, "Exception when expanding directory node.");
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
				DkEnvironment.Reload(null);
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
				ThreadHelper.ThrowIfNotOnUIThread();

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
		private void UpdateForFileFilter()
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

				var hiddenExt = GetHiddenExtensions();
				var appSettings = DkEnvironment.CurrentAppSettings;

				var filter = new TextFilter(filterText);
				foreach (var file in appSettings.SourceAndIncludeFiles)
				{
					if (hiddenExt.Contains(System.IO.Path.GetExtension(file).ToLower())) continue;

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

				c_fileTree.Visibility = System.Windows.Visibility.Hidden;
				c_fileList.Visibility = System.Windows.Visibility.Visible;
			}
		}

		private HashSet<string> GetHiddenExtensions()
		{
			var hiddenExt = ProbeToolsPackage.Instance.ProbeExplorerOptions.HiddenExtensions;
			if (string.IsNullOrWhiteSpace(hiddenExt)) hiddenExt = Constants.DefaultHiddenExtensions;
			var hiddenExtList = new HashSet<string>(from e in StringHelper.ParseWordList(hiddenExt)
													select e.StartsWith(".") ? e.ToLower() : string.Concat(".", e.ToLower()));
			return hiddenExtList;
		}

		private ListBoxItem CreateFileListItem(string fileName)
		{
			var item = new ListBoxItem();

			var panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			panel.Children.Add(new Image
			{
				Source = FileImage,
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
				UpdateForFileFilter();
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
			c_filesTab.IsSelected = true;
			this.UpdateLayout();
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

		private void CreateNewFile(string fileName, DkAppSettings appSettings)
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

			appSettings.ReloadFilesList();
			RefreshFileTree(appSettings);
			SelectFileInTree(fileName);
		}

		#region Function List
		private VsTextEditor.IWpfTextView _activeView;
		private VsText.ITextSnapshot _activeSnapshot;
		private FunctionListItem[] _activeFunctions;

		private void RefreshFunctionList(VsTextEditor.IWpfTextView view)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!c_functionTab.IsSelected || view == null) return;

			string className = null;
			var fileName = VsTextUtil.TryGetDocumentFileName(view.TextBuffer);
			if (!string.IsNullOrEmpty(fileName)) className = FileContextHelper.GetClassNameFromFileName(fileName);

			if (view != null)
			{
				var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(view.TextBuffer);
				if (fileStore != null)
				{
					var snapshot = view.TextSnapshot;
					if (_activeView != view || _activeSnapshot != snapshot)
					{
						_activeView = view;
						_activeSnapshot = snapshot;

						var appSettings = DkEnvironment.CurrentAppSettings;
						_activeFunctions = (from f in fileStore.GetFunctionDropDownList(appSettings, fileName, snapshot)
											orderby f.Name.ToLower()
											select new FunctionListItem(f)).ToArray();
						ApplyFunctionFilter();
						c_functionList.ItemsSource = _activeFunctions;
					}
					return;
				}
			}

			_activeView = null;
			_activeSnapshot = null;
			_activeFunctions = null;
			c_functionList.ItemsSource = null;
		}

		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					if (e.AddedItems.Count == 1 && e.AddedItems[0] == c_functionTab)
					{
						RefreshFunctionList(Shell.ActiveView);
					}
				}
				catch (Exception ex)
				{
					this.ShowError(ex);
				}
			});
		}

		internal class FunctionListItem : INotifyPropertyChanged
		{
			private FunctionDefinition _def;
			private CodeSpan _span;
			private bool _visible = true;

			public event PropertyChangedEventHandler PropertyChanged;

			internal FunctionListItem(FunctionDropDownItem func)
			{
				_span = func.Span;
				_def = func.Definition;
			}

			public FunctionDefinition Definition => _def;
			public string Name => _def.Name;
			public CodeSpan Span => _span;

			public bool Visible
			{
				get { return _visible; }
				set
				{
					if (_visible != value)
					{
						_visible = value;
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Visible)));
					}
				}
			}
		}

		public static BitmapImage FunctionImage
		{
			get
			{
				if (_functionImg == null) _functionImg = Res.FunctionImg.ToBitmapImage();
				return _functionImg;
			}
		}

		private void ApplyFunctionFilter()
		{
			if (_activeFunctions == null) return;

			var filter = new TextFilter(c_functionFilter.Text);
			foreach (var func in _activeFunctions)
			{
				func.Visible = filter.Match(func.Name);
			}
		}

		private void ActivateFunction(FunctionListItem func, bool setDocFocus)
		{
			if (_activeView == null || _activeSnapshot == null || func == null) return;
			if (!setDocFocus && !ProbeToolsPackage.Instance.ProbeExplorerOptions.AutoScrollToFunctions) return;

			if (setDocFocus)
			{
				_activeView.VisualElement.Focus();
			}

			var nav = Navigation.Navigator.TryGetForView(_activeView);
			if (nav != null)
			{
				nav.MoveTo(new VsText.SnapshotPoint(_activeSnapshot, func.Span.Start));
			}
		}

		public void FocusFunctionFilter()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			RefreshFunctionList(Shell.ActiveView);

			c_functionTab.IsSelected = true;
			this.UpdateLayout();
			c_functionFilter.Focus();
			c_functionFilter.SelectAll();
		}

		private void FunctionList_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Up && c_functionList.SelectedIndex == 0)
				{
					e.Handled = true;
					c_functionFilter.Focus();
					c_functionFilter.SelectAll();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FunctionListBoxItem_Selected(object sender, RoutedEventArgs e)
		{
			try
			{
				var lbi = sender as ListBoxItem;
				if (lbi != null)
				{
					var func = lbi.Content as FunctionListItem;
					if (func != null)
					{
						ActivateFunction(func, false);
					}
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FunctionListBoxItem_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				var lbi = sender as ListBoxItem;
				if (lbi != null)
				{
					var func = lbi.Content as FunctionListItem;
					if (func != null)
					{
						ActivateFunction(func, true);
					}
				}
			}
		}

		private void FunctionListBoxItem_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			try
			{
				if (c_functionList.SelectedItem != null)
				{
					e.Handled = true;
					ActivateFunction(c_functionList.SelectedItem as FunctionListItem, true);
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FunctionFilter_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FunctionFilter_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Escape)
				{
					e.Handled = true;
					c_functionFilter.Text = string.Empty;
				}
				else if (e.Key == Key.Down)
				{
					var topFunc = c_functionList.Items.Cast<FunctionListItem>().FirstOrDefault(i => i.Visible);
					if (topFunc != null)
					{
						var topItem = c_functionList.ItemContainerGenerator.ContainerFromItem(topFunc) as ListBoxItem;
						if (topItem != null)
						{
							e.Handled = true;
							topItem.IsSelected = true;
							c_functionList.ScrollIntoView(topItem);
							topItem.Focus();
							return;
						}
					}

					c_functionList.Focus();
				}
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FunctionFilter_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				ApplyFunctionFilter();
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}

		private void FunctionFilterClear_MouseEnter(object sender, MouseEventArgs e)
		{
			c_functionFilterClear.Opacity = 1.0;
		}

		private void FunctionFilterClear_MouseLeave(object sender, MouseEventArgs e)
		{
			c_functionFilterClear.Opacity = 0.5;
		}

		private void FunctionFilterClear_MouseUp(object sender, MouseButtonEventArgs e)
		{
			c_functionFilter.Text = string.Empty;
			c_functionFilter.Focus();
		}

		private void FunctionFindAllReferences_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				var menuItem = e.OriginalSource as MenuItem;
				if (menuItem == null) return;

				var funcItem = menuItem.DataContext as FunctionListItem;
				if (funcItem == null) return;

				GoToDefinitionHelper.TriggerFindReferences(funcItem.Definition.ExternalRefId, funcItem.Definition.FullName);
			}
			catch (Exception ex)
			{
				this.ShowError(ex);
			}
		}
		#endregion

		#region Run Tab
		public void FocusRunTab()
		{
			c_runTab.IsSelected = true;
			this.UpdateLayout();
		}
		#endregion

		#region Theming
		void VSTheme_ThemeChanged(object sender, EventArgs e)
		{
			try
			{
				_clearImage = null;
				_fileImage = null;
				_toolTipBackgroundBrush = null;
				_toolTipForegroundBrush = null;

				FirePropertyChanged(NameOf_ClearImage);

				UpdateForFileFilter();
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		private BitmapImage _clearImage;
		public const string NameOf_ClearImage = "ClearImage";
		public BitmapImage ClearImage
		{
			get
			{
				if (_clearImage == null)
				{
					switch (VSTheme.CurrentTheme)
					{
						case VSThemeMode.Dark:
							_clearImage = Res.ClearIcon_Dark.ToBitmapImage();
							break;
						default:
							_clearImage = Res.ClearIcon_Light.ToBitmapImage();
							break;
					}
				}
				return _clearImage;
			}
		}

		private BitmapImage _fileImage;
		public const string NameOf_FileImage = "FileImage";
		public BitmapImage FileImage
		{
			get
			{
				if (_fileImage == null)
				{
					_fileImage = VSTheme.CurrentTheme == VSThemeMode.Dark ? Res.FileImg_Dark.ToBitmapImage() : Res.FileImg_Light.ToBitmapImage();
				}
				return _fileImage;
			}
		}

		private Brush _toolTipBackgroundBrush;
		public Brush ToolTipBackgroundBrush
		{
			get
			{
				if (_toolTipBackgroundBrush == null)
				{
					switch (VSTheme.CurrentTheme)
					{
						case VSThemeMode.Dark:
							_toolTipBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x42, 0x42, 0x45));
							break;
						default:
							_toolTipBackgroundBrush = Brushes.White;
							break;
					}
				}
				return _toolTipBackgroundBrush;
			}
		}

		private Brush _toolTipForegroundBrush;
		public Brush ToolTipForegroundBrush
		{
			get
			{
				if (_toolTipForegroundBrush == null)
				{
					switch (VSTheme.CurrentTheme)
					{
						case VSThemeMode.Dark:
							_toolTipForegroundBrush = Brushes.White;
							break;
						default:
							_toolTipForegroundBrush = Brushes.Black;
							break;
					}
				}
				return _toolTipForegroundBrush;
			}
		}
		#endregion
	}
}
