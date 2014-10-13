using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace DkTools.Run
{
	[Guid(GuidList.strRunOptions)]
	public class RunOptions : DialogPage
	{
		public bool RunSam { get; set; }
		public bool RunCam { get; set; }
		public bool Diags { get; set; }
		public bool LoadSam { get; set; }
		public bool SetDbDate { get; set; }
		public int TransReportTimeout { get; set; }
		public int TransAbortTimeout { get; set; }
		public int MinChannels { get; set; }
		public int MaxChannels { get; set; }
		public int LoadSamTime { get; set; }
		public bool CamDevMode { get; set; }
		public bool CamDesignMode { get; set; }
		public string SamArgs { get; set; }
		public string CamArgs { get; set; }

		public RunOptions()
		{
			this.RunSam = true;
			this.RunCam = true;
			this.Diags = true;
			this.LoadSam = false;
			this.LoadSamTime = 10000;
			this.SetDbDate = true;
			this.TransReportTimeout = 10;
			this.TransAbortTimeout = 20;
			this.MinChannels = 1;
			this.MaxChannels = 2;
			this.CamDevMode = true;
			this.CamDesignMode = false;
		}

		internal string CreateSamArgsString()
		{
			var sb = new StringBuilder();
			sb.Append(string.Format("/N{0}", CleanSamName(string.Concat(ProbeEnvironment.CurrentApp, "_", System.Environment.UserName))));
			sb.Append(string.Format(" /p{0}", ProbeEnvironment.SamPort));
			sb.Append(" /o0");
			sb.Append(string.Format(" /y{0:00}{1:00}", TransReportTimeout, TransAbortTimeout));
			sb.Append(string.Format(" /z{0}", MinChannels));
			sb.Append(string.Format(" /Z{0}", MaxChannels));
			sb.Append(string.Format(" /P{0}", ProbeEnvironment.CurrentApp));
			if (Diags) sb.Append(" /d2");

			if (!string.IsNullOrWhiteSpace(SamArgs))
			{
				sb.Append(" ");
				sb.Append(SamArgs);
			}

			return sb.ToString();
		}

		internal string CreateCamArgsString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("appname=" + ProbeEnvironment.CurrentApp);
			sb.Append(" networkname=" + CleanSamName(System.Environment.UserName + "_" + System.Environment.MachineName));

			if (Diags) sb.Append(" devmode=2");
			else if (CamDevMode) sb.Append(" devmode");

			if (CamDesignMode) sb.Append(" designmode=true");

			if (!string.IsNullOrWhiteSpace(CamArgs))
			{
				sb.Append(" ");
				sb.Append(CamArgs);
			}

			return sb.ToString();
		}

		private string CleanSamName(string name)
		{
			StringBuilder sb = new StringBuilder(name.Length);
			foreach (char ch in name)
			{
				if (Char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
			}
			return sb.ToString();
		}
	}
}
