using System;

namespace UniGame.Editor
{
    /// <summary>
    /// Описание ресурса для генерации link.xml
    /// </summary>
    [Serializable]
    public struct LinkXmlResource
    {
        public LinkXmlResourceType Type;
        public string Value;          // Строковое значение (namespace, assembly name, type name, regex pattern, base type name)

        public static LinkXmlResource FromRegex(string regexPattern)
            => new LinkXmlResource { Type = LinkXmlResourceType.RegexPattern, Value = regexPattern };

        public static LinkXmlResource FromBaseType(Type baseType)
            => new LinkXmlResource { Type = LinkXmlResourceType.BaseType, Value = baseType.FullName ?? baseType.Name };

        public static LinkXmlResource FromBaseType(string baseTypeName)
            => new LinkXmlResource { Type = LinkXmlResourceType.BaseType, Value = baseTypeName };

        public static LinkXmlResource FromNamespace(string namespaceName)
            => new LinkXmlResource { Type = LinkXmlResourceType.Namespace, Value = namespaceName };

        public static LinkXmlResource FromAssembly(string assemblyName)
            => new LinkXmlResource { Type = LinkXmlResourceType.Assembly, Value = assemblyName };

        public static LinkXmlResource FromConcreteType(string typeName)
            => new LinkXmlResource { Type = LinkXmlResourceType.ConcreteType, Value = typeName };

        public static LinkXmlResource FromConcreteType(Type type)
            => new LinkXmlResource { Type = LinkXmlResourceType.ConcreteType, Value = type.FullName ?? type.Name };
    }
}