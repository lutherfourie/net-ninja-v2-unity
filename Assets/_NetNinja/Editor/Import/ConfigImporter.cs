using System.Collections.Generic;
using System.IO;
using NetNinja.Config;
using NetNinja.Core;
using NetNinja.Core.State;
using UnityEditor;
using UnityEngine;

namespace NetNinja.Editor.Import
{
    /// <summary>
    /// JSON (config/default.json) → NetNinjaConfigSO.
    /// Parity-gated keys come from CoreConfig defaults (same literals as golden export).
    /// Do not re-type parity doubles in the window.
    /// </summary>
    public static class ConfigImporter
    {
        public static NetNinjaConfigSO ImportFromJson(
            string jsonPath,
            string assetPath = "Assets/_NetNinja/Config/NetNinjaConfig.asset")
        {
            // Authority for parity keys: CoreConfig.CreateDefault() (generated from config/default.json).
            // The JSON path is validated present so CI/export prerequisites stay honest.
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Missing golden config JSON", jsonPath);

            var cfg = CoreConfig.CreateDefault();
            var so = ScriptableObject.CreateInstance<NetNinjaConfigSO>();
            so.keys = new List<KeyEntry>();
            foreach (var key in cfg.SortedKeys())
            {
                var raw = cfg.Raw(key);
                double asDouble;
                if (raw is double d) asDouble = d;
                else if (raw is bool b) asDouble = b ? 1 : 0;
                else if (raw is string)
                    continue; // string keys (policy/mode) stay on CoreConfig defaults at flatten time
                else
                    asDouble = System.Convert.ToDouble(raw);

                so.keys.Add(new KeyEntry { key = key, value = asDouble });
            }

            so.ConfigHashBadge = FnvStateHasher.HashConfig(cfg);

            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var existing = AssetDatabase.LoadAssetAtPath<NetNinjaConfigSO>(assetPath);
            if (existing != null)
            {
                existing.keys = so.keys;
                existing.ConfigHashBadge = so.ConfigHashBadge;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AssetDatabase.CreateAsset(so, assetPath);
            AssetDatabase.SaveAssets();
            return so;
        }
    }
}
