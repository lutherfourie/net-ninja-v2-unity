using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    /// <summary>
    /// INSPECTOR (ADR-0001/0019): read-only view of the deterministic telemetry journal. Consume/verify
    /// surface, never rule-authoring. Stub this pass.
    /// </summary>
    public sealed class TelemetryInspectorWindow : EditorWindow
    {
        [MenuItem("Net Ninja/Workstation/Telemetry")]
        public static void Open()
        {
            var w = GetWindow<TelemetryInspectorWindow>();
            w.titleContent = new GUIContent("Telemetry");
        }

        void CreateGUI()
        {
            rootVisualElement.Add(WorkstationShell.CreateRoot("Telemetry (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }
    }
}
