using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    /// <summary>
    /// INSPECTOR (ADR-0001/0019): read-only golden-vector / parity view against the exported oracle
    /// (ADR-0011). Consume/verify surface, never rule-authoring. Stub this pass.
    /// </summary>
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
