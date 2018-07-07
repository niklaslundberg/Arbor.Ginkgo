using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Arbor.Xdt;

namespace Arbor.Ginkgo
{
    public static class IisHelper
    {
        public static async Task<IisExpress> StartWebsiteAsync(
            Path websitePath,
            Path templatePath,
            Action<Path> onCopiedWebsite = null,
            int httpPort = -1,
            string transformConfiguration = null,
            string tempPath = null,
            bool removeSiteOnExit = true,
            int httpsPort = -1,
            string customHostName = "",
            bool httpsEnabled = false,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            bool ignoreSiteRemovalErrors = false,
            Action<string> logger = null)
        {
            if (websitePath == null)
            {
                throw new ArgumentNullException(nameof(websitePath));
            }

            if (templatePath == null)
            {
                throw new ArgumentNullException(nameof(templatePath));
            }

            if (httpPort > 0 && httpsPort == httpPort)
            {
                throw new ArgumentException($"HTTP port and https port cannot be the same, {httpPort}");
            }

            int usedHttpPort = httpPort >= IPEndPoint.MinPort ? httpPort : GetAvailableHttpPort();
            int usedHttpsPort = -1;

            if (httpsEnabled)
            {
                usedHttpsPort = httpsPort >= IPEndPoint.MinPort ? httpsPort : GetAvailableHttpsPort(logger, usedHttpPort);
            }

            var iisExpress = new IisExpress(logger);

            Path tempWebsitePath = tempPath != null
                ? new Path(tempPath)
                : Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "Arbor.Ginkgo",
                    "TempWebsite",
                    DateTime.UtcNow.Ticks.ToString(),
                    httpPort.ToString(CultureInfo.InvariantCulture));

            CopyWebsiteToTempPath(websitePath, tempWebsitePath, logger);

            logger?.Invoke(string.Format("Copying files from {0} to {1}", websitePath.FullName, tempWebsitePath.FullName));

            TransformWebConfig(websitePath, transformConfiguration, tempWebsitePath, logger);

            onCopiedWebsite?.Invoke(tempWebsitePath);

            await iisExpress.StartAsync(
                    templatePath,
                    usedHttpPort,
                    usedHttpsPort,
                    tempWebsitePath,
                    removeSiteOnExit,
                    customHostName,
                    environmentVariables,
                    ignoreSiteRemovalErrors);

            return iisExpress;
        }

        static void TransformWebConfig(Path websitePath, string transformConfiguration, Path tempWebsitePath, Action<string> logger)
        {
            if (string.IsNullOrWhiteSpace(transformConfiguration))
            {
                return;
            }

            Path transformRootFile = Path.Combine(websitePath, "web.config");

            Path transformationFile = Path.Combine(websitePath, $"web.{transformConfiguration}.config");

            if (File.Exists(transformRootFile.FullName) && File.Exists(transformationFile.FullName))
            {
                Path targetFile = Path.Combine(tempWebsitePath, "web.config");

                string fullName = new FileInfo(targetFile.FullName).Directory.FullName;

                if (Directory.Exists(fullName))
                {
                    logger?.Invoke(string.Format("Transforming root file '{0}' with transformation '{1}' into '{2}'", transformRootFile.FullName, transformationFile.FullName, targetFile.FullName));

                    string tempFile = null;

                    using (var transformable = new XmlTransformableDocument())
                    {
                        transformable.PreserveWhitespace = true;

                        transformable.Load(transformRootFile.FullName);

                        using (var transformation = new XmlTransformation(transformationFile.FullName))
                        {
                            if (transformation.Apply(transformable))
                            {
                                tempFile = System.IO.Path.GetTempFileName();

                                transformable.Save(tempFile);

                                File.Copy(tempFile, targetFile.FullName, overwrite: true);

                            }
                        }
                    }

                    File.Delete(tempFile);
                }
                else
                {
                    logger?.Invoke(string.Format("Directory {0} does not exist", fullName));
                }
            }
        }

        static void CopyWebsiteToTempPath(Path websitePath, Path tempPath, Action<string> logger)
        {
            var originalWebsiteDirectory = new DirectoryInfo(websitePath.FullName);

            var tempDirectory = new DirectoryInfo(tempPath.FullName);

            if (tempDirectory.Exists)
            {
                logger?.Invoke(string.Format("Deleting temp directory {0}", tempDirectory.FullName));
                tempDirectory.Delete(true);
            }

            tempDirectory.Refresh();
            logger?.Invoke(string.Format("Creating temp directory {0}", tempDirectory.FullName));
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
                                      "Microsoft.CodeAnalysis.VisualBasic.dll",
                                      "Microsoft.Build.Tasks.CodeAnalysis.dll",
                                      "VBCSCompiler.exe",
                                      "web.debug.config",
                                      "web.release.config"
                                  };
            var bannedDirectories = new List<string> {"obj", "node_modules", "bower_components"};

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

            logger?.Invoke(string.Format("Copied {0} items", itemsCopied));
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
        static int GetAvailableHttpsPort(Action<string> logger, params int[] exclusions)
        {
            var range = new PortPoolRange(44330, 100);

            var excluded = new List<int>();

            if (exclusions != null && exclusions.Any())
            {
                excluded.AddRange(exclusions);
            }

            int availableHttpsPort = TcpHelper.GetAvailablePort(range, excluded);

            logger?.Invoke($"Got dynamic https port {availableHttpsPort}");

            return availableHttpsPort;
        }
    }
}