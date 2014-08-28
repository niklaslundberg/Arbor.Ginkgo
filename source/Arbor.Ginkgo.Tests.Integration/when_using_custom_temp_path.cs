﻿using System.IO;
using Arbor.Aesculus.Core;
using Machine.Specifications;

namespace Arbor.Ginkgo.Tests.Integration
{
    public class when_using_custom_temp_path
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
            string sourceRoot = VcsPathHelper.FindVcsRootPath();

            websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

            templatePath = Path.Combine(sourceRoot, "source", "applicationHost.config");

            tempPath = Path.Combine(System.IO.Path.GetTempPath(), "Arbor.Ginkgo");
        };

        Because of =
            () => { iis = IisHelper.StartWebsiteAsync(websitePath, templatePath, tempPath: tempPath.FullName).Result; };

        It should_have_created_the_temp_path = () => Directory.Exists(iis.WebsitePath.FullName);
    }
}