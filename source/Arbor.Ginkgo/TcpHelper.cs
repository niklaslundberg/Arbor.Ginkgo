using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace Arbor.Ginkgo
{
	internal static class TcpHelper
	{
		public static int GetAvailablePort(PortPoolRange range)
		{
			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

			for (int port = range.StartPort; port <= range.EndPort; port++)
			{
				var portIsInUse = tcpConnInfoArray.Any(tcpPort => tcpPort.LocalEndPoint.Port == port);

				if (!portIsInUse)
				{
					return port;
				}
			}

			throw new InvalidOperationException(string.Format("Could not find any TCP port in range {0}", range.Format()));
		}
	}
}