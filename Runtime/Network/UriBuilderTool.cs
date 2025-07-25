﻿namespace UniModules.Runtime.Network
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;
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
        
                #region url pattern updater

        

        
        public static Regex UrlPatternRegex = new Regex("{([a-zA-Z0-9_]+)}");
        
        public static BindingFlags UrlBindingFlags = BindingFlags.Public | BindingFlags.Instance 
                                                                         | BindingFlags.NonPublic 
                                                                         | BindingFlags.IgnoreCase;
        
        public static string UpdateUrlPattern(this string url,object source)
        {
            if(source == null) return url;
            
            var matches = UrlPatternRegex.Matches(url);
            var result = url;
            
            foreach (Match match in matches)
            {
                result = UpdatePattern(result,source,match);
            }

            return result;
        }
        
        public static string UpdatePattern(string url,object source,Match match)
        {
            var matchValue = match.Value;
            var thisType = source.GetType();
            var result = url;
            if(match.Groups.Count < 2) return result;
        
            var group = match.Groups[1];
        
            var value = group.Value;
            var field = thisType.GetField(value, UrlBindingFlags);
            var replaceValue = string.Empty;
            
            if (field != null)
            {
                var fieldValue = field.GetValue(source);
                if (fieldValue != null)
                    replaceValue = fieldValue.ToString();
            }
            
            var property = thisType.GetProperty(value, UrlBindingFlags);
            if (property != null)
            {
                var propertyValue = property.GetValue(source);
                if (propertyValue != null)
                    replaceValue = propertyValue.ToString();
            }
            
            result = result.Replace(matchValue, replaceValue);

            return result;
        }
        
        
        #endregion

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
        
        public static UriBuilder CreateUriBuilder(string host, int port,
            string schema = "http", string path = "")
        {
            var builder = new UriBuilder(host)
            {
                Path = path,
                Port = port,
                Scheme = schema
            };
            return builder;
        }
        
        public static UriBuilder GetUriBuilder(string url)
        {
            var builder = new UriBuilder(url);
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
            if(_urlsCache.TryGetValue(hash, out var url))
                return url;

            var result = MergeUrlNonCached(serverUrl, path);
            
            _urlsCache[hash] = result;
            return result;
        }
        
        public static string MergeUrlNonCached(this string serverUrl, string path)
        {
            path = string.IsNullOrEmpty(path) ? string.Empty : path;

            var uriBuilder = GetUriBuilder(serverUrl);
            
            // Убираем лишние слэши и объединяем
            var currentPath = uriBuilder.Path.TrimEnd('/');
            var addPath = path.TrimStart('/');
            
            uriBuilder.Path = $"{currentPath}/{addPath}";
            
            var result = uriBuilder.Uri.ToString();
            return result;
        }
    }
}