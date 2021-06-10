namespace DK.Modeling.Tokens
{
	public class ClassAndFunctionToken : GroupToken
	{
		private FunctionCallToken _funcToken;

		internal ClassAndFunctionToken(Scope scope, ClassToken classToken, DotToken dotToken, FunctionCallToken funcToken, Definitions.FunctionDefinition funcDef)
			: base(scope)
		{
			AddToken(classToken);
			AddToken(dotToken);
			AddToken(_funcToken = funcToken);
		}

		public override DataType ValueDataType
		{
			get
			{
				return _funcToken.ValueDataType;
			}
		}
	}
}
