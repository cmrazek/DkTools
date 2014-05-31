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
		}
	}
}
