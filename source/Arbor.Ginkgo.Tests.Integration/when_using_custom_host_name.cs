using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using Arbor.Aesculus.Core;
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

            if (Directory.Exists(tempPath.FullName))
            {
                Directory.Delete(tempPath.FullName, recursive: true);
            }
        };

        Establish context = () =>
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            string sourceRoot = VcsPathHelper.FindVcsRootPath();

            websitePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration.WebApp");

            templatePath = Path.Combine(sourceRoot, "source", "Arbor.Ginkgo.Tests.Integration", "applicationHost.config");

            tempPath = Path.Combine(System.IO.Path.GetTempPath(), "Arbor.Ginkgo", Guid.NewGuid().ToString());
        };

        Because of =
            () =>
            {
                customHostName = "iisexpresstest.local";
                iis = IisHelper.StartWebsiteAsync(websitePath, templatePath, tempPath: tempPath.FullName, customHostName: customHostName, onCopiedWebsite: OnCopiedWebsite, sslTcpPort: 44300, tcpPort:55557, httpsEnabled:true, customHostNameSslTcpPort:44355).Result;
            };

        static void OnCopiedWebsite(Path path)
        {
            
        }

        It should_be_accessible_via_http_localhost = () =>
        {
            using (var client = new HttpClient())
            {
                var requestUri = string.Format("http://localhost:{0}/api/test", iis.Port.ToString(CultureInfo.InvariantCulture));

                Console.WriteLine(requestUri);

                var response = client.GetAsync(requestUri).Result;

                response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            }
        };

        It should_be_accessible_via_https_localhost = () =>
        {
            using (var client = new HttpClient())
            {
                var requestUri = string.Format("https://localhost:{0}/api/test", iis.HttpsPort.ToString(CultureInfo.InvariantCulture));

                Console.WriteLine(requestUri);

                var response = client.GetAsync(requestUri).Result;

                response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            }
        };

        It should_be_accessible_via_http_custom_host_name = () =>
        {
            using (var client = new HttpClient())
            {
                var requestUri = string.Format("http://{0}:{1}/api/test", customHostName, iis.Port.ToString(CultureInfo.InvariantCulture));
                
                Console.WriteLine(requestUri);

                var response = client.GetAsync(requestUri).Result;

                response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            }
        };

        It should_be_accessible_via_https_custom_host_name = () =>
        {
            using (var client = new HttpClient())
            {
                var requestUri = string.Format("https://{0}:{1}/api/test", customHostName, iis.CustomHostNameHttpsPort.ToString(CultureInfo.InvariantCulture));
                
                Console.WriteLine(requestUri);
                
                var response = client.GetAsync(requestUri).Result;

                response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            }
        };

        static string customHostName;
    }
}