using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Arbor.Ginkgo
{
	public static class IisHelper
	{
		public static async Task<IisExpress> StartWebsiteAsync(Path websitePath, Path templatePath)
		{
			int port = GetAvailablePort();

			var iisExpress = new IisExpress();

			var tempWebsitePath = CopyWebsiteToTempPath(websitePath, port);

			await iisExpress.Start(templatePath, port, tempWebsitePath, removeSiteOnExit: true);
			return iisExpress;
		}

		static Path CopyWebsiteToTempPath(Path websitePath, int port)
		{
			var tempPath = Path.Combine(System.IO.Path.GetTempPath(), "Ginkgo", "TempWebsite",
			                            port.ToString(CultureInfo.InvariantCulture));


			var originalWebsiteDirectory = new DirectoryInfo(websitePath.FullName);

			var tempDirectory = new DirectoryInfo(tempPath.FullName);

			if (tempDirectory.Exists)
			{
				tempDirectory.Delete(true);
			}

			tempDirectory.Create();

			var bannedExtensionList = new List<string> {".user", ".cs", ".csproj"};

			Predicate<FileInfo> bannedExtensions =
				file =>
				bannedExtensionList.Any(extension => extension.Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase));
			IEnumerable<Predicate<FileInfo>> filesToExclude = new List<Predicate<FileInfo>>
				                                                  {
					                                                  bannedExtensions
				                                                  };
			originalWebsiteDirectory.CopyTo(tempDirectory, filesToExclude: filesToExclude);

			return tempPath;
		}

		static int GetAvailablePort()
		{
			var range = new PortPoolRange(44300, 100);

			return TcpHelper.GetAvailablePort(range);
		}
	}
}