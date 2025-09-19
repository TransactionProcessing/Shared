using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Web
{
    public class QueryStringBuilder
    {
        private Dictionary<string, (object value, Boolean alwaysInclude)> parameters = new Dictionary<String, (Object value, Boolean alwaysInclude)>();

        public QueryStringBuilder AddParameter(string key,
                                               object value) =>
            AddParameter(key, value, false);

        public QueryStringBuilder AddParameter(string key, object value, Boolean alwaysInclude)
        {
            this.parameters.Add(key, (value, alwaysInclude));
            return this;
        }

        static Dictionary<string, object> FilterDictionary(Dictionary<string, (object value, Boolean alwaysInclude)> inputDictionary)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            foreach (KeyValuePair<String, (object value, Boolean alwaysInclude)> entry in inputDictionary)
            {
                if (entry.Value.value != null && !IsDefaultValue(entry.Value.value, entry.Value.alwaysInclude))
                {
                    result.Add(entry.Key, entry.Value.value);
                }
            }

            return result;
        }

        static bool IsDefaultValue<T>(T value, Boolean alwaysInclude)
        {
            if (alwaysInclude)
                return false;

            Object? defaultValue = GetDefault(value.GetType());

            if (defaultValue == null && value.GetType() == typeof(String))
            {
                defaultValue = String.Empty;
            }
            return defaultValue.Equals(value);
        }

        public static object GetDefault(Type t)
        {
            Func<object> f = GetDefault<object>;
            return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        }

        private static T GetDefault<T>()
        {
            return default(T);
        }

        public string BuildQueryString()
        {
            Dictionary<String, Object> filtered = FilterDictionary(this.parameters);

            if (filtered.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder queryString = new();

            foreach (KeyValuePair<String, Object> kvp in filtered)
            {
                if (queryString.Length > 0)
                {
                    queryString.Append("&");
                }

                queryString.Append($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
            }

            return queryString.ToString();
        }
    }
}
