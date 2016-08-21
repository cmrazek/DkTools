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
						using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\11.0\General"))
						{
							var obj = key.GetValue("CurrentTheme");
							if (obj == null)
							{
								_mode = VSThemeMode.Light;
							}
							else
							{
								var guid = obj.ToString();
								if (guid.Equals("de3dbbcd-f642-433c-8353-8f1df4370aba", StringComparison.OrdinalIgnoreCase))
								{
									_mode = VSThemeMode.Light;
								}
								else if (guid.Equals("1ded0138-47ce-435e-84ef-9ec1f439b749", StringComparison.OrdinalIgnoreCase))
								{
									_mode = VSThemeMode.Dark;
								}
								else if (guid.Equals("a4d6a176-b948-4b29-8c66-53c97a1ed7d0", StringComparison.OrdinalIgnoreCase))
								{
									_mode = VSThemeMode.Light;
								}
								else
								{
									Log.Debug("Unknown theme: {0}", guid);
									_mode = VSThemeMode.Light;
								}
							}
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

			CodeModel.Definitions.Definition.OnThemeChanged();
		}
	}

	internal enum VSThemeMode
	{
		Light,
		Dark
	}
}
