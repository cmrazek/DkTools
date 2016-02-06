using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class CompositeToken : GroupToken
	{
		private DataType _dataType;

		public CompositeToken(Scope scope, DataType dataType, params Token[] tokens)
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
