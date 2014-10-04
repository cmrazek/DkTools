using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DkTools.ProbeExplorer
{
	public class RepoInfoItem : INotifyPropertyChanged
	{
		public string Interface { get; set; }
		public string PropertyName { get; set; }
		public string Text { get; set; }

		private int _indent;
		private bool _expanded;
		private bool _expandable;
		private RepoInfoItem[] _subItems;
		private object _value;
		private Visibility _visibility = Visibility.Visible;

		private static BitmapImage _expandedIcon;
		private static BitmapImage _collapsedIcon;

		private const double k_indentWidth = 20.0;

		public event PropertyChangedEventHandler PropertyChanged;

		public RepoInfoItem(string interfaceName, string propName, object value, string text, int indent)
		{
			Interface = interfaceName;
			PropertyName = propName;
			_value = value;
			Text = text;
			_indent = indent;

			if (value is MoreInfoItem)
			{
				_expandable = true;
			}
		}

		public ImageSource Icon
		{
			get
			{
				if (_expandable)
				{
					if (_expanded)
					{
						if (_expandedIcon == null) _expandedIcon = Res.ExpandMinus.ToBitmapImage();
						return _expandedIcon;
					}
					else
					{
						if (_collapsedIcon == null) _collapsedIcon = Res.ExpandPlus.ToBitmapImage();
						return _collapsedIcon;
					}
				}
				else
				{
					return null;
				}
			}
		}

		public int Indent
		{
			get { return _indent; }
			set { _indent = value; }
		}

		public Thickness IconMargin
		{
			get { return new Thickness(_indent * k_indentWidth, 0, 0, 0); }
		}

		public bool IsExpanded
		{
			get { return _expanded; }
		}

		public void OnExpanded(IEnumerable<RepoInfoItem> subItems)
		{
			_expanded = true;
			_subItems = subItems.ToArray();
			FirePropertyChanged("IsExpanded");
			FirePropertyChanged("Icon");
		}

		public void OnCollapsed()
		{
			_expanded = false;
			_subItems = null;
			FirePropertyChanged("IsExpanded");
			FirePropertyChanged("Icon");
		}

		public IEnumerable<RepoInfoItem> SubItems
		{
			get { return _subItems; }
		}

		private void FirePropertyChanged(string propName)
		{
			var ev = PropertyChanged;
			if (ev != null) ev(this, new PropertyChangedEventArgs(propName));
		}

		public object Value
		{
			get { return _value != null ? _value : "(null)"; }
		}

		public Visibility Visibility
		{
			get { return _visibility; }
			set
			{
				if (_visibility != value)
				{
					_visibility = value;
					FirePropertyChanged("Visibility");
				}
			}
		}

		public string TypeText
		{
			get
			{
				if (_value == null) return string.Empty;
				if (_value is MoreInfoItem) return (_value as MoreInfoItem).TypeText;
				return RepoInfo.GetObjectTypeText(_value);
			}
		}
	}

	public class MoreInfoItem
	{
		public string Name { get; set; }
		public object Object { get; set; }
		public string DisplayText { get; set; }

		public MoreInfoItem(string name, object obj, string displayText)
		{
			Name = name;
			Object = obj;
			DisplayText = displayText;
		}

		public override string ToString()
		{
			return DisplayText == null ? "..." : DisplayText;
		}

		public string TypeText
		{
			get
			{
				if (Object == null) return string.Empty;
				return RepoInfo.GetObjectTypeText(Object);
			}
		}
	}
}
