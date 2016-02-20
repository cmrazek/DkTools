using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal struct FilePosition
	{
		private string _fileName;
		private int _pos;
		private bool _primaryFile;

		public static readonly FilePosition Empty = new FilePosition();

		public FilePosition(string fileName, int pos, bool primaryFile)
		{
			_fileName = fileName;
			_pos = pos;
			_primaryFile = primaryFile;
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public int Position
		{
			get { return _pos; }
		}

		public bool PrimaryFile
		{
			get { return _primaryFile; }
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(FilePosition)) return false;

			var right = (FilePosition)obj;
			return _fileName == right._fileName && _pos == right._pos;
		}

		public override int GetHashCode()
		{
			return _fileName.GetHashCode() ^ _pos.GetHashCode();
		}

		public bool IsInFile
		{
			get { return !string.IsNullOrEmpty(_fileName); }
		}
	}
}
