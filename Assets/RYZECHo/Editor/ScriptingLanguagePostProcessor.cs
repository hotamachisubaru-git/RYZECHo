using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace RYZECHo.Editor
{
    public class ScriptingLanguagePostProcessor : AssetPostprocessor
    {
        static string OnGeneratedCSProject(string path, string content)
        {
            var fileName = Path.GetFileName(path);
            Debug.Log($"[RYZECHo] OnGeneratedCSProject called: {fileName}");

            var doc = XDocument.Parse(content);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var langVersions = doc.Descendants(ns + "LangVersion").ToList();
            foreach (var lv in langVersions)
            {
                var oldVal = lv.Value;
                lv.Value = "12.0";
                Debug.Log($"[RYZECHo] LangVersion changed: {oldVal} -> {lv.Value} in {fileName}");
            }

            return doc.ToString();
        }
    }
}
