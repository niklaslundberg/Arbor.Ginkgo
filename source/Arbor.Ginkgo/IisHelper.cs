using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Microsoft.Web.XmlTransform;

namespace Arbor.Ginkgo
{
    public static class IisHelper
    {
        public static async Task<IisExpress> StartWebsiteAsync(Path websitePath, Path templatePath,
            Action<Path> onCopiedWebsite = null, int tcpPort = -1, string transformConfiguration = null,
            string tempPath = null, bool removeSiteOnExit = true, int sslTcpPort = -1, string customHostName = "", bool httpsEnabled = false, int customHostNameSslTcpPort = -1)
        {
            int httpPort = tcpPort >= IPEndPoint.MinPort ? tcpPort : GetAvailableHttpPort();
            int httpsPort = -1;
            int customHostNameHttpsPort = -1;

            if (httpsEnabled)
            {
                httpsPort = sslTcpPort >= IPEndPoint.MinPort ? sslTcpPort : GetAvailableHttpsPort(httpPort);
                customHostNameHttpsPort = customHostNameSslTcpPort >= IPEndPoint.MinPort ? customHostNameSslTcpPort : GetAvailableHttpsPort(httpPort, httpsPort);
            }

            var iisExpress = new IisExpress();

            Path tempWebsitePath = tempPath != null
                ? new Path(tempPath)
                : Path.Combine(System.IO.Path.GetTempPath(), "Arbor.Ginkgo", "TempWebsite", Guid.NewGuid().ToString(),
                    httpPort.ToString(CultureInfo.InvariantCulture));

            CopyWebsiteToTempPath(websitePath, tempWebsitePath);

            Console.WriteLine("Copying files from {0} to {1}", websitePath.FullName, tempWebsitePath.FullName);

            TransformWebConfig(websitePath, transformConfiguration, tempWebsitePath);

            if (onCopiedWebsite != null)
            {
                onCopiedWebsite(tempWebsitePath);
            }

            await
                iisExpress.StartAsync(templatePath, httpPort, httpsPort, tempWebsitePath, removeSiteOnExit,
                    customHostName, customHostNameHttpsPort);
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

                string fullName = new FileInfo(targetFile.FullName).Directory.FullName;
                if (Directory.Exists(fullName))
                {
                    Console.WriteLine("Transforming root file '{0}' with transformation '{1}' into '{2}'", transformRootFile.FullName, transformationFile.FullName, targetFile.FullName);

                    var transformable = new XmlTransformableDocument();
                    transformable.Load(transformRootFile.FullName);

                    var transformation = new XmlTransformation(transformationFile.FullName);

                    if (transformation.Apply(transformable))
                    {
                        transformable.Save(targetFile.FullName);
                    }
                }
                else
                {
                    Console.WriteLine("Directory {0} does not exist", fullName);
                }
            }
        }

        static void CopyWebsiteToTempPath(Path websitePath, Path tempPath)
        {
            var originalWebsiteDirectory = new DirectoryInfo(websitePath.FullName);

            var tempDirectory = new DirectoryInfo(tempPath.FullName);

            if (tempDirectory.Exists)
            {
                Console.WriteLine("Deleting temp directory {0}", tempDirectory.FullName);
                tempDirectory.Delete(true);
            }
            tempDirectory.Refresh();
            Console.WriteLine("Creating temp directory {0}", tempDirectory.FullName);
            tempDirectory.Create();

            var bannedExtensionList = new List<string>
                                          {
                                              ".user",
                                              ".cs",
                                              ".csproj",
                                              ".dotSettings",
                                              ".suo",
                                              ".xproj",
                                              ".targets",
                                              ".nuspec",
                                              ".orig",
                                              ".ncrunchproject"
                                          };

            var bannedFiles = new List<string>
                                  {
                                      "packages.config",
                                      "project.json",
                                      "project.lock.json",
                                      "config.json",
                                      "bower.json",
                                      "package.json",
                                      "gruntfile.json",
                                      "Microsoft.CodeAnalysis.Analyzers.dll",
                                      "Microsoft.Build.Tasks.CodeAnalysis.dll",
                                      "csc.exe",
                                      "Microsoft.CodeAnalysis.CSharp.dll",
                                      "Microsoft.CodeAnalysis.VisualBasic.dll",
                                      "vbc.exe",
                                      "VBCSCompiler.exe",
                                      "web.debug.config",
                                      "web.release.config"
                                  };
            var bannedDirectories = new List<string> {"obj", "node_modules", "bower_components", "roslyn"};

            Predicate<FileInfo> bannedExtensions =
                file =>
                    bannedExtensionList.Any(
                        extension => extension.Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase));

            Predicate<FileInfo> bannedFileNames =
                file =>
                    bannedFiles.Any(
                        bannedFile => bannedFile.Equals(file.Name, StringComparison.InvariantCultureIgnoreCase));

            IEnumerable<Predicate<FileInfo>> filesToExclude = new List<Predicate<FileInfo>>
                                                              {
                                                                  bannedExtensions,
                                                                  bannedFileNames
                                                              };

            int itemsCopied = originalWebsiteDirectory.CopyTo(tempDirectory, filesToExclude: filesToExclude,
                directoriesToExclude: bannedDirectories);

            Console.WriteLine("Copied {0} items", itemsCopied);
        }

        static int GetAvailableHttpPort(int? exclude = null)
        {
            var range = new PortPoolRange(45000, 100);

            var excluded = new List<int>();

            if (exclude.HasValue)
            {
                excluded.Add(exclude.Value);
            }

            return TcpHelper.GetAvailablePort(range, excluded);
        }
        static int GetAvailableHttpsPort(params int[] exclusions)
        {
            var range = new PortPoolRange(44330, 100);

            var excluded = new List<int>();

            if (exclusions != null && exclusions.Any())
            {
                excluded.AddRange(exclusions);
            }

            return TcpHelper.GetAvailablePort(range, excluded);
        }
    }
}