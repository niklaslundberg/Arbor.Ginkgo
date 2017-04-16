using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace Arbor.Ginkgo.Tests.Integration.WebApp.Controllers
{
    public class TestController : ApiController
    {
        public object Get()
        {
            var data = new
            {
                Configuration = ConfigurationManager.AppSettings["Configuration"],
                EnvironmentVariables = Environment.GetEnvironmentVariables().ToStringArray().OrderBy(pair => pair.Key).ToArray(),
                CurrentDirectory = Directory.GetCurrentDirectory()
            };

            return data;
        }
    }
}