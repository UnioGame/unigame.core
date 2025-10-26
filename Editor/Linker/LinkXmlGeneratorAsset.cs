using System.Collections.Generic;
using UnityEngine;

namespace UniGame.Editor
{
    [CreateAssetMenu(menuName = "UniGame/Tools/Link Xml Generator Asset", fileName = "LinkXmlGenerator")]
    public class LinkXmlGeneratorAsset : ScriptableObject
    {
        [Header("Link XML Resources")]
        [Tooltip("List of resources to include in link.xml generation")]
        public List<LinkXmlResourceSettings> Resources = new List<LinkXmlResourceSettings>();

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        [ContextMenu("Generate Link Xml")]
        public void Generate()
        {
            var allResources = new List<LinkXmlResource>();

            // Add resources from the Resources list
            foreach (var resourceSetting in Resources)
            {
                if (!resourceSetting.Enabled) continue;

                var resource = resourceSetting.ToLinkXmlResource();
                if (resource.HasValue)
                {
                    allResources.Add(resource.Value);
                }
            }

            if (allResources.Count == 0)
            {
                Debug.LogWarning($"[{name}] No resources configured for link.xml generation.", this);
                return;
            }

            Debug.Log($"[{name}] Generating link.xml with {allResources.Count} resources...", this);
            LinkXmlGenerator.GenerateFromResources(allResources);
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Add Namespace Resource")]
#endif
        [ContextMenu("Add Namespace Resource")]
        public void AddNamespaceResource()
        {
            Resources.Add(new LinkXmlResourceSettings
            {
                Type = LinkXmlResourceType.Namespace,
                Enabled = true,
                StringValue = "MyProject.Core"
            });
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Add Base Type Resource")]
#endif
        [ContextMenu("Add Base Type Resource")]
        public void AddBaseTypeResource()
        {
            Resources.Add(new LinkXmlResourceSettings
            {
                Type = LinkXmlResourceType.BaseType,
                Enabled = true,
                StringValue = "UnityEngine.MonoBehaviour"
            });
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Add Regex Resource")]
#endif
        [ContextMenu("Add Regex Resource")]
        public void AddRegexResource()
        {
            Resources.Add(new LinkXmlResourceSettings
            {
                Type = LinkXmlResourceType.RegexPattern,
                Enabled = true,
                StringValue = @".*Controller$"
            });
        }
    }
}