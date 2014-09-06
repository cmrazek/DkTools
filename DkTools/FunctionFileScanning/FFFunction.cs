using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.FunctionFileScanning
{
	internal class FFFunction
	{
		private string _name;
		private string _sig;
		private string _fileName;
		private CodeModel.Span _span;
		private CodeModel.DataType _dataType;
		private CodeModel.FunctionPrivacy _privacy;
		private CodeModel.Definitions.FunctionDefinition _def;

		private FFFunction()
		{
		}

		public static FFFunction FromDatabase(FunctionFileDatabase.Function_t func)
		{
			if (string.IsNullOrEmpty(func.name) || string.IsNullOrEmpty(func.signature) || string.IsNullOrEmpty(func.fileName))
			{
				return null;
			}

			var span = func.span.ToCodeModelSpan();
			var dataType = func.dataType != null ? func.dataType.ToCodeModelDataType() : CodeModel.DataType.Int;
			var privacy = func.privacySpecified ? func.privacy.ToCodeModelFunctionPrivacy() : CodeModel.FunctionPrivacy.Public;

			return new FFFunction
			{
				_name = func.name,
				_sig = func.signature,
				_fileName = func.fileName,
				_span = span,
				_dataType = dataType,
				_privacy = privacy,
				_def = new CodeModel.Definitions.FunctionDefinition(new CodeModel.Scope(), func.name, new CodeModel.ExternalToken(func.fileName, span), dataType, func.signature,
					CodeModel.Position.Start, CodeModel.Position.Start, privacy, true)
			};
		}

		public static FFFunction FromCodeModelDefinition(CodeModel.Definitions.FunctionDefinition def)
		{
			return new FFFunction
			{
				_name = def.Name,
				_sig = def.Signature,
				_fileName = def.SourceFileName,
				_span = def.SourceSpan,
				_dataType = def.DataType,
				_privacy = def.Privacy,
				_def = def
			};
		}

		public FunctionFileDatabase.Function_t ToDatabase()
		{
			return new FunctionFileDatabase.Function_t
			{
				name = _name,
				signature = _sig,
				fileName = _fileName,
				span = _span.ToFunctionFileDatabase(),
				dataType = _dataType.ToFuncDbDataType(),
				privacySpecified = true,
				privacy = _privacy.ToFuncDbFunctionPrivacy()
			};
		}

		public string Name
		{
			get { return _name; }
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public string Signature
		{
			get { return _sig; }
		}

		public CodeModel.Definitions.FunctionDefinition Definition
		{
			get { return _def; }
		}
	}
}
