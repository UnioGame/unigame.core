#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniGame.Editor
{
    public static class LinkXmlGenerator
{
    public const string LinkXmlPath = "Assets/link.xml";

    private const string BeginMarker = "<!-- BEGIN-AUTO-GENERATED-BY-LinkXmlNamespaceGenerator -->";
    private const string EndMarker   = "<!-- END-AUTO-GENERATED-BY-LinkXmlNamespaceGenerator -->";

    /// <summary>
    /// Генерирует секцию link.xml на основе списка ресурсов.
    /// Перезаписывает только блок между BeginMarker и EndMarker, сохраняя остальное содержимое.
    /// </summary>
    /// <param name="resources">Список ресурсов для включения в link.xml</param>
    public static void GenerateFromResources(IEnumerable<LinkXmlResource> resources)
    {
        var resourceList = (resources ?? Array.Empty<LinkXmlResource>()).ToArray();
        if (resourceList.Length == 0)
        {
            Debug.LogWarning("[LinkXmlGenerator] No resources provided for generation.");
            return;
        }

        // Проверяем, что путь к link.xml корректный
        if (string.IsNullOrEmpty(LinkXmlPath))
        {
            Debug.LogError("[LinkXmlGenerator] LinkXmlPath is null or empty.");
            return;
        }

        // Кешируем список всех сборок в домене для переиспользования
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        Debug.Log($"[LinkXmlGenerator] Processing {allAssemblies.Length} assemblies...");

        // assembly -> namespaces
        var nsMap = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        // assembly -> types
        var typeMap = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        // whole assemblies to preserve
        var wholeAssemblies = new SortedSet<string>(StringComparer.Ordinal);

        // Compile regex patterns once
        var regexPatterns = resourceList
            .Where(r => r.Type == LinkXmlResourceType.RegexPattern && !string.IsNullOrWhiteSpace(r.Value))
            .Select(r => new System.Text.RegularExpressions.Regex(r.Value, System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .ToArray();

        var namespaceResources = resourceList
            .Where(r => r.Type == LinkXmlResourceType.Namespace && !string.IsNullOrWhiteSpace(r.Value))
            .Select(r => r.Value.Trim())
            .ToHashSet(StringComparer.Ordinal);

        var assemblyResources = resourceList
            .Where(r => r.Type == LinkXmlResourceType.Assembly && !string.IsNullOrWhiteSpace(r.Value))
            .Select(r => r.Value.Trim())
            .ToHashSet(StringComparer.Ordinal);

        var concreteTypeResources = resourceList
            .Where(r => r.Type == LinkXmlResourceType.ConcreteType && !string.IsNullOrWhiteSpace(r.Value))
            .Select(r => r.Value.Trim())
            .ToHashSet(StringComparer.Ordinal);

        // Получаем все типы из всех сборок только один раз для базовых типов
        Dictionary<string, Type> allTypesByName = null;
        var baseTypeNames = resourceList
            .Where(r => r.Type == LinkXmlResourceType.BaseType && !string.IsNullOrWhiteSpace(r.Value))
            .Select(r => r.Value.Trim())
            .ToArray();

        Type[] baseTypeResources = null;
        if (baseTypeNames.Length > 0)
        {
            // Создаем словарь типов только если есть BaseType ресурсы
            allTypesByName = new Dictionary<string, Type>(StringComparer.Ordinal);
            
            foreach (var asm in allAssemblies)
            {
                if (asm == null || asm.IsDynamic || IsEditorAssemblyName(asm.GetName().Name)) continue;
                
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                catch { continue; }
                
                if (types == null) continue;
                
                foreach (var type in types)
                {
                    if (type == null) continue;
                    
                    var fullName = type.FullName;
                    var name = type.Name;
                    
                    // Добавляем по полному имени и короткому имени
                    if (!string.IsNullOrEmpty(fullName) && !allTypesByName.ContainsKey(fullName))
                        allTypesByName[fullName] = type;
                    if (!string.IsNullOrEmpty(name) && !allTypesByName.ContainsKey(name))
                        allTypesByName[name] = type;
                }
            }
            
            // Теперь находим базовые типы по именам
            baseTypeResources = baseTypeNames
                .Select(typeName => 
                {
                    // Сначала пробуем Type.GetType (для встроенных типов)
                    var type = Type.GetType(typeName);
                    if (type != null) return type;
                    
                    // Затем ищем в нашем словаре
                    allTypesByName.TryGetValue(typeName, out type);
                    return type;
                })
                .Where(t => t != null)
                .ToArray();
        }
        else
        {
            baseTypeResources = Array.Empty<Type>();
        }

        foreach (var asm in allAssemblies)
        {
            if (asm == null || asm.IsDynamic) continue;
            var asmName = asm.GetName().Name;
            if (IsEditorAssemblyName(asmName)) continue;

            // Проверяем, нужна ли вся сборка
            if (assemblyResources.Contains(asmName))
            {
                wholeAssemblies.Add(asmName);
                continue; // Пропускаем детальную обработку типов
            }

            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
            if (types == null || types.Length == 0) continue;

            foreach (var type in types)
            {
                // Пропускаем Editor типы
                if (IsEditorNamespace(type.Namespace))
                    continue;

                var typeFullName = type.FullName ?? type.Name;
                var typeNamespace = type.Namespace;
                bool shouldInclude = false;

                // 1) Проверка конкретных типов
                if (concreteTypeResources.Contains(typeFullName) || concreteTypeResources.Contains(type.Name))
                {
                    shouldInclude = true;
                }

                // 2) Проверка namespace
                if (!shouldInclude && !string.IsNullOrEmpty(typeNamespace))
                {
                    foreach (var ns in namespaceResources)
                    {
                        if (typeNamespace.Equals(ns, StringComparison.Ordinal) || 
                            typeNamespace.StartsWith(ns + ".", StringComparison.Ordinal))
                        {
                            if (!nsMap.TryGetValue(asmName, out var nsSet))
                            {
                                nsSet = new SortedSet<string>(StringComparer.Ordinal);
                                nsMap[asmName] = nsSet;
                            }
                            nsSet.Add(ns);
                            shouldInclude = true;
                            break;
                        }
                    }
                }

                // 3) Проверка базовых типов
                if (!shouldInclude && baseTypeResources.Length > 0)
                {
                    // Включаем сам базовый тип
                    foreach (var baseType in baseTypeResources)
                    {
                        if (baseType == type)
                        {
                            shouldInclude = true;
                            break;
                        }
                    }

                    // Включаем наследников (только если не generic definition)
                    if (!shouldInclude && !type.IsGenericTypeDefinition)
                    {
                        foreach (var baseType in baseTypeResources)
                        {
                            if (baseType.IsAssignableFrom(type))
                            {
                                shouldInclude = true;
                                break;
                            }
                        }
                    }
                }

                // 4) Проверка regex паттернов
                if (!shouldInclude && regexPatterns.Length > 0)
                {
                    foreach (var regex in regexPatterns)
                    {
                        if (regex.IsMatch(typeFullName))
                        {
                            shouldInclude = true;
                            break;
                        }
                    }
                }

                // Добавляем тип, если он подходит
                if (shouldInclude)
                {
                    if (!typeMap.TryGetValue(asmName, out var typeSet))
                    {
                        typeSet = new SortedSet<string>(StringComparer.Ordinal);
                        typeMap[asmName] = typeSet;
                    }
                    typeSet.Add(typeFullName);
                }
            }
        }

        // Сборка общего авто-блока
        var gen = new StringBuilder();
        gen.AppendLine(BeginMarker);
        gen.AppendLine("  <!-- DO NOT EDIT MANUALLY: this section is regenerated by LinkXmlGenerator -->");

        // Сначала целые сборки
        foreach (var asmName in wholeAssemblies)
        {
            gen.AppendLine($"  <assembly fullname=\"{Escape(asmName)}\" preserve=\"all\"/>");
        }

        // Затем частичные сборки (namespace + types)
        var allPartialAssemblies = new SortedSet<string>(
            nsMap.Keys.Concat(typeMap.Keys),
            StringComparer.Ordinal);

        foreach (var asmName in allPartialAssemblies)
        {
            // Пропускаем, если уже включена целиком
            if (wholeAssemblies.Contains(asmName)) continue;

            gen.AppendLine($"  <assembly fullname=\"{Escape(asmName)}\">");

            // Namespace'ы
            if (nsMap.TryGetValue(asmName, out var nsSet))
            {
                foreach (var ns in nsSet)
                    gen.AppendLine($"    <namespace fullname=\"{Escape(ns)}\" preserve=\"all\"/>");
            }

            // Конкретные типы
            if (typeMap.TryGetValue(asmName, out var typeSet))
            {
                foreach (var typeFullName in typeSet)
                    gen.AppendLine($"    <type fullname=\"{Escape(typeFullName)}\" preserve=\"all\"/>");
            }

            gen.AppendLine("  </assembly>");
        }

        gen.AppendLine(EndMarker);

        try
        {
            // Убеждаемся, что базовый link.xml существует перед чтением
            EnsureBaseLinkXml();

            var text = File.ReadAllText(LinkXmlPath, Encoding.UTF8);
            text = ReplaceBetween(text, BeginMarker, EndMarker, gen.ToString());

            File.WriteAllText(LinkXmlPath, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(LinkXmlPath);
            Debug.Log($"[LinkXmlGenerator] link.xml updated successfully at: {LinkXmlPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LinkXmlGenerator] Failed to update link.xml: {ex.Message}");
            throw;
        }
    }

    private static bool IsEditorAssemblyName(string asmName)
    {
        if (string.IsNullOrEmpty(asmName)) return false;
        return asmName.EndsWith(".Editor", StringComparison.Ordinal)
            || asmName.Contains(".Editor.", StringComparison.Ordinal)
            || asmName.EndsWith(".EditorTests", StringComparison.Ordinal)
            || asmName.Equals("UnityEditor", StringComparison.Ordinal);
    }

    private static bool IsEditorNamespace(string namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName)) return false;
        
        return namespaceName.StartsWith("UnityEditor", StringComparison.Ordinal)
            || namespaceName.Contains(".Editor.", StringComparison.Ordinal)
            || namespaceName.EndsWith(".Editor", StringComparison.Ordinal);
    }

    private static void EnsureBaseLinkXml()
    {
        if (!File.Exists(LinkXmlPath))
        {
            var directory = Path.GetDirectoryName(LinkXmlPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"[LinkXmlGenerator] Created directory: {directory}");
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("<linker>");
            sb.AppendLine("  <!-- You can keep your manual rules here -->");
            sb.AppendLine($"  {BeginMarker}");
            sb.AppendLine("  <!-- DO NOT EDIT MANUALLY: this section is regenerated by LinkXmlGenerator -->");
            sb.AppendLine($"  {EndMarker}");
            sb.AppendLine("</linker>");
            
            File.WriteAllText(LinkXmlPath, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(LinkXmlPath);
            Debug.Log($"[LinkXmlGenerator] Created new link.xml file at: {LinkXmlPath}");
            return;
        }

        var text = File.ReadAllText(LinkXmlPath, Encoding.UTF8);
        if (!text.Contains("<linker>"))
            throw new InvalidOperationException("link.xml must have a <linker> root element.");

        if (!text.Contains(BeginMarker) || !text.Contains(EndMarker))
        {
            int insertPos = text.LastIndexOf("</linker>", StringComparison.OrdinalIgnoreCase);
            if (insertPos < 0)
                throw new InvalidOperationException("link.xml: root <linker> not found.");

            var block = new StringBuilder();
            block.AppendLine($"  {BeginMarker}");
            block.AppendLine("  <!-- DO NOT EDIT MANUALLY: this section is regenerated by LinkXmlGenerator -->");
            block.AppendLine($"  {EndMarker}");

            text = text.Insert(insertPos, block.ToString());
            File.WriteAllText(LinkXmlPath, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(LinkXmlPath);
        }
    }

    private static string ReplaceBetween(string source, string begin, string end, string wholeReplacement)
    {
        int start = source.IndexOf(begin, StringComparison.Ordinal);
        int stop  = source.IndexOf(end,   StringComparison.Ordinal);
        
        if (start < 0 || stop < 0 || stop <= start)
        {
            // Если маркеры не найдены, вставляем перед закрывающим тегом </linker>
            int insertPos = source.LastIndexOf("</linker>", StringComparison.OrdinalIgnoreCase);
            if (insertPos < 0)
                throw new InvalidOperationException("link.xml: root <linker> not found.");

            var block = new StringBuilder();
            block.AppendLine("  " + wholeReplacement.TrimEnd());
            
            return source.Insert(insertPos, block.ToString() + Environment.NewLine);
        }

        var prefix = source.Substring(0, start);
        var suffix = source.Substring(stop + end.Length);

        return prefix + wholeReplacement + suffix;
    }

    private static string Escape(string s)
    {
        return s
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}

    // Пример вашего интерфейса/базового класса — только для меню-демо:
    public interface MyRuntimeInterface {}
}
#endif
