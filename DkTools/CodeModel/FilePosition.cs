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

		public FilePosition(string fileName, int pos)
		{
			_fileName = fileName;
			_pos = pos;
			_primaryFile = false;
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
			return string.Equals(_fileName, right._fileName, StringComparison.OrdinalIgnoreCase) && _pos == right._pos;
		}

		public static bool operator == (FilePosition left, FilePosition right)
		{
			if (left == null) return right == null;
			return left.Equals(right);
		}

		public static bool operator != (FilePosition left, FilePosition right)
		{
			if (left == null) return right != null;
			return !left.Equals(right);
		}

		public override int GetHashCode()
		{
			return _fileName.GetHashCode() ^ _pos.GetHashCode();
		}

		public bool IsInFile
		{
			get { return !string.IsNullOrEmpty(_fileName); }
		}

		public bool IsEmpty
		{
			get { return string.IsNullOrEmpty(_fileName); }
		}

		public override string ToString() => $"{_fileName}({_pos})";
	}
}
