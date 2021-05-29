namespace DK.Modeling.Tokens
{
	public class CompositeToken : GroupToken
	{
		private DataType _dataType;

		internal CompositeToken(Scope scope, DataType dataType, params Token[] tokens)
			: base(scope)
		{
			foreach (var token in tokens) AddToken(token);

			_dataType = dataType;
		}

		public override DataType ValueDataType
		{
			get
			{
				if (_dataType != null) return _dataType;
				return base.ValueDataType;
			}
		}
	}
}
