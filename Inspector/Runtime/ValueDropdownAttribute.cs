namespace UniGame.Inspector
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    public class ValueDropdownAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public string LabelName { get; }

        public ValueDropdownAttribute(string methodName,string label = "") : base(true)
        {
            MethodName = methodName;
            LabelName = label;
        }
    }
}