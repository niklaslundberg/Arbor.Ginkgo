using System;
using System.IO;
using System.Threading;
using Machine.Specifications;

namespace Arbor.Ginkgo.Tests.Integration
{
    public class when_using_custom_temp_path
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
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
                new DirectoryInfo(tempPath.FullName).DeleteRecursive();
            }
        };

        private Establish context = () =>
        {
            string sourceRoot = VcsTestPathHelper.FindVcsRootPath();

            websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

            templatePath = Path.Combine(sourceRoot, "source", "applicationHost.config");

            tempPath = Path.Combine(System.IO.Path.GetTempPath(), $"Arbor.Ginkgo_{Guid.NewGuid()}");
        };

        private Because of =
            () =>
            {
                iis = IisHelper.StartWebsiteAsync(
                    websitePath,
                    templatePath,
                    tempPath: tempPath.FullName,
                    ignoreSiteRemovalErrors: true,
                    removeSiteOnExit: true,
                    logger: Console.WriteLine).Result;
            };

        private It should_have_created_the_temp_path = () => Directory.Exists(iis.WebsitePath.FullName);
    }
}