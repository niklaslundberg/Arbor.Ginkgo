using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.Ginkgo.Tests.Integration.WebApp.Controllers
{
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