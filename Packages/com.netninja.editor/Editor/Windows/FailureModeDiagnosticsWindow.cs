using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    public sealed class FailureModeDiagnosticsWindow : EditorWindow
    {
        [MenuItem("Net Ninja/Workstation/Failure Modes")]
        public static void Open()
        {
            var w = GetWindow<FailureModeDiagnosticsWindow>();
            w.titleContent = new GUIContent("Failure Modes");
        }

        void CreateGUI()
        {
            rootVisualElement.Add(WorkstationShell.CreateRoot("Failure Modes (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }
    }
}
