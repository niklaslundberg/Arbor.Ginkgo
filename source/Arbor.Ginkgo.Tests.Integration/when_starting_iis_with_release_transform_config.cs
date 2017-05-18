using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Machine.Specifications;

namespace Arbor.Ginkgo.Tests.Integration
{
    public class when_starting_iis_with_release_transform_config
    {
        static IisExpress iis;
        static HttpResponseMessage result;

        Cleanup after = () =>
        {
            using (iis)
            {
            }

            if (iis.WebsitePath != null)
            {
                new DirectoryInfo(iis.WebsitePath.FullName).DeleteRecursive();
            }
        };

        Establish context = () =>
        {
            httpPort = 55443;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            string sourceRoot = VcsTestPathHelper.FindVcsRootPath();

            Path websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

            Path templatePath = Path.Combine(sourceRoot, "source", "applicationHost.config");

            Task<IisExpress> startWebsite = IisHelper.StartWebsiteAsync(websitePath, templatePath,
                path => Console.WriteLine("Using website folder " + path), transformConfiguration: "release", httpPort: httpPort, ignoreSiteRemovalErrors: true);

            iis = startWebsite.Result;
        };

        Because of = () =>
        {
            using (var httpClient = new HttpClient())
            {
                result = httpClient.GetAsync($"http://localhost:{iis.Port}/api/test").Result;
            }

            Console.WriteLine(result.StatusCode + " " + result.Content.ReadAsStringAsync().Result);
        };

        It should_return_success_status_code = () => result.IsSuccessStatusCode.ShouldBeTrue();
        static int httpPort;
    }
}