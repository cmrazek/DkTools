namespace DK.Code
{
	public enum ServerContext
	{
		Neutral,
		Server,
		Client
	}

	public static class ServerContextHelper
	{
		public static ServerContext FromFileName(string fileName)
		{
			switch (FileContextHelper.GetFileContextFromFileName(fileName))
			{
				case FileContext.ServerTrigger:
				case FileContext.ServerClass:
				case FileContext.ServerProgram:
					return ServerContext.Server;

				case FileContext.ClientTrigger:
				case FileContext.ClientClass:
				case FileContext.GatewayProgram:
					return ServerContext.Client;

				default:
					return ServerContext.Neutral;
			}
		}

		public static int GetScore(this ServerContext sc, ServerContext call)
		{
			switch (sc)
			{
				case ServerContext.Server:
					if (call == ServerContext.Server) return 1;
					else if (call == ServerContext.Client) return -1;
					else return 0;

				case ServerContext.Client:
					if (call == ServerContext.Client) return 1;
					else if (call == ServerContext.Server) return -1;
					else return 0;

				default:
					if (call == ServerContext.Neutral) return 1;
					return -1;
			}
		}
	}
}
