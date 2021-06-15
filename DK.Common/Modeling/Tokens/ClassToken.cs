using DK.Code;
using DK.Definitions;
using DK.Repository;

namespace DK.Modeling.Tokens
{
	public class ClassToken : WordToken
	{
		internal ClassToken(Scope scope, CodeSpan span, string text, ClassDefinition classDef)
			: base(scope, span, text)
		{
			SourceDefinition = classDef;
		}

		internal ClassToken(Scope scope, CodeSpan span, string text, RepoClass ffClass)
			: base(scope, span, text)
		{
			SourceDefinition = new ClassDefinition(ffClass.Name, ffClass.FileName, scope.Model.ServerContext);
		}
	}
}
