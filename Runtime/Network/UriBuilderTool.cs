namespace UniModules.Runtime.Network
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Runtime.CompilerServices;
    using System.Text;
    
    using UnityEngine;
    using UnityEngine.Networking;
    
#if NET_STANDARD
     using System.Web;
#endif

    public static class UriBuilderTool
    {
        private static readonly Dictionary<int, string> _urlsCache = new();
        private static readonly List<string> _queryParameters = new();
        private static readonly Dictionary<string,string> _queryKeyValues = new();
        private static readonly Dictionary<string, UriBuilder> _builders = new(8);
        
#if UNITY_EDITOR
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            _builders.Clear();
            _urlsCache.Clear();
        }
        
#endif

        public static NameValueCollection ParseQueryString(this string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }

        public static NameValueCollection ParseQueryString(this string query, Encoding encoding)
        {
#if NET_STANDARD
            return HttpUtility.ParseQueryString(query, encoding);
#endif
            
            var result = new NameValueCollection();
            var queryLength = query.Length;
            var namePos = query.StartsWith('?') ? 1 : 0;
            
            if (queryLength == namePos)
                return result;

            while (namePos <= queryLength)
            {
                int valuePos = -1, valueEnd = -1;
                for (var q = namePos; q < queryLength; q++)
                {
                    if (valuePos == -1 && query[q] == '=')
                    {
                        valuePos = q + 1;
                    }
                    else if (query[q] == '&')
                    {
                        valueEnd = q;
                        break;
                    }
                }

                string? name;
                
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = UnityWebRequest.UnEscapeURL(query.Substring(namePos, valuePos - namePos - 1), encoding);
                }

                if (valueEnd < 0)
                {
                    valueEnd = query.Length;
                }

                namePos = valueEnd + 1;
                string value = UnityWebRequest.UnEscapeURL(query.Substring(valuePos, valueEnd - valuePos), encoding);
                result.Add(name, value);
            }

            return result;
        }
        
        public static UriBuilder GetUriBuilder(string url)
        {
            // if (_builders.TryGetValue(url, out var builder))
            //     return builder;
            
            var builder = new UriBuilder(url);
            _builders[url] = builder;
            return builder;
        }
        
        public static string SetUrlQueryParameters(this string url, Dictionary<string, string> parameters)
        {
            if (parameters is not { Count: > 0 }) return url;
            
            var uriBuilder = new UriBuilder(url);
            uriBuilder = SetUrlQueryParameters(uriBuilder, parameters);
            return uriBuilder.ToString();
        }
        
        public static string AddUrlQueryParameters(this string url, Dictionary<string, string> parameters)
        {
            if (parameters is not { Count: > 0 }) return url;
            
            var uriBuilder = new UriBuilder(url);
            uriBuilder = AddUrlQueryParameters(uriBuilder, parameters);
            return uriBuilder.ToString();
        }
        
        public static UriBuilder AddUrlQueryParameters(this UriBuilder uriBuilder, Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return uriBuilder;
            var query = BuildQueryString(parameters);
            var builderQuery = uriBuilder.Query;
            
            uriBuilder.Query = !string.IsNullOrEmpty(builderQuery) 
                ? $"{builderQuery.TrimStart('?')}&{query}" 
                // Set the new query if there was no existing query
                : query;
            
            return uriBuilder;
        }
        
        public static UriBuilder SetUrlQueryParameters(this UriBuilder uriBuilder, IReadOnlyDictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return uriBuilder;
            var query = BuildQueryString(parameters);
            uriBuilder.Query = query;
            return uriBuilder;
        }
        
        public static UriBuilder SetUrlQueryParameters(this UriBuilder uriBuilder,NameValueCollection parameters)
        {
            if (parameters == null || parameters.Count == 0) return uriBuilder;
            _queryKeyValues.Clear();
            
            foreach (var key in parameters.AllKeys)
                _queryKeyValues[key] = parameters[key];
            
            uriBuilder = SetUrlQueryParameters(uriBuilder,_queryKeyValues);
            return uriBuilder;
        }
        
        public static UriBuilder AddUrlQueryParameters(this UriBuilder uriBuilder,NameValueCollection parameters)
        {
            if (parameters == null || parameters.Count == 0) return uriBuilder;
            _queryKeyValues.Clear();
            
            foreach (var key in parameters.AllKeys)
                _queryKeyValues[key] = parameters[key];
            
            uriBuilder = AddUrlQueryParameters(uriBuilder,_queryKeyValues);
            return uriBuilder;
        }


        public static string BuildQueryString(this IReadOnlyDictionary<string, string> parameters)
        {
            if(parameters == null || parameters.Count == 0)
                return string.Empty;
            
            _queryParameters.Clear();
            var query = string.Empty;
            
            foreach (var parameter in parameters)
                _queryParameters.Add(BuildQueryString(parameter.Key, parameter.Value));
            
            // Combine existing query with new parameters
            var additionalQuery = string.Join("&", _queryParameters);
            return additionalQuery;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string BuildQueryString(this string key, string value)
        {
            return $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
        }
        
        public static UriBuilder AddUrlQueryParameter(this UriBuilder uriBuilder, string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return uriBuilder;
            
            var additionalQuery = BuildQueryString(key, value);
            // Append new parameters to the existing query
            uriBuilder.Query = !string.IsNullOrEmpty(uriBuilder.Query) 
                ? $"{uriBuilder.Query.TrimStart('?')}&{additionalQuery}" 
                // Set the new query if there was no existing query
                : additionalQuery;
            return uriBuilder;
        }
        
        public static string AddUrlQueryParameter(this string url, string key, string value)
        {
            var uriBuilder = new UriBuilder(url);
            var additionalQuery = BuildQueryString(key, value);
            // Append new parameters to the existing query
            uriBuilder.Query = !string.IsNullOrEmpty(uriBuilder.Query) 
                ? $"{uriBuilder.Query.TrimStart('?')}&{additionalQuery}" 
                // Set the new query if there was no existing query
                : additionalQuery;
            return uriBuilder.ToString();
        }
        
        public static string MergeUrl(this string serverUrl,string path)
        {
            var hash = HashCode.Combine(serverUrl, path);
            if (_urlsCache.TryGetValue(hash, out var url))
                return url;

            path = string.IsNullOrEmpty(path) ? string.Empty : path;

            var uriBuilder = GetUriBuilder(serverUrl);

            var basePath = uriBuilder.Path?.TrimEnd('/') ?? "";
            var appendPath = path.TrimStart('/');
            uriBuilder.Path = $"{basePath}/{appendPath}";

            var result = uriBuilder.Uri.ToString();
            _urlsCache[hash] = result;

            return result;
        }
    }
}