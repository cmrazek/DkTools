using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.PreprocessorTokens
{
	internal class NumberToken : Token
	{
		private long? _value;

		public NumberToken(GroupToken parent, string text)
			: base(parent)
		{
			long value;
			if (!long.TryParse(text, out value))
			{
				Log.WriteDebug("'{0}' is not a valid number.", text);
				_value = null;
			}
			else _value = value;
		}

		public NumberToken(GroupToken parent, long? value)
			: base(parent)
		{
			_value = value;
		}

		public override long? Value
		{
			get { return _value; }
		}
	}
}
