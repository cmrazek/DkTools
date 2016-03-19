using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.DkDict
{
	class Tag
	{
		public string Name { get; private set; }
		public string Value { get; private set; }

		public Tag(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}
}
