using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VibrantCode.HubQ.Web.Client
{
    internal class UrlUtils
    {
        public static string GetUrl(string url, object? parameters = null)
        {
            if (parameters != null)
            {
                return GetUrl(url, parameters: EnumeratePropertiesAsString(parameters));
            }
            else
            {
                return GetUrl(url, parameters: Enumerable.Empty<KeyValuePair<string, string>>());
            }
        }

        public static string GetUrl(string url, IEnumerable<KeyValuePair<string, string>>? parameters = null)
        {
            if (parameters != null)
            {
                var prefix = "?";
                foreach (var pair in parameters)
                {
                    url = $"{url}{prefix}{pair.Key}={pair.Value}";
                    prefix = "&";
                }
            }
            return url;
        }

        private static IEnumerable<KeyValuePair<string, string>> EnumeratePropertiesAsString(object obj)
        {
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = prop.Name;
                var value = prop.GetValue(obj);

                // The runtime is super clever about boxing Nullable<T>. If the value is null, it
                // boxes it as a null object reference, so this just works!

                if (value != null)
                {
                    yield return new KeyValuePair<string, string>(name, value.ToString());
                }
            }
        }
    }
}
