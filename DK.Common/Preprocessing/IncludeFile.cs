﻿using DK.AppEnvironment;
using DK.Code;
using System;

namespace DK.Preprocessing
{
	sealed class IncludeFile
	{
		private string _fullPathName;
		private CodeSource _source;

		public IncludeFile(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

			_fullPathName = fileName;
		}

		public CodeSource GetSource(DkAppSettings appSettings)
		{
			if (_source == null)
			{
				_source = appSettings.Context.IncludeFileCache.GetIncludeFileSource(appSettings, _fullPathName, FileContextHelper.IncludeFileShouldBeMerged(_fullPathName));
				if (_source == null)
				{
					_source = new CodeSource();
					_source.Append(string.Empty, _fullPathName, 0, 0, true, false, false);
					_source.Flush();
				}
			}
			return _source;
		}

		public string FullPathName
		{
			get { return _fullPathName; }
		}
	}
}
