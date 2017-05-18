using System;
using System.IO;
using System.Linq;
using Machine.Specifications;

namespace Arbor.Ginkgo.Tests.Integration
{
    public class when_copying_files
    {
        private static IisExpress iis;

        private static Path websitePath;

        private static Path templatePath;

        private static Path tempPath;

        private Cleanup after = () =>
        {
            using (iis)
            {
            }

            if (tempPath != null)
            {
                new DirectoryInfo(tempPath.FullName).DeleteRecursive();
            }
        };

        private Establish context = () =>
        {
            string sourceRoot = VcsTestPathHelper.FindVcsRootPath();

            websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

            templatePath = Path.Combine(sourceRoot, "source", "applicationHost.config");

            tempPath = Path.Combine(System.IO.Path.GetTempPath(), "Arbor.Ginkgo", Guid.NewGuid().ToString());
        };

        private Because of =
            () =>
            {
                iis = IisHelper.StartWebsiteAsync(websitePath,
                    templatePath,
                    tempPath: tempPath.FullName,
                    ignoreSiteRemovalErrors: true,
                    removeSiteOnExit: false).Result;
            };

        private It should_not_contain_a_banned_directory =
            () =>
            {
                var directoryInfo = new DirectoryInfo(tempPath.FullName);
                directoryInfo.GetFiles("*.*", SearchOption.AllDirectories)
                    .Select(file => file.Name)
                    .ToList()
                    .ShouldNotContain("Microsoft.CodeAnalysis.Analyzers.dll");

                PrintDirectory(directoryInfo);
            };

        private static void PrintDirectory(DirectoryInfo directoryInfo)
        {
            Console.WriteLine(directoryInfo.FullName);

            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
            {
                Console.WriteLine(fileInfo.FullName);
            }

            foreach (DirectoryInfo subDir in directoryInfo.GetDirectories())
            {
                PrintDirectory(subDir);
            }
        }
    }
}