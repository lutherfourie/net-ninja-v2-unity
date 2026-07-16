using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    public sealed class GoldenVectorParityWindow : EditorWindow
    {
        [MenuItem("Net Ninja/Workstation/Golden Vectors")]
        public static void Open()
        {
            var w = GetWindow<GoldenVectorParityWindow>();
            w.titleContent = new GUIContent("Golden Vectors");
        }

        void CreateGUI()
        {
            rootVisualElement.Add(WorkstationShell.CreateRoot("Golden Vectors (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }
    }
}
