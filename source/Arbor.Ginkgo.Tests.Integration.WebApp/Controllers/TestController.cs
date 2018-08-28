using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace Arbor.Ginkgo.Tests.Integration.WebApp.Controllers
{
    public class TestController : ApiController
    {
        [Route("~/")]
        [Route("~/api/test")]
        public object Get()
        {
            KeyValuePair<string, string>[] sortedEnvironmentVariables = Environment.GetEnvironmentVariables()
                .ToStringArray()
                .OrderBy(pair => pair.Key)
                .ToArray();

            var data = new
            {
                Configuration = ConfigurationManager.AppSettings["Configuration"],
                EnvironmentVariables = sortedEnvironmentVariables,
                CurrentDirectory = Directory.GetCurrentDirectory()
            };

            return data;
        }
    }
}