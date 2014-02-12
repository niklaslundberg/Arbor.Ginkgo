﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Web.XmlTransform;

namespace Arbor.Ginkgo
{
	public static class IisHelper
	{
		public static async Task<IisExpress> StartWebsiteAsync(Path websitePath, Path templatePath,
		                                                       Action<Path> onCopiedWebsite = null, int tcpPort = -1, string transformConfiguration = null, string tempPath = null)
		{
			int port = tcpPort >= IPEndPoint.MinPort ? tcpPort : GetAvailablePort();

			var iisExpress = new IisExpress();

            Path tempWebsitePath = tempPath != null ? new Path(tempPath) : Path.Combine(System.IO.Path.GetTempPath(), "Ginkgo", "TempWebsite",
			                            port.ToString(CultureInfo.InvariantCulture));

            CopyWebsiteToTempPath(websitePath, tempWebsitePath);

            TransformWebConfig(websitePath, transformConfiguration, tempWebsitePath);

			if (onCopiedWebsite != null)
			{
				onCopiedWebsite(tempWebsitePath);
			}

			await iisExpress.Start(templatePath, port, tempWebsitePath, removeSiteOnExit: true);
			return iisExpress;
		}

	    static void TransformWebConfig(Path websitePath, string transformConfiguration, Path tempWebsitePath)
	    {
	        if (string.IsNullOrWhiteSpace(transformConfiguration))
	        {
	            return;
	        }
	        
	        Path transformRootFile = Path.Combine(websitePath, "web.config");

	        Path transformationFile = Path.Combine(websitePath, string.Format("web.{0}.config", transformConfiguration));

	        if (File.Exists(transformRootFile.FullName) && File.Exists(transformationFile.FullName))
	        {
	            Path targetFile = Path.Combine(tempWebsitePath, "web.config");

	            var transformable = new XmlTransformableDocument();
	            transformable.Load(transformRootFile.FullName);

	            var transformation = new XmlTransformation(transformationFile.FullName);

	            if (transformation.Apply(transformable))
	            {
	                transformable.Save(targetFile.FullName);
	            }
	        }
	    }

	    static Path CopyWebsiteToTempPath(Path websitePath, Path tempPath)
		{
			var originalWebsiteDirectory = new DirectoryInfo(websitePath.FullName);

			var tempDirectory = new DirectoryInfo(tempPath.FullName);

			if (tempDirectory.Exists)
			{
				tempDirectory.Delete(true);
			}

			tempDirectory.Create();

			var bannedExtensionList = new List<string> {".user", ".cs", ".csproj", ".dotSettings", ".suo"};
			var bannedFiles = new List<string> {"packages.config"};
			var bannedDirectories = new List<string> {"obj"};

			Predicate<FileInfo> bannedExtensions =
				file =>
				bannedExtensionList.Any(extension => extension.Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase));

			Predicate<FileInfo> bannedFileNames =
				file =>
				bannedFiles.Any(bannedFile => bannedFile.Equals(file.Name, StringComparison.InvariantCultureIgnoreCase));

			IEnumerable<Predicate<FileInfo>> filesToExclude = new List<Predicate<FileInfo>>
				                                                  {
					                                                  bannedExtensions,
					                                                  bannedFileNames
				                                                  };

			originalWebsiteDirectory.CopyTo(tempDirectory, filesToExclude: filesToExclude, directoriesToExclude: bannedDirectories);

			return tempPath;
		}

		static int GetAvailablePort()
		{
			var range = new PortPoolRange(44300, 100);

			return TcpHelper.GetAvailablePort(range);
		}
	}
}