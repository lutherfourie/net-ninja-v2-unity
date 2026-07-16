using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    public sealed class TempoDdaTunerWindow : EditorWindow
    {
        [MenuItem("Net Ninja/Workstation/Tempo / DDA")]
        public static void Open()
        {
            var w = GetWindow<TempoDdaTunerWindow>();
            w.titleContent = new GUIContent("Tempo / DDA");
        }

        void CreateGUI()
        {
            rootVisualElement.Add(WorkstationShell.CreateRoot("Tempo / DDA (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }
    }
}
