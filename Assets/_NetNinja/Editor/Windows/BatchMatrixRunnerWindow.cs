using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    /// <summary>
    /// RUNNER (ADR-0001/0019): runs the batch-matrix (whose definitions are AUTHORED in the web
    /// workstation and imported) to generate traces and verify the port. Consume/run, never rule-authoring. Stub this pass.
    /// </summary>
    public sealed class BatchMatrixRunnerWindow : EditorWindow
    {
        [MenuItem("Net Ninja/Workstation/Batch Matrix")]
        public static void Open()
        {
            var w = GetWindow<BatchMatrixRunnerWindow>();
            w.titleContent = new GUIContent("Batch Matrix");
        }

        void CreateGUI()
        {
            rootVisualElement.Add(WorkstationShell.CreateRoot("Batch Matrix (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }
    }
}
