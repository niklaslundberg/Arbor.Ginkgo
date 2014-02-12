using System.Configuration;
using System.Web.Http;

namespace Arbor.Ginkgo.Tests.Integration.WebApp.Controllers
{
    public class TestController : ApiController
    {
        public object Get()
        {
            return new {Configuration = ConfigurationManager.AppSettings["Configuration"]};
        }
    }
}