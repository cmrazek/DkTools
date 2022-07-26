namespace DK.Preprocessing.Tokens
{
	internal class NumberToken : Token
	{
		private long? _value;

		public NumberToken(GroupToken parent, string text)
			: base(parent)
		{
			long value;
			if (!long.TryParse(text, out value)) _value = null;
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
