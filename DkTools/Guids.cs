// Guids.cs
// MUST match guids.h
using System;

namespace DkTools
{
	static class GuidList
	{
		public const string strProbeToolsPkg = "38ad600c-15b8-4899-be00-9361f35cd8d9";
		public const string strProbeToolsCmdSet = "7a61de10-9508-4214-8946-33f1f60c6747";
		public const string strProbeExplorerOptions = "5F6C8F0F-9132-4907-878E-81C368CD694E";
		public const string strRunOptions = "1288EDB8-92E0-464D-A043-6B8910A55FBB";
		public const string strTaggingOptions = "F9C962E2-8823-4FD3-913E-3BD1E7910EEB";
		public const string strEditorOptions = "e509284d-7a84-4aa6-aa5d-8e31e0e8792b";

		public static readonly Guid guidProbeToolsCmdSet = new Guid(strProbeToolsCmdSet);
	};
}