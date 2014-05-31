using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DkTools
{
	internal static class WpfUtil
	{
		public static T VisualUpwardsSearch<T>(this DependencyObject source) where T : DependencyObject
		{
			while (source != null && !(source is T))
			{
				if (source is Visual || source is Visual3D)
				{
					source = VisualTreeHelper.GetParent(source);
				}
				else
				{
					source = LogicalTreeHelper.GetParent(source);
				}
			}

			return source as T;
		}
	}
}
