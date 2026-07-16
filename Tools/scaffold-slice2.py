#!/usr/bin/env python3
"""Generate Slice 2 Unity shell scaffolding (structural; no Unity batchmode)."""
from __future__ import annotations
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def write(rel: str, content: str) -> None:
    p = ROOT / rel
    p.parent.mkdir(parents=True, exist_ok=True)
    if not content.endswith("\n"):
        content += "\n"
    p.write_text(content, encoding="utf-8")


def guid_for(rel: str) -> str:
    return hashlib.md5(rel.replace("\\", "/").encode()).hexdigest()


def ensure_meta(rel: str, folder: bool = False) -> None:
    path = ROOT / rel
    meta = Path(str(path) + ".meta")
    if meta.exists():
        return
    g = guid_for(rel)
    if folder or path.is_dir():
        body = f"""fileFormatVersion: 2
guid: {g}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    else:
        body = f"""fileFormatVersion: 2
guid: {g}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta.write_text(body, encoding="utf-8")


def pkg(name: str, display: str, deps: dict | None = None) -> None:
    write(
        f"Packages/{name}/package.json",
        json.dumps(
            {
                "name": name,
                "version": "0.1.0",
                "displayName": display,
                "unity": "6000.4",
                "dependencies": deps or {},
            },
            indent=2,
        ),
    )


def asmdef(rel: str, name: str, refs: list[str], editor: bool = False, no_engine: bool = False) -> None:
    data = {
        "name": name,
        "rootNamespace": name,
        "references": refs,
        "includePlatforms": ["Editor"] if editor else [],
        "excludePlatforms": [],
        "allowUnsafeCode": False,
        "overrideReferences": False,
        "precompiledReferences": [],
        "autoReferenced": not no_engine,
        "defineConstraints": [],
        "versionDefines": [],
        "noEngineReferences": no_engine,
    }
    write(rel, json.dumps(data, indent=4))


