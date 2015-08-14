using System;
using System.IO;
using System.Linq;

using Machine.Specifications;

namespace Arbor.Ginkgo.Tests.Integration
{
    public class when_copying_files
    {
        static IisExpress iis;

        static Path websitePath;

        static Path templatePath;

        static Path tempPath;

        Cleanup after = () =>
            {
                using (iis)
                {
                }

                if (Directory.Exists(tempPath.FullName))
                {
                    Directory.Delete(tempPath.FullName, recursive: true);
                }
            };

        Establish context = () =>
            {
                string sourceRoot = VcsTestPathHelper.FindVcsRootPath();

                websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

                templatePath = Path.Combine(sourceRoot, "source", "applicationHost.config");

                tempPath = Path.Combine(System.IO.Path.GetTempPath(), "Arbor.Ginkgo", Guid.NewGuid().ToString());
            };

        Because of =
            () => { iis = IisHelper.StartWebsiteAsync(websitePath, templatePath, tempPath: tempPath.FullName).Result; };

        It should_not_contain_a_banned_directory =
            () =>
                {
                    var directoryInfo = new DirectoryInfo(tempPath.FullName);
                    directoryInfo.GetFiles()
                        .Select(file => file.Name)
                        .ToList()
                        .ShouldNotContain("Microsoft.CodeAnalysis.Analyzers.dll");

                    PrintDirectory(directoryInfo);
                };

        static void PrintDirectory(DirectoryInfo directoryInfo)
        {
            Console.WriteLine(directoryInfo.FullName);

            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                Console.WriteLine(fileInfo.FullName);
            }

            foreach (var subDir in directoryInfo.GetDirectories())
            {
                PrintDirectory(subDir);
            }
        }
    }
}