using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace DkTools
{
	public class IniFile
	{
		private class IniSection
		{
			public Dictionary<string, string> values = new Dictionary<string, string>();
		}

		private string _fileName = "";
		private Dictionary<string, IniSection> _sections = new Dictionary<string, IniSection>();
		private List<string> _sectionNames = new List<string>();

		private static Regex _rxSection = new Regex(@"^\[([^\]]+)\]\s*$");
		private static Regex _rxValue = new Regex(@"^(\w+)\=(.*)$");

		public IniFile()
		{
		}

		public IniFile(string fileName)
		{
			_fileName = fileName;
			Load();
		}

		public void Load()
		{
			_sections.Clear();
			_sectionNames.Clear();
			if (string.IsNullOrEmpty(_fileName)) return;

			//Log.Write("Loading INI file '{0}'.", _fileName);
			using (StreamReader sr = new StreamReader(_fileName))
			{
				IniSection curSection = null;

				while (!sr.EndOfStream)
				{
					string line = sr.ReadLine().Trim();
					if (line.StartsWith("#") || line.StartsWith(";")) continue;

					Match match = _rxSection.Match(line);
					if (match.Success)
					{
						string sectionName = match.Groups[1].Value;
						//Log.Write("Found section '{0}'.", sectionName);
						if (!_sections.TryGetValue(sectionName.ToLower(), out curSection))
						{
							curSection = new IniSection();
							_sections.Add(sectionName.ToLower(), curSection);
							_sectionNames.Add(sectionName);
						}
					}
					else if (curSection != null && (match = _rxValue.Match(line)).Success)
					{
						//Log.Write("Found key '{0}' with value '{1}'.", match.Groups[1].Value, match.Groups[2].Value);
						curSection.values[match.Groups[1].Value.ToLower()] = match.Groups[2].Value;
					}
				}
			}

			//Log.Write("Finished loading INI file.");
		}

		public string this[string sectionName, string keyName]
		{
			get
			{
				IniSection section;
				if (!_sections.TryGetValue(sectionName.ToLower(), out section)) return "";

				string val;
				if (section.values.TryGetValue(keyName.ToLower(), out val)) return val;
				return "";
			}
		}

		public IEnumerable<string> SectionNames
		{
			get { return _sectionNames; }
		}
	}
}
