using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
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
                EnvironmentVariables = Environment.GetEnvironmentVariables().ToStringArray()
            };

            return data;
        }
    }

    public static class DictionaryExtensions
    {
        public static KeyValuePair<string, string>[] ToStringArray(this IDictionary dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            IEnumerable<object> keys = dictionary.Keys.OfType<object>();

            return keys
                .Select(key => new KeyValuePair<string, string>(key.ToString(), dictionary[key].ToString()))
                .OrderBy(item => item.Key)
                .ToArray();
        }
    }
}