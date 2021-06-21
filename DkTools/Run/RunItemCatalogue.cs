﻿using DK.AppEnvironment;
using DK.Diagnostics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Run
{
	class RunItemCatalogue
	{
		private Dictionary<string, JToken> _apps = new Dictionary<string, JToken>();

		public const string SystemRunItem_SAM = "SAM";
		public const string SystemRunItem_CAM = "CAM";

		public void Load()
		{
			_apps.Clear();

			try
			{
				var jsonFileName = Path.Combine(ProbeToolsPackage.AppDataDir, Constants.RunJsonFileName);
				if (!File.Exists(jsonFileName)) return;
				var jsonContent = File.ReadAllText(jsonFileName);
				var jsonRoot = JObject.Parse(jsonContent);

				var jsonApps = jsonRoot["apps"];
				if (jsonApps != null && jsonApps.Type == JTokenType.Array)
				{
					foreach (var jsonApp in jsonApps)
					{
						if (jsonApp.Type != JTokenType.Object) continue;

						var appName = jsonApp["name"]?.ToString();
						if (string.IsNullOrEmpty(appName)) continue;

						var jsonItems = jsonApp["items"];
						if (jsonItems.Type != JTokenType.Array) continue;

						_apps[appName] = jsonItems;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public void Save()
		{
			try
			{
				var json = new JObject
				{
					{ "apps", new JArray(
						_apps.Select(a => new JObject
						{
							{ "name", a.Key },
							{ "items", a.Value }
						})
					)}
				};

				var jsonFileName = Path.Combine(ProbeToolsPackage.AppDataDir, Constants.RunJsonFileName);
				File.WriteAllText(jsonFileName, json.ToString());
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public static RunItem GetSystemDefinedRunItem(string title, DkAppSettings appSettings)
		{
			switch (title)
			{
				case SystemRunItem_SAM:
					return RunItem.CreateSam();
				case SystemRunItem_CAM:
					return RunItem.CreateCam();
				default:
					return null;
			}
		}

		public IEnumerable<RunItem> GetRunItemsForApp(DkAppSettings appSettings)
		{
			if (appSettings == null) throw new ArgumentNullException(nameof(appSettings));

			if (_apps.TryGetValue(appSettings.AppName, out var jsonItems))
			{
				foreach (var jsonItem in jsonItems)
				{
					var runItem = RunItem.FromJson(jsonItem, appSettings);
					if (runItem != null) yield return runItem;
				}
			}
			else
			{
				yield return GetSystemDefinedRunItem(SystemRunItem_SAM, appSettings);
				yield return GetSystemDefinedRunItem(SystemRunItem_CAM, appSettings);
			}
		}

		public void SetRunItemsForApp(DkAppSettings appSettings, IEnumerable<RunItem> runItems)
		{
			if (appSettings == null) throw new ArgumentNullException(nameof(appSettings));

			_apps[appSettings.AppName] = new JArray(runItems.Select(r => r.ToJson()));
		}

		public static string GetSamFilePath(DkAppSettings appSettings) => Path.Combine(appSettings.PlatformPath, "SAM.exe");
		public static string GetSamWorkingDir(DkAppSettings appSettings) => appSettings.ExeDirs.FirstOrDefault() ?? appSettings.PlatformPath;
		public static string GetCamFilePath(DkAppSettings appSettings) => Path.GetFullPath(Path.Combine(appSettings.PlatformPath, "..\\CAMNet\\CAMNet.exe"));
		public static string GetCamWorkingDir(DkAppSettings appSettings) => Path.GetDirectoryName(GetCamFilePath(appSettings));
	}
}
