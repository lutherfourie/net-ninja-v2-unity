using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    public sealed class WaveSweepAuthoringWindow : EditorWindow
    {
        [MenuItem("Net Ninja/Workstation/Wave / Sweep")]
        public static void Open()
        {
            var w = GetWindow<WaveSweepAuthoringWindow>();
            w.titleContent = new GUIContent("Wave / Sweep");
        }

        void CreateGUI()
        {
            rootVisualElement.Add(WorkstationShell.CreateRoot("Wave / Sweep (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }
    }
}
