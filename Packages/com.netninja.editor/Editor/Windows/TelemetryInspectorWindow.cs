using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
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
