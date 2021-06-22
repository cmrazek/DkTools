using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace DkTools
{
	public static class WpfUtil
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

		public static BitmapImage ToBitmapImage(this System.Drawing.Bitmap bmp)
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
	}
}
