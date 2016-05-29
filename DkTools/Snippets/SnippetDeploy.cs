using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Snippets
{
	internal static class SnippetDeploy
	{
		private const string k_resourcePrefix = "DkTools.Snippets.Default.";
		private const string k_resourceSuffix = ".snippet";

		public static void DeploySnippets()
		{
			try
			{
				var asmInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
				var modified = asmInfo.LastWriteTime;

				var appDataDir = ProbeToolsPackage.AppDataDir;
				var snippetDir = Path.Combine(appDataDir, "Snippets");

				var asm = Assembly.GetExecutingAssembly();
				DeployFile(asm, "DkTools.Snippets.SnippetIndex.xml", Path.Combine(appDataDir, "SnippetIndex.xml"), modified);

				foreach (var resName in asm.GetManifestResourceNames())
				{
					if (resName.StartsWith(k_resourcePrefix, StringComparison.OrdinalIgnoreCase) &&
						resName.EndsWith(k_resourceSuffix, StringComparison.OrdinalIgnoreCase))
					{
						var fileName = resName.Substring(k_resourcePrefix.Length);


						var lastDot = fileName.LastIndexOf('.');
						if (lastDot > 0)
						{
							var title = fileName.Substring(0, lastDot).Replace('.', '\\');
							var ext = fileName.Substring(lastDot);
							fileName = string.Concat(title, ext);
						}

						DeployFile(asm, resName, Path.Combine(snippetDir, fileName), modified);
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		private static void DeployFile(Assembly asm, string resourceName, string fileName, DateTime deployDate)
		{
			try
			{
				Log.Write(LogLevel.Info, "Deploying snippet resource '{0}' to '{1}'...", resourceName, fileName);

				var dirPath = Path.GetDirectoryName(fileName);
				if (!Directory.Exists(dirPath)) FileUtil.CreateDirectoryRecursive(dirPath);

				if (File.Exists(fileName))
				{
					var fileInfo = new FileInfo(fileName);
					if (fileInfo.LastWriteTime >= deployDate)
					{
						Log.Debug("Snippet file '{0}' is already up-to-date.", fileName);
						return;
					}
				}

				using (var stream = asm.GetManifestResourceStream(resourceName))
				{
					if (stream == null) throw new InvalidOperationException(string.Format("Snippet resource '{0}' could not be found.", resourceName));

					var length = (int)stream.Length;
					byte[] buf;
					buf = new byte[length];
					stream.Read(buf, 0, length);
					File.WriteAllBytes(fileName, buf);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}
	}
}
