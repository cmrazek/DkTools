using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel
{
	internal struct Scope
	{
		private CodeFile _file;
		private ScopeHint _hint;
		private int _depth;
		private bool _visible;	// Is this file directly visible to the user?
		private DefinitionProvider _defProvider;
		private string _className;
		private CodeParser _code;
		private IBreakOwner _breakOwner;
		private IContinueOwner _continueOwner;
		private DataType _returnDataType;

		public Scope(CodeModel model)
		{
			_file = null;
			_hint = default(ScopeHint);
			_depth = 0;
			_visible = false;
			_defProvider = model.DefinitionProvider;
			_className = null;
			_code = null;
			_breakOwner = null;
			_continueOwner = null;
			_returnDataType = null;
		}

		public Scope(CodeFile file, int depth, ScopeHint hint, bool visible, DefinitionProvider defProvider)
		{
#if DEBUG
			if (defProvider == null) throw new InvalidOperationException("Model has no definition provider.");
#endif
			_file = file;
			_depth = depth;
			_hint = hint;
			_visible = visible;
			_defProvider = defProvider;
			_className = null;
			_code = file.CodeParser;
			_breakOwner = null;
			_continueOwner = null;
			_returnDataType = null;
		}

		public Scope Clone()
		{
			return new Scope(_file, _depth, _hint, _visible, _defProvider)
			{
				_className = _className,
				_breakOwner = _breakOwner,
				_continueOwner = _continueOwner,
				_returnDataType = _returnDataType
			};
		}

		public Scope CloneIndent()
		{
			var ret = Clone();
			ret._depth++;
			return ret;
		}

		public Scope CloneIndentNonRoot()
		{
			var scope = CloneIndent();
			scope.Root = false;
			return scope;
		}

		public bool Root
		{
			get { return (_hint & ScopeHint.NotOnRoot) == 0; }
			set
			{
				if (value) _hint &= ~ScopeHint.NotOnRoot;
				else _hint |= ScopeHint.NotOnRoot;
			}
		}

		public int Depth
		{
			get { return _depth; }
		}

		public CodeFile File
		{
			get { return _file; }
			set
			{
#if DEBUG
				if (_file != null) throw new InvalidOperationException("Cannot reassign file for scope.");
#endif
				_file = value;
			}
		}

		public ScopeHint Hint
		{
			get { return _hint; }
			set { _hint = value; }
		}

		public bool Visible
		{
			get { return _visible; }
		}

		public DefinitionProvider DefinitionProvider
		{
			get { return _defProvider; }
		}

		public FileStore FileStore
		{
			get
			{
				if (_file == null) return null;
				return _file.Model.FileStore;
			}
		}

		public CodeModel Model
		{
			get { return _file != null ? _file.Model : null; }
		}

		public string ClassName
		{
			get { return _className; }
			set { _className = value; }
		}

		public CodeParser Code
		{
			get { return _code; }
		}

		public IBreakOwner BreakOwner
		{
			get { return _breakOwner; }
			set { _breakOwner = value; }
		}

		public IContinueOwner ContinueOwner
		{
			get { return _continueOwner; }
			set { _continueOwner = value; }
		}

		public DataType ReturnDataType
		{
			get { return _returnDataType; }
			set { _returnDataType = value; }
		}
	}

	[Flags]
	public enum ScopeHint
	{
		/// <summary>
		/// No special hints.
		/// </summary>
		None = 0x00,

		/// <summary>
		/// Inside argument list for a function definition.
		/// </summary>
		FunctionArgs = 0x01,

		/// <summary>
		/// Function calls are not allowed here.
		/// </summary>
		SuppressFunctionCall = 0x02,

		/// <summary>
		/// Variable declarations are not allowed here.
		/// </summary>
		SuppressVarDecl = 0x04,

		/// <summary>
		/// Function definitions are not allowed here.
		/// </summary>
		SuppressFunctionDefinition = 0x08,

		/// <summary>
		/// No longer parsing on the root.
		/// </summary>
		NotOnRoot = 0x10,

		/// <summary>
		/// Variables are not allowed to be used here.
		/// </summary>
		SuppressVars = 0x20,

		/// <summary>
		/// Data types will not be parsed here.
		/// </summary>
		SuppressDataType = 0x40,

		/// <summary>
		/// Flow control statements (if, switch, while, etc.) will not be parsed here.
		/// </summary>
		SuppressControlStatements = 0x80,

		/// <summary>
		/// Inside the where clause of a select statement.
		/// </summary>
		SelectWhereClause = 0x100,

		/// <summary>
		/// Inside the order by of a select statement.
		/// </summary>
		SelectOrderBy = 0x200,

		/// <summary>
		/// Inside the body of a select statement.
		/// </summary>
		SelectBody = 0x400,

		/// <summary>
		/// Inside the from section of a select statement.
		/// </summary>
		SelectFrom = 0x800,

		/// <summary>
		/// Currently parsing inside an alter statement.
		/// </summary>
		InsideAlter = 0x1000,
	}
}
