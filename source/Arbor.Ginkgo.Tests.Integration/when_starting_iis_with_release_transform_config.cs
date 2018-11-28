using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Machine.Specifications;
using Newtonsoft.Json;

namespace Arbor.Ginkgo.Tests.Integration
{
    public class when_starting_iis_with_release_transform_config
    {
        private static IisExpress iis;
        private static HttpResponseMessage result;

        private static int httpPort;
        private static Path _tempPath;

        private Cleanup after = () =>
        {
            using (iis)
            {
            }

            if (_tempPath != null)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
                new DirectoryInfo(_tempPath.FullName).DeleteRecursive();
            }
        };

        private Establish context = () =>
        {
            httpPort = 55443;

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            string sourceRoot = VcsTestPathHelper.FindVcsRootPath();

            Path websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

            Path templatePath = Path.Combine(sourceRoot, "source", "applicationHost.config");

            _tempPath = Path.Combine(System.IO.Path.GetTempPath(), $"Arbor.Ginkgo_{Guid.NewGuid()}");

            Task<IisExpress> startWebsite = IisHelper.StartWebsiteAsync(websitePath,
                templatePath,
                path => Console.WriteLine($"Using website folder {path}"),
                transformConfiguration: "release",
                httpPort: httpPort,
                ignoreSiteRemovalErrors: true,
                logger: Console.WriteLine,
                tempPath: _tempPath.FullName);

            iis = startWebsite.Result;
        };

        private Because of = () =>
        {
            using (var httpClient = new HttpClient())
            {
                result = httpClient.GetAsync($"http://localhost:{iis.Port}/api/test").Result;
            }

            string body = result.Content.ReadAsStringAsync().Result;

            Console.WriteLine(body);

            var deserializeAnonymousType = JsonConvert.DeserializeAnonymousType(body,
                new
                {
                    Configuration = "",
                    EnvironmentVariables = Array.Empty<KeyValuePair<string, string>>(),
                    CurrentDirectory = string.Empty
                });

            deserializeAnonymousType.Configuration.ShouldEqual("Release");

            Console.WriteLine(result.StatusCode + " " + body);
        };

        private It should_return_success_status_code = () => result.IsSuccessStatusCode.ShouldBeTrue();
    }
}