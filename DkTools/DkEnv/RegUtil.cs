using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DkTools.DkEnv
{
	internal static class RegUtil
	{
		public static string GetString(this RegistryKey key, string name, string defValue = "")
		{
			try
			{
				var obj = key.GetValue(name);
				if (obj == null) return defValue;
				return obj.ToString();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
				return defValue;
			}
		}
	}
}
