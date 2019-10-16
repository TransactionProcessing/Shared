using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Shared.Extensions
{
    public static class StringExtensions
    {
        #region public static Boolean TryParseJson<T>(this String obj, out T result)        
        /// <summary>
        /// Tries the parse json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static Boolean TryParseJson<T>(this String obj, out T result)
        {
            try
            {
                // Validate missing fields of object
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.MissingMemberHandling = MissingMemberHandling.Error;

                result = JsonConvert.DeserializeObject<T>(obj);
                return true;
            }
            catch (JsonReaderException jrex)
            {
                result = default(T);
                return false;
            }
            catch (JsonSerializationException jsex)
            {
                result = default(T);
                return false;
            }
        }
        #endregion
    }
}
