namespace UniGame.Drawers
{
    using System.Collections;
    using UnityEditor;
    using UnityEngine.UIElements;
    using System.Reflection;

    [CustomPropertyDrawer(typeof(ValueDropdownAttribute))]
    public class ValueDropdownPropertyDrawer : PropertyDrawer
    {
        public static readonly BindingFlags BindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                                           BindingFlags.Instance | BindingFlags.IgnoreCase;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            var dropdownAttr = (ValueDropdownAttribute)attribute;
            var serializedObject = property.serializedObject;
            var target = serializedObject.targetObject;
            var parent = property.GetParentInstance();
            var targetType = parent.GetType();
            
            if (property.propertyType != SerializedPropertyType.String)
            {
                container.Add(new Label("DropdownFrom only supports string fields"));
                return container;
            }

            // Получаем метод
            var methodInfo = targetType.GetMethod(dropdownAttr.MethodName, BindingFlags);

            if (methodInfo == null)
            {
                container.Add(new Label($"Method '{dropdownAttr.MethodName}' not found"));
                return container;
            }

            var result = methodInfo.Invoke(parent, null);
            if (result is not IEnumerable enumerableResult)
            {
                container.Add(new Label($"Method '{dropdownAttr.MethodName}' must return IEnumerable<T>"));
                return container;
            }

            var activeValue = property.stringValue;
            var dropdown = new DropdownField(property.stringValue);
            var isLabelDefined = !string.IsNullOrEmpty(dropdownAttr.LabelName);
            
            dropdown.label = property.displayName;
            dropdown.choices.Clear();

            foreach (var item in enumerableResult)
            {
                var itemValue = item.GetValue(dropdownAttr.LabelName);
                var label = itemValue?.ToString();
                if(string.IsNullOrEmpty(label))
                    continue;
                dropdown.choices.Add(label);
            }

            dropdown.value = activeValue;
            
            dropdown.RegisterValueChangedCallback(evt =>
            {
                property.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();

                if (target != null)
                    EditorUtility.SetDirty(target);
            });

            container.Add(dropdown);
            
            return container;
        }
    }
}