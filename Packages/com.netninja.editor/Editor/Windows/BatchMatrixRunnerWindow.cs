using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
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
