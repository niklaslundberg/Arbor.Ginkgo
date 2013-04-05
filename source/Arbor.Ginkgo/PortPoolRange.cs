using System;
using System.Globalization;
using System.Net;

namespace Arbor.Ginkgo
{
	internal class PortPoolRange
	{
		readonly int _portCount;

		public PortPoolRange(int port, int portCount = 0)
		{
			_portCount = portCount;
			StartPort = port;
			if (portCount < 0)
			{
				throw new ArgumentOutOfRangeException("portCount", "Port count must be a non-negative number");
			}

			if (port < IPEndPoint.MinPort)
			{
				throw new ArgumentOutOfRangeException("port",
				                                      string.Format("Port must be a number between {0} and {1}", IPEndPoint.MinPort,
				                                                    IPEndPoint.MaxPort));
			}
			if (port > IPEndPoint.MaxPort)
			{
				throw new ArgumentOutOfRangeException("port",
				                                      string.Format("Port must be a number between {0} and {1}", IPEndPoint.MinPort,
				                                                    IPEndPoint.MaxPort));
			}
			if (port + portCount > IPEndPoint.MaxPort)
			{
				throw new ArgumentOutOfRangeException("portCount",
				                                      string.Format("The last port number cannot be greater than {0}",
				                                                    IPEndPoint.MaxPort));
			}
		}

		public int StartPort { get; private set; }

		public int EndPort
		{
			get { return StartPort + _portCount; }
		}

		public string Format()
		{
			if (_portCount == 1)
			{
				return StartPort.ToString(CultureInfo.InvariantCulture);
			}

			return string.Format("{0}-{1}", StartPort, EndPort);
		}
	}
}