using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Machine.Specifications;

namespace Arbor.Ginkgo.Tests.Integration
{
    public class when_using_custom_host_name
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

            if (tempPath != null)
            {
                try
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(200));
                    new DirectoryInfo(tempPath.FullName).DeleteRecursive();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        };

        Establish context = () =>
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            string sourceRoot = VcsTestPathHelper.FindVcsRootPath();

            websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

            templatePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration", "applicationHost.config");

            tempPath = Path.Combine(System.IO.Path.GetTempPath(), $"Arbor.Ginkgo_{Guid.NewGuid()}");
        };

        Because of = () =>
            {
                customHostName = "iisexpresstest.local";
                var environmentVariables = new Dictionary<string, string> {{"TEST", "ABC"}};
                iis = IisHelper.StartWebsiteAsync(
                    websitePath,
                    templatePath,
                    tempPath: tempPath.FullName,
                    customHostName: customHostName,
                    onCopiedWebsite: OnCopiedWebsite,
                    httpsPort: 44435,
                    httpPort: 55557,
                    httpsEnabled: true,
                    environmentVariables: environmentVariables,
                    ignoreSiteRemovalErrors: true,
                    logger: Console.WriteLine).Result;
            };

        static void OnCopiedWebsite(Path path)
        {

        }

        private It should_not_be_accessible_via_http_localhost = () =>
        {
            using (var client = new HttpClient())
            {
                string requestUri = $"http://localhost:{iis.Port.ToString(CultureInfo.InvariantCulture)}/api/test";

                Console.WriteLine(requestUri);

                HttpResponseMessage response = client.GetAsync(requestUri).Result;

                    Console.WriteLine(response);

                response.StatusCode.ShouldNotEqual(HttpStatusCode.OK);

            }
        };

        private It should_not_be_accessible_via_https_localhost = () =>
        {
            using (var client = new HttpClient())
            {
                string requestUri = $"https://localhost:{iis.HttpsPort.ToString(CultureInfo.InvariantCulture)}/api/test";

                Console.WriteLine(requestUri);

                Exception exception = Catch.Exception(() =>
                {
                    using (HttpResponseMessage response = client.GetAsync(requestUri).Result)
                    {
                        Console.WriteLine(response);

                        response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
                    }
                });

                Console.WriteLine(exception);
                (exception.InnerException as HttpRequestException).ShouldNotBeNull();
            }
        };

        It should_be_accessible_via_http_custom_host_name = () =>
        {
            using (var client = new HttpClient())
            {
                string requestUri = $"http://{customHostName}:{iis.Port.ToString(CultureInfo.InvariantCulture)}/api/test";

                Console.WriteLine(requestUri);

                HttpResponseMessage response = client.GetAsync(requestUri).Result;

                Console.WriteLine(response);
                Console.WriteLine(response?.Content.ReadAsStringAsync().Result);

                response?.StatusCode.ShouldEqual(HttpStatusCode.OK);
            }
        };

        It should_be_accessible_via_https_custom_host_name = () =>
        {
            using (var client = new HttpClient())
            {
                string requestUri =
                    $"https://{customHostName}:{iis.HttpsPort.ToString(CultureInfo.InvariantCulture)}/api/test";

                Console.WriteLine(requestUri);

                HttpResponseMessage response = client.GetAsync(requestUri).Result;

                response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            }
        };

        static string customHostName;
    }
}