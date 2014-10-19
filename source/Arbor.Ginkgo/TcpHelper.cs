using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Arbor.Ginkgo
{
	internal static class TcpHelper
	{
		public static int GetAvailablePort(PortPoolRange range, IEnumerable<int> exludes = null)
		{
		    var excluded = (exludes ?? new List<int>()).ToList();

			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

			for (int port = range.StartPort; port <= range.EndPort; port++)
			{
				var portIsInUse = tcpConnInfoArray.Any(tcpPort => tcpPort.LocalEndPoint.Port == port);

			    int port1 = port;
			    if (!portIsInUse && !excluded.Any(p => p == port1))
				{
					return port;
				}
			}

			throw new InvalidOperationException(string.Format("Could not find any TCP port in range {0}", range.Format()));
		}
	}
}