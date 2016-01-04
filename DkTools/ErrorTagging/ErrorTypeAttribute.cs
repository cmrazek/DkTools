using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ErrorTagging
{
	class ErrorTypeAttribute : Attribute
	{
		public ErrorType Type { get; set; }

		public ErrorTypeAttribute(ErrorType type)
		{
			Type = type;
		}
	}
}
