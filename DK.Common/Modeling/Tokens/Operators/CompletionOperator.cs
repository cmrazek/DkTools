namespace DK.Modeling.Tokens
{
	public abstract class CompletionOperator : GroupToken
	{
		public abstract DataType CompletionDataType { get; }

		internal CompletionOperator(Scope scope)
			: base(scope)
		{ }
	}
}
