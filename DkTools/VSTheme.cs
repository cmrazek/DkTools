using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools
{
	internal static class VSTheme
	{
		private static VSThemeMode? _mode;

		public static event EventHandler ThemeChanged;

		public static VSThemeMode CurrentTheme
		{
			get
			{
				if (!_mode.HasValue)
				{
					try
					{
						var color = Microsoft.VisualStudio.PlatformUI.VSColorTheme.GetThemedColor(Microsoft.VisualStudio.PlatformUI.EnvironmentColors.SystemWindowTextColorKey);
						if ((color.R + color.G + color.B) / 3 > 192)
						{
							_mode = VSThemeMode.Dark;
						}
						else
						{
							_mode = VSThemeMode.Light;
						}
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Exception when attempting to get current VS theme. Defaulting to 'light'.");
						_mode = VSThemeMode.Light;
					}
				}

				return _mode.Value;
			}
		}

		public static void OnThemeChanged()
		{
			_mode = null;
			Log.Debug("Detected theme change. New theme: {0}", CurrentTheme);

			var ev = ThemeChanged;
			if (ev != null) ev(null, EventArgs.Empty);
		}
	}

	internal enum VSThemeMode
	{
		Light,
		Dark
	}
}
