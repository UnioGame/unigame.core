namespace UniGame.Inspector
{
    using System;
    using System.Collections;
    using System.Reflection;
    using UnityEditor;

    public static class Extensions
    {
        public static readonly BindingFlags BindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                                                           BindingFlags.Instance | BindingFlags.IgnoreCase;
        public const string ArrayDataPrefix = "Array.data[";
        
        public static object GetParentInstance(this SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath;
            var elements = path.Split('.');

            for (var i = 0; i < elements.Length - 1; i++) // -1, чтобы получить родителя, не само поле
            {
                var element = elements[i];

                // Если поле - массив, обрабатывай отдельно
                if (element.Contains(ArrayDataPrefix))
                {
                    var arrayField = element.Substring(0, element.IndexOf('['));
                    var index = int.Parse(element.Substring(element.IndexOf('[')).Replace("[", "").Replace("]", "").Replace("data", ""));
                    if (GetFieldValue(obj, arrayField) is IList list && index < list.Count)
                        obj = list[index];
                }
                else
                {
                    obj = GetFieldValue(obj, element);
                }
            }

            return obj;
        }

        public static object GetValue(this object value, string source)
        {
            var type = value.GetType();
            return GetValue(value, type, source);
        }

        public static object GetValue(this object value,Type type, string source)
        {
            if (value == null) return string.Empty;
            
            if (string.IsNullOrEmpty(source))
                return value.ToString();

            if(type.IsPrimitive) return value;
            if(type == typeof(string)) return value;
            
            var property = type.GetProperty(source, BindingFlags);
            if (property != null)
            {
                return property.GetValue(value, null);
            }
            var field = type.GetField(source, BindingFlags);
            if (field != null)
            {
                return field.GetValue(value);
            }
            var method = type.GetMethod(source, BindingFlags);
            if (method != null)
            {
                return method.Invoke(value, null);
            }
            return string.Empty;
        }

        public static object GetFieldValue(object obj, string fieldName)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(obj);
        }

    }
}