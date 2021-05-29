using System;

namespace DK.Preprocessing
{
	public struct IncludeDependency
	{
		private string _fileName;
		private bool _include;
		private bool _localizedFile;
		private string _content;

		public static readonly IncludeDependency[] EmptyArray = new IncludeDependency[0];

		public IncludeDependency(string fileName, bool include, bool localizedFile, string content)
		{
			if (content == null) throw new ArgumentNullException(nameof(content));

			_fileName = fileName;
			_include = include;
			_localizedFile = localizedFile;
			_content = content;
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public bool Include
		{
			get { return _include; }
		}

		public bool LocalizedFile
		{
			get { return _localizedFile; }
		}

		public string Content
		{
			get { return _content; }
		}
	}
}
