using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools
{
	class ProbeAppSettings
	{
		public string AppName { get; set; }
		public bool Initialized { get; set; }
		public string PlatformPath { get; set; }
		public string[] AppNames { get; set; }
		public string[] SourceDirs { get; set; }
		public string[] IncludeDirs { get; set; }
		public string[] LibDirs { get; set; }
		public string[] ExeDirs { get; set; }
		public string ObjectDir { get; set; }
		public string TempDir { get; set; }
		public string ReportDir { get; set; }
		public string DataDir { get; set; }
		public string LogDir { get; set; }

		public ProbeAppSettings()
		{
			AppName = string.Empty;
			Initialized = false;
			PlatformPath = string.Empty;
			AppNames = new string[0];
			SourceDirs = new string[0];
			IncludeDirs = new string[0];
			LibDirs = new string[0];
			ExeDirs = new string[0];
			ObjectDir = string.Empty;
			TempDir = string.Empty;
			ReportDir = string.Empty;
			DataDir = string.Empty;
			LogDir = string.Empty;
		}
	}
}
