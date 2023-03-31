using Microsoft.Win32;
using System;

namespace DkTools.AppEnvironment
{
    static class RegistryKeyUtil
    {
        public static string GetString(this RegistryKey key, string name, string defValue = "")
        {
            try
            {
                var obj = key.GetValue(name);
                if (obj == null) return defValue;
                return obj.ToString();
            }
            catch (Exception)
            {
                return defValue;
            }
        }
    }
}
