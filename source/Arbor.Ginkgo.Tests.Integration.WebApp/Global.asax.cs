using System.Web;
using System.Web.Http;

namespace Arbor.Ginkgo.Tests.Integration.WebApp
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}