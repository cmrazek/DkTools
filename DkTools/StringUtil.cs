using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DkTools
{
	internal static class StringUtil
	{
		public static readonly string[] EmptyStringArray = new string[0];

		public static string Combine(this IEnumerable<string> list)
		{
			var sb = new StringBuilder();
			foreach (var str in list)
			{
				if (str != null) sb.Append(str);
			}
			return sb.ToString();
		}

		public static string Combine(this IEnumerable<string> list, string delim)
		{
			if (string.IsNullOrEmpty(delim)) return list.Combine();

			var sb = new StringBuilder();
			var first = true;
			foreach (var str in list)
			{
				if (first) first = false;
				else sb.Append(delim);
				if (str != null) sb.Append(str);
			}
			return sb.ToString();
		}

		public static IEnumerable<T> Delim<T>(this IEnumerable<T> list, T delim)
		{
			var first = true;
			foreach (var item in list)
			{
				if (first) first = false;
				else yield return delim;
				yield return item;
			}
		}

		public static bool EqualsI(this string pathA, string pathB)
		{
			return string.Equals(pathA, pathB, StringComparison.OrdinalIgnoreCase);
		}
	}
}
