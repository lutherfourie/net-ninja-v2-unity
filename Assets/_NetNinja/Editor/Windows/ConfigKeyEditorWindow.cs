using System.IO;
using NetNinja.Config;
using NetNinja.Editor.Controls;
using NetNinja.Editor.Import;
using NetNinja.Editor.Shell;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetNinja.Editor.Windows
{
    public sealed class ConfigKeyEditorWindow : EditorWindow
    {
        NetNinjaConfigSO _so;
        HashBadge _badge;
        ScrollView _list;

        [MenuItem("Net Ninja/Workstation/Config & Keys")]
        public static void Open()
        {
            var w = GetWindow<ConfigKeyEditorWindow>();
            w.titleContent = new GUIContent("Config / Keys");
            w.minSize = new Vector2(480, 360);
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.netninja.editor/Editor/Shell/WorkstationShell.uss"));
            var shell = WorkstationShell.CreateRoot("Config / Key Editor");
            root.Add(shell);

            _badge = new HashBadge();
            shell.Add(_badge);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var importBtn = new Button(ImportGolden) { text = "Import config/default.json" };
            var refreshBtn = new Button(RefreshList) { text = "Refresh" };
            var saveBtn = new Button(Save) { text = "Save SO" };
            row.Add(importBtn);
            row.Add(refreshBtn);
            row.Add(saveBtn);
            shell.Add(row);

            var soField = new ObjectField("Config SO") { objectType = typeof(NetNinjaConfigSO) };
            soField.RegisterValueChangedCallback(evt =>
            {
                _so = evt.newValue as NetNinjaConfigSO;
                RefreshList();
            });
            shell.Add(soField);

            _list = new ScrollView();
            shell.Add(_list);

            // Auto-load if present
            _so = AssetDatabase.LoadAssetAtPath<NetNinjaConfigSO>("Assets/_NetNinja/Config/NetNinjaConfig.asset");
            soField.value = _so;
            RefreshList();
        }

        void ImportGolden()
        {
            var json = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "config", "default.json"));
            if (!File.Exists(json))
            {
                EditorUtility.DisplayDialog("Config Import", "Missing " + json, "OK");
                return;
            }
            _so = ConfigImporter.ImportFromJson(json);
            _badge.SetHash(_so.ConfigHashBadge);
            RefreshList();
            Debug.Log("[NetNinja] Imported config → hash " + _so.ConfigHashBadge);
        }

        void RefreshList()
        {
            _list.Clear();
            if (_so == null) return;
            _badge.SetHash(string.IsNullOrEmpty(_so.ConfigHashBadge) ? "—" : _so.ConfigHashBadge);
            foreach (var e in _so.keys)
            {
                var kr = new KeyRow();
                kr.KeyField.value = e.key;
                kr.ValueField.value = e.value;
                var entry = e;
                kr.ValueField.RegisterValueChangedCallback(evt =>
                {
                    entry.value = evt.newValue;
                    EditorUtility.SetDirty(_so);
                });
                _list.Add(kr);
            }
        }

        void Save()
        {
            if (_so == null) return;
            EditorUtility.SetDirty(_so);
            AssetDatabase.SaveAssets();
        }
    }
}
