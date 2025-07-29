using System;
using System.Collections.Generic;
using UniGame.Drawers;
using UnityEngine;


[CreateAssetMenu(menuName = "UniGame/Examples/Drawers/DemoDrawersAsset", fileName = "DemoDrawersAsset")]
public class UniDrawersDemoAsset : ScriptableObject
{
    [ValueDropdown(nameof(GetDropdownValues))]
    public string valueDropdown = "Default Value";
    
    [ValueDropdown(nameof(GetStaticDropdownValues))]
    public string valueStaticDropdown = "Default Value";

    [ValueDropdown(nameof(GetDropdownIntValues))]
    public string valueIntDropdown = "NONE";
    
    [ValueDropdown(nameof(GetDropdownIntValues),label:"label")]
    public string valueLabelDropdown = "NONE";
    
    public DemoDrawerClass demoDrawerClass = new();
    
    
    public IEnumerable<DemoLabelValue> GetDropdownLabelValues()
    {
        yield return new DemoLabelValue(){ value = "", label = "label1"};
        yield return new DemoLabelValue(){ value = "", label = "label2"};
        yield return new DemoLabelValue(){ value = "", label = "label3"};
    }
    
    public IEnumerable<int> GetDropdownIntValues()
    {
        yield return 1;
        yield return 55;
        yield return 66;
    }
    
    public IEnumerable<string> GetDropdownValues()
    {
        yield return "Default Value";
        yield return "Value 1";
        yield return "Value 2";
    }
    
    public static IEnumerable<string> GetStaticDropdownValues()
    {
        yield return "STATIC 1";
        yield return "STATIC 1";
        yield return "STATIC 12";
    }
}

[Serializable]
public class DemoDrawerClass
{
    [ValueDropdown(nameof(GetClassDropdownValues))]
    public string valueDropdown = "Default Value";
    
    public IEnumerable<string> GetClassDropdownValues()
    {
        yield return "Default Value";
        yield return "Value 1";
        yield return "Value 2";
        yield return "Value 3";
        yield return "Value 4";
        yield return "Value 5";
    }
}

[Serializable]
public class DemoLabelValue
{
    public string label;
    public string value;
}
