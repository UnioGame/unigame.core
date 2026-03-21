namespace Editor.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEditor.Build;

    public class EditorSettingsUtility
    {
        private const string DefinesSeparator = ";";

        public static string[] GetDefines()
        {
            var activeBuildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedGroup = NamedBuildTarget.FromBuildTargetGroup(activeBuildGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedGroup, out var symbolsValue);
            return symbolsValue;
        }
        
        public static void ApplyDefines(List<string> addKeys, List<string> removeKeys, string definesValue = "")
        {
            var activeBuildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedGroup = NamedBuildTarget.FromBuildTargetGroup(activeBuildGroup);
            var symbolsValue = GetDefines();

            var origin = symbolsValue.ToArray();
            var symbols = symbolsValue;
            var buildDefines = definesValue.Split(new[] { DefinesSeparator }, StringSplitOptions.None);

            var defines = new List<string>(symbols.Length + buildDefines.Length + addKeys.Count);

            defines.AddRange(symbols);
            defines.AddRange(buildDefines);
            defines.AddRange(addKeys);
            defines.RemoveAll(removeKeys.Contains);
            defines.RemoveAll(string.IsNullOrEmpty);

            defines = defines.Distinct().ToList();

            if (defines.Count == 0) return;

            if (origin.All(defines.Contains) && defines.All(origin.Contains))
                return;

            var definesBuilder = new StringBuilder(300);

            foreach (var define in defines)
            {
                definesBuilder.Append(define);
                definesBuilder.Append(DefinesSeparator);
            }

            PlayerSettings.SetScriptingDefineSymbols(namedGroup, definesBuilder.ToString());
        }
    }
}