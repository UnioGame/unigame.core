using System;
using UnityEngine;

namespace UniGame.Editor
{
    /// <summary>
    /// Serializable wrapper for LinkXmlResource with Unity Inspector support
    /// </summary>
    [System.Serializable]
    public class LinkXmlResourceSettings
    {
        [Tooltip("Enable/disable this resource")]
        public bool Enabled = true;

        [Tooltip("Type of link XML resource")]
        public LinkXmlResourceType Type = LinkXmlResourceType.Namespace;

        [Tooltip("String value for Namespace, Assembly, ConcreteType, RegexPattern, or BaseType (full type name)")]
        public string StringValue = "";

        /// <summary>
        /// Convert this settings object to LinkXmlResource
        /// </summary>
        public LinkXmlResource? ToLinkXmlResource()
        {
            if (!Enabled) return null;

            switch (Type)
            {
                case LinkXmlResourceType.Namespace:
                    if (string.IsNullOrWhiteSpace(StringValue)) return null;
                    return LinkXmlResource.FromNamespace(StringValue.Trim());

                case LinkXmlResourceType.Assembly:
                    if (string.IsNullOrWhiteSpace(StringValue)) return null;
                    return LinkXmlResource.FromAssembly(StringValue.Trim());

                case LinkXmlResourceType.ConcreteType:
                    if (string.IsNullOrWhiteSpace(StringValue)) return null;
                    return LinkXmlResource.FromConcreteType(StringValue.Trim());

                case LinkXmlResourceType.RegexPattern:
                    if (string.IsNullOrWhiteSpace(StringValue)) return null;
                    return LinkXmlResource.FromRegex(StringValue.Trim());

                case LinkXmlResourceType.BaseType:
                    if (string.IsNullOrWhiteSpace(StringValue)) return null;
                    return LinkXmlResource.FromBaseType(StringValue.Trim());

                default:
                    return null;
            }
        }
    }
}