def main() -> None:
    # --- remaining packages ---
    pkg("com.netninja.config", "Net Ninja Config", {"com.netninja.contracts": "0.1.0", "com.netninja.core": "0.1.0"})
    pkg("com.netninja.adapters", "Net Ninja Adapters", {
        "com.netninja.contracts": "0.1.0", "com.netninja.core": "0.1.0", "com.netninja.config": "0.1.0"
    })
    pkg("com.netninja.view", "Net Ninja View", {"com.netninja.contracts": "0.1.0"})
    pkg("com.netninja.composition", "Net Ninja Composition", {
        "com.netninja.contracts": "0.1.0", "com.netninja.core": "0.1.0",
        "com.netninja.config": "0.1.0", "com.netninja.adapters": "0.1.0", "com.netninja.view": "0.1.0"
    })
    pkg("com.netninja.telemetry", "Net Ninja Telemetry Export", {"com.netninja.contracts": "0.1.0", "com.netninja.adapters": "0.1.0"})
    pkg("com.netninja.editor", "Net Ninja Editor", {
        "com.netninja.contracts": "0.1.0", "com.netninja.core": "0.1.0",
        "com.netninja.config": "0.1.0", "com.netninja.adapters": "0.1.0"
    })
    pkg("com.netninja.determinism-analyzer", "Net Ninja Determinism Analyzer", {})

    # asmdefs (GUIDs will be assigned by Unity later; name refs OK for skeleton)
    asmdef("Packages/com.netninja.config/Runtime/NetNinja.Config.asmdef", "NetNinja.Config",
           ["NetNinja.Contracts", "NetNinja.Core"])
    asmdef("Packages/com.netninja.adapters/Runtime/NetNinja.Adapters.asmdef", "NetNinja.Adapters",
           ["NetNinja.Contracts", "NetNinja.Core", "NetNinja.Config",
            "Unity.InputSystem", "Unity.Addressables", "VContainer", "MessagePipe", "UniTask",
            "Unity.Newtonsoft.Json"])
    asmdef("Packages/com.netninja.view/Runtime/NetNinja.View.asmdef", "NetNinja.View",
           ["NetNinja.Contracts", "VContainer", "Unity.RenderPipelines.Universal.Runtime",
            "Unity.TextMeshPro", "R3", "UniTask"])
    asmdef("Packages/com.netninja.composition/Runtime/NetNinja.Composition.asmdef", "NetNinja.Composition",
           ["NetNinja.Contracts", "NetNinja.Core", "NetNinja.Config", "NetNinja.Adapters",
            "NetNinja.View", "VContainer", "MessagePipe", "UniTask"])
    asmdef("Packages/com.netninja.telemetry/Runtime/NetNinja.Telemetry.Export.asmdef", "NetNinja.Telemetry.Export",
           ["NetNinja.Contracts", "NetNinja.Adapters", "Unity.Newtonsoft.Json"])
    asmdef("Packages/com.netninja.editor/Editor/NetNinja.Editor.asmdef", "NetNinja.Editor",
           ["NetNinja.Contracts", "NetNinja.Core", "NetNinja.Config", "NetNinja.Adapters",
            "TriInspector", "VContainer"], editor=True)

    # Config
    write("Packages/com.netninja.config/Runtime/KeyEntry.cs", """using System;
using UnityEngine;

namespace NetNinja.Config
{
    [Serializable]
    public class KeyEntry
    {
        public string key;
        public double value;
    }
}
""")
    write("Packages/com.netninja.config/Runtime/NetNinjaConfigSO.cs", """using System.Collections.Generic;
using UnityEngine;

namespace NetNinja.Config
{
    [CreateAssetMenu(menuName = "Net Ninja/Config", fileName = "NetNinjaConfig")]
    public class NetNinjaConfigSO : ScriptableObject
    {
        public List<KeyEntry> keys = new List<KeyEntry>();
        [SerializeField] string configHashBadge = "";

        public string ConfigHashBadge
        {
            get => configHashBadge;
            set => configHashBadge = value;
        }

        public double GetDouble(string key, double codeDefault)
        {
            for (int i = 0; i < keys.Count; i++)
                if (keys[i].key == key) return keys[i].value;
            return codeDefault;
        }
    }
}
""")
    write("Packages/com.netninja.config/Runtime/KeyRegistry.cs", """namespace NetNinja.Config
{
    /// <summary>Known net.*/tempo.*/autopilot.* key names for the Config window.</summary>
    public static class KeyRegistry
    {
        public static readonly string[] Prefixes = { "net.", "tempo.", "autopilot.", "ceremony.", "spawn.", "world.", "temper.", "juggle.", "items.", "charm.", "diag.", "run.", "telegraph.", "input." };
    }
}
""")
    write("Packages/com.netninja.config/Runtime/ConfigService.cs", """namespace NetNinja.Config
{
    public sealed class ConfigService
    {
        readonly NetNinjaConfigSO _so;
        public ConfigService(NetNinjaConfigSO so) { _so = so; }
        public double GetDouble(string key, double codeDefault) => _so != null ? _so.GetDouble(key, codeDefault) : codeDefault;
    }
}
""")

    # Adapters stubs
    for name, body in {
        "SimPump.cs": """using UnityEngine;
using VContainer.Unity;
using NetNinja.Core;

namespace NetNinja.Adapters
{
    /// <summary>IFixedTickable owner of DT=1/60 sim step.</summary>
    public sealed class SimPump : IFixedTickable
    {
        readonly Sim _sim;
        public SimPump(Sim sim) { _sim = sim; }
        public void FixedTick() { /* device input applied by DeviceInputSource; then _sim.Step() */ }
    }
}
""",
        "DeviceInputSource.cs": """using NetNinja.Contracts;
using UnityEngine;

namespace NetNinja.Adapters
{
    /// <summary>Input System → IInputReader (engine-coupled production binding).</summary>
    public sealed class DeviceInputSource : IInputReader
    {
        public InputFrame Read(int tick, double simTime)
        {
            // Skeleton: zero input; wire Input System actions in a follow-up.
            return new InputFrame(0, -2.5, pointerFollow: true, tick: tick);
        }
    }
}
""",
        "ConfigFlattener.cs": """using NetNinja.Config;
using NetNinja.Core;

namespace NetNinja.Adapters
{
    public static class ConfigFlattener
    {
        public static CoreConfig Flatten(NetNinjaConfigSO so)
        {
            var cfg = CoreConfig.CreateDefault();
            if (so == null) return cfg;
            foreach (var e in so.keys)
                if (!string.IsNullOrEmpty(e.key))
                    cfg.Set(e.key, e.value);
            return cfg;
        }
    }
}
""",
        "TelemetryBridge.cs": """using NetNinja.Core.Telemetry;

namespace NetNinja.Adapters
{
    /// <summary>Drains Core journal → MessagePipe with run-context stamping (edge only).</summary>
    public sealed class TelemetryBridge
    {
        public void Drain(TelemetrySink sink)
        {
            // Skeleton: MessagePipe IPublisher registration generated later.
            _ = sink;
        }
    }
}
""",
        "StateProjector.cs": """using NetNinja.Contracts;

namespace NetNinja.Adapters
{
    public sealed class StateProjector
    {
        public StateSnapshot Last { get; private set; }
        public void Project(StateSnapshot snap) => Last = snap;
    }
}
""",
        "SweepPolicySO.cs": """using UnityEngine;
using NetNinja.Contracts;
using NetNinja.Core.SweepPolicy;

namespace NetNinja.Adapters
{
    public abstract class SweepPolicySO : ScriptableObject
    {
        public abstract ISweepPolicy Create();
    }

    [CreateAssetMenu(menuName = "Net Ninja/Sweep Policy/Fair")]
    public class FairSweepPolicySO : SweepPolicySO
    {
        public override ISweepPolicy Create() => new FairSweepPolicy();
    }
}
""",
    }.items():
        write(f"Packages/com.netninja.adapters/Runtime/{name}", body)

    # View stubs
    for name, body in {
        "OrthoCameraRig.cs": "using UnityEngine;\nnamespace NetNinja.View { public class OrthoCameraRig : MonoBehaviour { } }\n",
        "SafeAreaController.cs": "using UnityEngine;\nnamespace NetNinja.View { public class SafeAreaController : MonoBehaviour { } }\n",
        "VerletNetRendererFeature.cs": "using UnityEngine.Rendering.Universal;\nnamespace NetNinja.View { public class VerletNetRendererFeature : ScriptableRendererFeature {\n  public override void Create() {}\n  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {}\n}}\n",
        "NetFxVolume.cs": "using UnityEngine;\nnamespace NetNinja.View { public class NetFxVolume : MonoBehaviour { } }\n",
        "SnapshotToFloat.cs": "using NetNinja.Contracts;\nusing UnityEngine;\nnamespace NetNinja.View {\n  public static class SnapshotToFloat {\n    public static Vector3 ToVector3(Vec3 v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);\n  }\n}\n",
        "Hud/HudController.cs": "using UnityEngine;\nnamespace NetNinja.View.Hud { public class HudController : MonoBehaviour { } }\n",
    }.items():
        write(f"Packages/com.netninja.view/Runtime/{name}", body)

    # Composition
    for name, body in {
        "RootLifetimeScope.cs": """using VContainer;
using VContainer.Unity;

namespace NetNinja.Composition
{
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // ConfigService, telemetry bus, input backend
        }
    }
}
""",
        "GameLifetimeScope.cs": """using VContainer;
using VContainer.Unity;

namespace NetNinja.Composition
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Sim, ISweepPolicy, adapters, View as ILateTickable
        }
    }
}
""",
        "EditorToolLifetimeScope.cs": """using VContainer;
using VContainer.Unity;

namespace NetNinja.Composition
{
    public sealed class EditorToolLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder) { }
    }
}
""",
        "ViewLateTickable.cs": """using VContainer.Unity;

namespace NetNinja.Composition
{
    /// <summary>Structural view-after-core: ILateTickable after IFixedTickable sim.</summary>
    public sealed class ViewLateTickable : ILateTickable
    {
        public void LateTick() { }
    }
}
""",
    }.items():
        write(f"Packages/com.netninja.composition/Runtime/{name}", body)

    write("Packages/com.netninja.telemetry/Runtime/OtlpExporter.cs", """namespace NetNinja.Telemetry.Export
{
    /// <summary>Optional OTLP export for vantage-deck. Schema stays in Contracts.</summary>
    public sealed class OtlpExporter
    {
        public void Flush() { }
    }
}
""")

    # Editor — functional Config/Key window + stubs
    write("Packages/com.netninja.editor/Editor/Shell/WorkstationShell.cs", """using UnityEngine.UIElements;

namespace NetNinja.Editor.Shell
{
    public static class WorkstationShell
    {
        public static VisualElement CreateRoot(string title)
        {
            var root = new VisualElement();
            root.AddToClassList("nn-shell");
            var h = new Label(title);
            h.AddToClassList("nn-shell__title");
            root.Add(h);
            return root;
        }
    }
}
""")
    write("Packages/com.netninja.editor/Editor/Shell/WorkstationShell.uxml", """<?xml version="1.0" encoding="utf-8"?>
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement class="nn-shell">
    <ui:Label name="title" text="Net Ninja Workstation" class="nn-shell__title"/>
    <ui:VisualElement name="content" class="nn-shell__content"/>
  </ui:VisualElement>
</ui:UXML>
""")
    write("Packages/com.netninja.editor/Editor/Shell/WorkstationShell.uss", """
.nn-shell { padding: 8px; flex-grow: 1; }
.nn-shell__title { font-size: 14px; -unity-font-style: bold; margin-bottom: 8px; }
.nn-shell__content { flex-grow: 1; }
.nn-hash-badge { padding: 4px 8px; background-color: rgb(40,40,48); color: rgb(120,220,160); margin: 4px 0; }
.nn-key-row { flex-direction: row; margin: 2px 0; }
.nn-key-row > .nn-key { width: 280px; }
.nn-key-row > .nn-val { flex-grow: 1; }
""")
    write("Packages/com.netninja.editor/Editor/Controls/KeyRow.cs", """using UnityEngine.UIElements;

namespace NetNinja.Editor.Controls
{
    public sealed class KeyRow : VisualElement
    {
        public readonly TextField KeyField = new TextField();
        public readonly DoubleField ValueField = new DoubleField();
        public KeyRow()
        {
            AddToClassList("nn-key-row");
            KeyField.AddToClassList("nn-key");
            ValueField.AddToClassList("nn-val");
            Add(KeyField);
            Add(ValueField);
        }
    }
}
""")
    write("Packages/com.netninja.editor/Editor/Controls/HashBadge.cs", """using UnityEngine.UIElements;

namespace NetNinja.Editor.Controls
{
    public sealed class HashBadge : Label
    {
        public HashBadge()
        {
            AddToClassList("nn-hash-badge");
            text = "configHash: —";
        }
        public void SetHash(string hex) => text = "configHash: " + hex;
    }
}
""")
    write("Packages/com.netninja.editor/Editor/Controls/PersonaPicker.cs", """using UnityEngine.UIElements;

namespace NetNinja.Editor.Controls
{
    public sealed class PersonaPicker : PopupField<string>
    {
        public PersonaPicker() : base(new System.Collections.Generic.List<string> {
            "perfect", "average", "sloppy", "perfect-nocorr"
        }, 0) { }
    }
}
""")
    write("Packages/com.netninja.editor/Editor/Import/ConfigImporter.cs", """using System.IO;
using NetNinja.Config;
using NetNinja.Core;
using NetNinja.Core.State;
using UnityEditor;
using UnityEngine;

namespace NetNinja.Editor.Import
{
    /// <summary>JSON (config/default.json) → NetNinjaConfigSO. Parity keys imported, never re-typed.</summary>
    public static class ConfigImporter
    {
        public static NetNinjaConfigSO ImportFromJson(string jsonPath, string assetPath = "Assets/_NetNinja/Config/NetNinjaConfig.asset")
        {
            var text = File.ReadAllText(jsonPath);
            var map = ParseLooseJson(text);
            var so = ScriptableObject.CreateInstance<NetNinjaConfigSO>();
            so.keys.Clear();
            foreach (var kv in map)
                so.keys.Add(new KeyEntry { key = kv.Key, value = kv.Value });

            // configHash from CoreConfig flatten of imported doubles
            var cfg = CoreConfig.CreateDefault();
            foreach (var e in so.keys) cfg.Set(e.key, e.value);
            so.ConfigHashBadge = FnvStateHasher.HashConfig(cfg);

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");
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

        /// <summary>Minimal JSON object parser for flat string→number|bool|string maps (no deps).</summary>
        public static System.Collections.Generic.Dictionary<string, double> ParseLooseJson(string text)
        {
            // Prefer Unity's JsonUtility is too weak for dicts; use simple regex-ish split via MiniJSON-like.
            // For robustness in editor without Newtonsoft in Editor asm: use CoreConfig.DefaultMap when path missing.
            var result = new System.Collections.Generic.Dictionary<string, double>();
            // Fallback: load defaults then overlay numeric tokens — actual path uses SimpleJSON-ish:
            try
            {
                // Unity 6 has no built-in dict JSON; parse with a tiny state machine for "key": value
                int i = 0;
                while (i < text.Length)
                {
                    int q1 = text.IndexOf('"', i);
                    if (q1 < 0) break;
                    int q2 = text.IndexOf('"', q1 + 1);
                    if (q2 < 0) break;
                    string key = text.Substring(q1 + 1, q2 - q1 - 1);
                    int colon = text.IndexOf(':', q2);
                    if (colon < 0) break;
                    int j = colon + 1;
                    while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
                    if (j >= text.Length) break;
                    if (text[j] == '"')
                    {
                        // string value — skip for double map (policy strings handled via CoreConfig defaults)
                        int e = text.IndexOf('"', j + 1);
                        i = e < 0 ? text.Length : e + 1;
                        continue;
                    }
                    if (text.Length >= j + 4 && text.Substring(j, 4) == "true")
                    {
                        result[key] = 1;
                        i = j + 4;
                        continue;
                    }
                    if (text.Length >= j + 5 && text.Substring(j, 5) == "false")
                    {
                        result[key] = 0;
                        i = j + 5;
                        continue;
                    }
                    int k = j;
                    while (k < text.Length && (char.IsDigit(text[k]) || text[k] == '-' || text[k] == '+' || text[k] == '.' || text[k] == 'e' || text[k] == 'E'))
                        k++;
                    if (k > j && double.TryParse(text.Substring(j, k - j), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var num))
                        result[key] = num;
                    i = k;
                }
            }
            catch { /* fall through */ }
            return result;
        }
    }
}
""")

    write("Packages/com.netninja.editor/Editor/Windows/ConfigKeyEditorWindow.cs", """using System.IO;
using NetNinja.Config;
using NetNinja.Editor.Controls;
using NetNinja.Editor.Import;
using NetNinja.Editor.Shell;
using UnityEditor;
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
""")
    write("Packages/com.netninja.editor/Editor/Windows/ConfigKeyEditorWindow.uxml",
          """<?xml version="1.0" encoding="utf-8"?>\n<ui:UXML xmlns:ui="UnityEngine.UIElements"><ui:Label text="ConfigKeyEditor"/></ui:UXML>\n""")

    stubs = [
        "WaveSweepAuthoringWindow", "PersonaRunnerWindow", "TempoDdaTunerWindow",
        "FailureModeDiagnosticsWindow", "BatchMatrixRunnerWindow", "TelemetryInspectorWindow",
        "GoldenVectorParityWindow",
    ]
    menus = [
        "Wave / Sweep", "Persona Runner", "Tempo / DDA", "Failure Modes",
        "Batch Matrix", "Telemetry", "Golden Vectors",
    ]
    for win, menu in zip(stubs, menus):
        write(f"Packages/com.netninja.editor/Editor/Windows/{win}.cs", f"""using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{{
    public sealed class {win} : EditorWindow
    {{
        [MenuItem("Net Ninja/Workstation/{menu}")]
        public static void Open()
        {{
            var w = GetWindow<{win}>();
            w.titleContent = new GUIContent("{menu}");
        }}

        void CreateGUI()
        {{
            rootVisualElement.Add(WorkstationShell.CreateRoot("{menu} (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }}
    }}
}}
""")
        write(f"Packages/com.netninja.editor/Editor/Windows/{win}.uxml",
              f"""<?xml version="1.0" encoding="utf-8"?>\n<ui:UXML xmlns:ui="UnityEngine.UIElements"><ui:Label text="{win}"/></ui:UXML>\n""")

    # Fix ConfigKeyEditorWindow - ObjectField needs UnityEditor.UIElements
    # Already using ObjectField - need using UnityEditor.UIElements

    # Tests stubs
    write("Assets/Tests/Conformance/NetNinja.Conformance.Tests.asmdef", json.dumps({
        "name": "NetNinja.Conformance.Tests",
        "references": ["NetNinja.Contracts", "NetNinja.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner", "Unity.Newtonsoft.Json"],
        "optionalUnityReferences": ["TestAssemblies"],
        "defineConstraints": ["UNITY_INCLUDE_TESTS"],
        "includePlatforms": [],
    }, indent=2))
    write("Assets/Tests/Conformance/GoldenVectorTests.cs", """using NUnit.Framework;
using NetNinja.Core.State;
using NetNinja.Core;

namespace NetNinja.Conformance.Tests
{
    public class GoldenVectorTests
    {
        [Test]
        public void ConfigHash_MatchesOracle()
        {
            Assert.AreEqual("6c3a8288f02919a3", FnvStateHasher.HashConfig(CoreConfig.CreateDefault()));
        }
    }
}
""")
    write("Assets/Tests/Conformance/SelfDeterminismTests.cs", "using NUnit.Framework;\nnamespace NetNinja.Conformance.Tests { public class SelfDeterminismTests { [Test] public void Placeholder() => Assert.Pass(); } }\n")
    write("Assets/Tests/Conformance/HasherMicroVectorTests.cs", "using NUnit.Framework;\nnamespace NetNinja.Conformance.Tests { public class HasherMicroVectorTests { [Test] public void Placeholder() => Assert.Pass(); } }\n")
    write("Assets/Tests/Conformance/ConfigHashGuardTests.cs", "using NUnit.Framework;\nnamespace NetNinja.Conformance.Tests { public class ConfigHashGuardTests { [Test] public void Placeholder() => Assert.Pass(); } }\n")
    write("Assets/Tests/Conformance/OnTargetParityPlayModeTests.cs", "using NUnit.Framework;\nnamespace NetNinja.Conformance.Tests { public class OnTargetParityPlayModeTests { [Test] public void Placeholder() => Assert.Pass(); } }\n")

    for suite, asm, refs in [
        ("Adapters", "NetNinja.Adapters.Tests", ["NetNinja.Contracts", "NetNinja.Core", "NetNinja.Adapters"]),
        ("View", "NetNinja.View.Tests", ["NetNinja.Contracts", "NetNinja.View"]),
        ("Composition", "NetNinja.Composition.Tests", ["NetNinja.Contracts", "NetNinja.Core", "NetNinja.Config", "NetNinja.Adapters", "NetNinja.View", "NetNinja.Composition", "VContainer"]),
    ]:
        write(f"Assets/Tests/{suite}/{asm}.asmdef", json.dumps({
            "name": asm,
            "references": refs + ["UnityEngine.TestRunner"],
            "optionalUnityReferences": ["TestAssemblies"],
            "defineConstraints": ["UNITY_INCLUDE_TESTS"],
        }, indent=2))
        write(f"Assets/Tests/{suite}/SmokeTests.cs", f"using NUnit.Framework;\nnamespace {asm} {{ public class SmokeTests {{ [Test] public void Placeholder() => Assert.Pass(); }} }}\n")

    write("Assets/Tests/Editor/NetNinja.Editor.Tests.asmdef", json.dumps({
        "name": "NetNinja.Editor.Tests",
        "references": ["NetNinja.Contracts", "NetNinja.Core", "NetNinja.Config", "NetNinja.Editor"],
        "includePlatforms": ["Editor"],
        "optionalUnityReferences": ["TestAssemblies"],
        "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    }, indent=2))
    for t in ["LawGuardTests", "AnalyzerTripTests", "ConfigRoundTripTests", "ConfigImportHashTests", "WindowCompileTests"]:
        write(f"Assets/Tests/Editor/{t}.cs", f"using NUnit.Framework;\nnamespace NetNinja.Editor.Tests {{ public class {t} {{ [Test] public void Placeholder() => Assert.Pass(); }} }}\n")

    # Project settings minimal
    write("ProjectSettings/EditorSettings.asset", """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!159 &1
EditorSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_SerializationMode: 2
  m_LineEndingsForNewScripts: 2
  m_DefaultBehaviorMode: 0
  m_PrefabRegularEnvironment: {fileID: 0}
  m_PrefabUIEnvironment: {fileID: 0}
  m_SpritePackerMode: 0
  m_SpritePackerCacheSize: 10
  m_SpritePackerPaddingPower: 1
  m_Bc7TextureCompressor: 0
  m_EtcTextureCompressorBehavior: 1
  m_EtcTextureFastCompressor: 1
  m_EtcTextureNormalCompressor: 2
  m_EtcTextureBestCompressor: 4
  m_ProjectGenerationIncludedExtensions: txt;xml;fnt;cd;asmdef;asmref;rsp;java;cpp;c;mm;m;h;shader;hlsl;json;md
  m_ProjectGenerationRootNamespace: 
  m_EnableTextureStreamingInEditMode: 1
  m_EnableTextureStreamingInPlayMode: 1
  m_EnableEditorAsyncCPUTextureLoading: 0
  m_AsyncShaderCompilation: 1
  m_PrefabModeAllowAutoSave: 1
  m_EnterPlayModeOptionsEnabled: 0
  m_EnterPlayModeOptions: 3
  m_GameObjectNamingDigits: 1
  m_GameObjectNamingScheme: 0
  m_AssetNamingUsesSpace: 1
  m_InspectorUseIMGUIDefaultInspector: 0
  m_UseLegacyProbeSampleCount: 0
  m_SerializeInlineMappingsOnOneLine: 1
  m_DisableCookiesInLightmapper: 1
  m_AssetPipelineMode: 1
  m_RefreshImportMode: 0
  m_CacheServerMode: 0
  m_CacheServerEndpoint: 
  m_CacheServerNamespacePrefix: default
  m_CacheServerEnableDownload: 1
  m_CacheServerEnableUpload: 1
  m_CacheServerEnableAuth: 0
  m_CacheServerEnableTls: 0
  m_CacheServerValidationMode: 2
  m_CacheServerDownloadBatchSize: 128
  m_EnableEnlightenBakedGI: 0
""")
    write("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.4.3f1\nm_EditorVersionWithRevision: 6000.4.3f1\n")
    write("ProjectSettings/IL2CPP-NOTES.md", """# IL2CPP floating-point contract

Pin **fp-contract=off** on all IL2CPP player builds (Android arm64, WebGL, iOS) so the C++ backend
does not rewrite `a*b+c` into FMA. Without this, ARM can diverge from golden doubles.

GameCI / Player build scripts must inject the equivalent of:
`-ffp-contract=off` (clang) / `/fp:precise` style contract disable for the IL2CPP toolchain.

Do **not** enable Burst fastmath on Core (Core is managed, no Burst).
""")

    # manifest — versions likely for Unity 6
    manifest = {
        "dependencies": {
            "com.netninja.contracts": "file:com.netninja.contracts",
            "com.netninja.core": "file:com.netninja.core",
            "com.netninja.config": "file:com.netninja.config",
            "com.netninja.adapters": "file:com.netninja.adapters",
            "com.netninja.view": "file:com.netninja.view",
            "com.netninja.composition": "file:com.netninja.composition",
            "com.netninja.telemetry": "file:com.netninja.telemetry",
            "com.netninja.editor": "file:com.netninja.editor",
            "com.netninja.determinism-analyzer": "file:com.netninja.determinism-analyzer",
            "com.unity.inputsystem": "1.14.0",
            "com.unity.addressables": "2.4.3",
            "com.unity.render-pipelines.universal": "17.1.0",
            "com.unity.textmeshpro": "3.2.0-pre.12",
            "com.unity.test-framework": "1.5.1",
            "com.unity.nuget.newtonsoft-json": "3.2.1",
            "com.unity.modules.ui": "1.0.0",
            "com.unity.modules.uielements": "1.0.0",
            "jp.hadashikick.vcontainer": "1.16.8",
            "com.cysharp.messagepipe": "1.8.1",
            "com.cysharp.messagepipe.vcontainer": "1.8.1",
            "com.cysharp.unitask": "2.5.10",
            "com.cysharp.r3": "1.3.0",
            "com.annulusgames.litmotion": "2.0.2",
            "com.code-philosophy.hybridclr": "file:../LocalPackages/empty-optional",
        },
        "scopedRegistries": [
            {
                "name": "package.openupm.com",
                "url": "https://package.openupm.com",
                "scopes": [
                    "jp.hadashikick.vcontainer",
                    "com.cysharp",
                    "com.annulusgames",
                    "com.madsbangstrup.openupm",
                ],
            }
        ],
    }
    # Cleaner manifest without hybridclr
    del manifest["dependencies"]["com.code-philosophy.hybridclr"]
    # Add mob-sakai via git if known; use openupm placeholders for tri-inspector
    manifest["dependencies"]["com.annulusgames.unity-toolbar-extender"] = "1.0.0"
    # tri-inspector openupm id
    manifest["dependencies"]["com.nate.triinspector"] = "1.14.0"
    # simplify - use well-known openupm package ids
    manifest = {
        "dependencies": {
            "com.netninja.contracts": "file:com.netninja.contracts",
            "com.netninja.core": "file:com.netninja.core",
            "com.netninja.config": "file:com.netninja.config",
            "com.netninja.adapters": "file:com.netninja.adapters",
            "com.netninja.view": "file:com.netninja.view",
            "com.netninja.composition": "file:com.netninja.composition",
            "com.netninja.telemetry": "file:com.netninja.telemetry",
            "com.netninja.editor": "file:com.netninja.editor",
            "com.netninja.determinism-analyzer": "file:com.netninja.determinism-analyzer",
            "com.unity.inputsystem": "1.14.0",
            "com.unity.addressables": "2.4.3",
            "com.unity.render-pipelines.universal": "17.1.0",
            "com.unity.ugui": "2.0.0",
            "com.unity.test-framework": "1.5.1",
            "com.unity.nuget.newtonsoft-json": "3.2.1",
            "com.unity.modules.imgui": "1.0.0",
            "com.unity.modules.ui": "1.0.0",
            "com.unity.modules.uielements": "1.0.0",
            "jp.hadashikick.vcontainer": "1.16.8",
            "com.cysharp.messagepipe": "1.8.1",
            "com.cysharp.messagepipe.vcontainer": "1.8.1",
            "com.cysharp.unitask": "2.5.10",
            "com.cysharp.r3": "1.3.0",
        },
        "scopedRegistries": [
            {
                "name": "package.openupm.com",
                "url": "https://package.openupm.com",
                "scopes": ["jp.hadashikick", "com.cysharp", "com.annulusgames", "com.coffee"],
            }
        ],
    }
    write("Packages/manifest.json", json.dumps(manifest, indent=2))
    write("Packages/packages-lock.json", json.dumps({
        "dependencies": {k: {"version": v, "depth": 0, "source": "embedded" if str(v).startswith("file:") else "registry",
                             "dependencies": {}} for k, v in manifest["dependencies"].items()},
        "_note": "Skeleton lock — Unity will rewrite on first resolve. Committed so worktrees share intent."
    }, indent=2))

    # Root docs / agent files
    write("AGENTS.md", """# Net Ninja — agent brief

- **Repo:** standalone Unity 6000.4.3f1 / URP. NOT gamespree.
- **Seams:** Contracts (engine-free) ← Core (engine-free, Core→Contracts only) ← Config/Adapters/View/Composition/Editor.
- **Parity:** `scripts/check.ps1` = tier-1 pure .NET. No Unity license. Golden via `golden/traces` + `Sim`.
- **Determinism:** double only in Contracts/Core; allowlist analyzer; DT=1/60; FNV hash per `docs/hashing-spec.md`.
- **Never** push/merge from agent; never touch net-lab or gamespree.
- **Config:** import `config/default.json` via Config/Key Editor — do not re-type parity literals.
""")
    write("CLAUDE.md", "@AGENTS.md\n")
    write("README.md", """# Net Ninja (Unity)

Editor-first Unity 6000.4.3f1 / URP edition of Pawfall's net-catch mechanic.

## Tier-1 check (no Unity license)

```powershell
./scripts/check.ps1
```

## Structure

Embedded UPM packages under `Packages/com.netninja.*`. See `AGENTS.md` and `docs/`.
""")
    write(".gitattributes", """* text=auto
*.cs text diff=csharp
*.asmdef text
*.json text
*.md text
*.uxml text
*.uss text
*.shader text
*.hlsl text
*.yaml text
*.yml text

# Unity YAML + SmartMerge
*.unity merge=unityyamlmerge eol=lf
*.prefab merge=unityyamlmerge eol=lf
*.asset merge=unityyamlmerge eol=lf
*.meta merge=unityyamlmerge eol=lf
*.mat merge=unityyamlmerge eol=lf
*.anim merge=unityyamlmerge eol=lf
*.controller merge=unityyamlmerge eol=lf

# golden/ and config/ stay plain text (non-LFS)
golden/** text
config/** text

# LFS binaries
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.jpeg filter=lfs diff=lfs merge=lfs -text
*.tga filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.ttf filter=lfs diff=lfs merge=lfs -text
*.otf filter=lfs diff=lfs merge=lfs -text
""")
    write(".editorconfig", """root = true
[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
[*.{json,yml,yaml,md}]
indent_size = 2
""")

    # Copy ADR pack
    adrs_src = Path(r"C:/GameDev/.vantage/grok-logs/netninja-skeleton/spec/ADRS.md")
    if adrs_src.exists():
        write("docs/adr/ADRS.md", adrs_src.read_text(encoding="utf-8"))
    write("docs/adr/template.md", """# ADR-XXXX: Title\n\n- Status: proposed\n- Date: YYYY-MM-DD\n\n## Context\n\n## Decision\n\n## Consequences\n""")
    write("docs/CONTEXT.md", """# Domain language\n\nSee net-lab CONTEXT: Intent grammar (Catch, Wave-ride, Pour, Hold, Reposition, Juggle, Abandon-sweep),\nPersonas (Perfect/Average/Sloppy), Sweep Policy, Wave Catch, Empty Loop / Catch-Skill Wall.\n""")
    write("docs/seams.md", """# Seams\n\nSee ARCHITECTURE-FINAL.json seamMap. Contracts = boundary types; Core = sim+personas; Adapters = engine glue; View = read-only; Composition = VContainer; Editor = workstation.\n""")

    # Tools
    write("Tools/gen-bootbrief/gen.py", """#!/usr/bin/env python3
from pathlib import Path
root = Path(__file__).resolve().parents[2]
asm = list((root / "Packages").rglob("*.asmdef"))
out = root / "BOOT-BRIEF.generated.md"
lines = ["# BOOT-BRIEF (generated)", "", f"asmdefs: {len(asm)}", ""]
for a in sorted(asm):
    lines.append(f"- `{a.relative_to(root)}`")
out.write_text("\\n".join(lines) + "\\n", encoding="utf-8")
print("wrote", out)
""")
    write("Tools/registration-gen/README.md", "Emits MessagePipe per-type registration from event structs. Skeleton stub.\n")
    write("Tools/config-import/README.md", "CLI counterpart of Editor ConfigImporter. Prefer Editor window for SO write.\n")

    # scripts
    write("scripts/check-full.ps1", """# Tier-2: Unity batchmode (requires UNITY_LICENSE). Deferred until license available.
Write-Host "tier-2 Unity batchmode not run in this environment (no UNITY_LICENSE)." -ForegroundColor Yellow
exit 0
""")
    write("scripts/check-full.sh", """#!/usr/bin/env bash
echo "tier-2 Unity batchmode not run (no UNITY_LICENSE)."
exit 0
""")
    write("scripts/run-webgl-parity.sh", "#!/usr/bin/env bash\necho 'WebGL parity: requires Unity license + build'\nexit 0\n")
    write("scripts/run-arm64-parity.sh", "#!/usr/bin/env bash\necho 'arm64 parity: requires Unity license + build'\nexit 0\n")
    write("scripts/new-worktree.ps1", """param([Parameter(Mandatory=$true)][string]$Branch, [string]$Path)
if (-not $Path) { $Path = Join-Path (Split-Path (Get-Location)) $Branch.Replace('/','-') }
git worktree add -b $Branch $Path
Write-Host "Worktree $Path — open Unity there so Library is per-worktree."
""")
    write("scripts/new-worktree.sh", """#!/usr/bin/env bash
BRANCH=$1; PATH_WT=${2:-../$1}
git worktree add -b "$BRANCH" "$PATH_WT"
echo "Worktree $PATH_WT — open Unity there so Library is per-worktree."
""")
    write("scripts/setup-smartmerge.ps1", """# Per-machine UnityYAMLMerge configuration
$unity = "C:/Program Files/Unity/Hub/Editor/6000.4.3f1/Editor/Data/Tools/UnityYAMLMerge.exe"
if (-not (Test-Path $unity)) { Write-Host "Adjust Unity path in this script."; exit 1 }
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "`"$unity`" merge -p %O %B %A %A"
Write-Host "SmartMerge configured."
""")
    write("scripts/setup-smartmerge.sh", """#!/usr/bin/env bash
echo "Configure merge.unityyamlmerge.driver to your UnityYAMLMerge path (see setup-smartmerge.ps1)."
""")

    # CI
    write(".github/workflows/check.yml", """name: tier-1 check
on:
  pull_request:
  push:
    branches: [main, feat/**]
jobs:
  parity:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - name: Tier-1 pure .NET
        run: |
          dotnet build Tools/determinism-analyzer/NetNinja.Determinism.Analyzer.csproj -c Release
          dotnet test Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj -c Release --nologo
""")
    write(".github/workflows/unity-tests.yml", """name: unity-tests (tier-2)
on:
  workflow_dispatch:
  # pull_request: enable when UNITY_LICENSE secret is set
jobs:
  editmode:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Placeholder
        run: |
          echo "Requires secrets.UNITY_LICENSE (Unity grant). Yellow until added."
          echo "Then: game-ci/unity-test-runner with unityVersion 6000.4.3f1"
          exit 0
""")
    write(".github/workflows/arm64-parity.yml", """name: arm64-parity
on:
  workflow_dispatch:
jobs:
  arm64:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Placeholder
        run: |
          echo "Requires secrets.UNITY_LICENSE + Android IL2CPP build with fp-contract=off."
          echo "See docs/parity-rings.md"
          exit 0
""")
    write(".github/workflows/webgl-parity.yml", """name: webgl-parity
on:
  workflow_dispatch:
jobs:
  webgl:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Placeholder
        run: |
          echo "Requires secrets.UNITY_LICENSE + WebGL player + headless hash capture."
          exit 0
""")
    write(".github/workflows/build-matrix.yml", """name: build-matrix
on:
  workflow_dispatch:
jobs:
  matrix:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        target: [Android, WebGL]
    steps:
      - uses: actions/checkout@v4
      - name: Placeholder
        run: |
          echo "Build ${{ matrix.target }} via game-ci/unity-builder when UNITY_LICENSE is present."
          echo "iOS: Linux exports Xcode project only; device build on macOS runner."
          exit 0
""")

    # Assets placeholders
    write("Assets/_NetNinja/Config/.gitkeep", "")
    write("Assets/_NetNinja/Scenes/.gitkeep", "")
    write("Assets/_NetNinja/Rendering/.gitkeep", "")
    write("Assets/_NetNinja/Input/NetNinjaControls.inputactions", """{"name":"NetNinjaControls","maps":[{"name":"Player","id":"a0","actions":[{"name":"Point","type":"Value","id":"a1","expectedControlType":"Vector2"}],"bindings":[]}],"controlSchemes":[]}\n""")
    write("Assets/_NetNinja/Prefabs/.gitkeep", "")
    write("Assets/_NetNinja/Art/.gitkeep", "")
    write("Assets/_NetNinja/Audio/.gitkeep", "")
    write("Assets/_NetNinja/UI/.gitkeep", "")

    # Fix ObjectField using
    p = ROOT / "Packages/com.netninja.editor/Editor/Windows/ConfigKeyEditorWindow.cs"
    t = p.read_text(encoding="utf-8")
    if "UnityEditor.UIElements" not in t:
        t = t.replace("using UnityEditor;\n", "using UnityEditor;\nusing UnityEditor.UIElements;\n")
        p.write_text(t, encoding="utf-8")

    # gen boot brief
    import subprocess
    subprocess.run(["python", str(ROOT / "Tools/gen-bootbrief/gen.py")], check=False)

    # metas for new files
    for dirpath, _, filenames in os_walk_safe(ROOT):
        rel_dir = str(Path(dirpath).relative_to(ROOT)).replace("\\", "/")
        if any(x in rel_dir for x in (".git", "bin", "obj", "Library", "node_modules")):
            continue
        ensure_meta(rel_dir if rel_dir != "." else "", folder=True)
        for f in filenames:
            if f.endswith(".meta"):
                continue
            ensure_meta(f"{rel_dir}/{f}" if rel_dir != "." else f)

    print("Slice 2 scaffold complete")


def os_walk_safe(root: Path):
    import os
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in (".git", "bin", "obj", "Library", "node_modules")]
        yield dirpath, dirnames, filenames


if __name__ == "__main__":
    main()
