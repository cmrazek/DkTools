using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools
{
	static class JsonHelpers
	{
		public static bool ToBool(this JToken json, bool defaultValue)
		{
			if (json == null) return defaultValue;
			if (json.Type != JTokenType.Boolean) return defaultValue;
			return json.ToObject<bool>();
		}

		public static int ToInt(this JToken json, int min, int max, int defaultValue)
		{
			if (json == null) return defaultValue;

			if (json.Type == JTokenType.Integer)
			{
				var value = json.ToObject<int>();
				if (value < min || value > max) return defaultValue;
				return value;
			}
			else if (json.Type == JTokenType.Float)
			{
				var value = Convert.ToInt32(json.ToObject<double>());
				if (value < min || value > max) return defaultValue;
				return value;
			}
			else
			{
				return defaultValue;
			}
		}
	}
}
