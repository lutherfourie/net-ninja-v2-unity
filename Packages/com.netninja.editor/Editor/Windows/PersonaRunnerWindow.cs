using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    public sealed class PersonaRunnerWindow : EditorWindow
    {
        [MenuItem("Net Ninja/Workstation/Persona Runner")]
        public static void Open()
        {
            var w = GetWindow<PersonaRunnerWindow>();
            w.titleContent = new GUIContent("Persona Runner");
        }

        void CreateGUI()
        {
            rootVisualElement.Add(WorkstationShell.CreateRoot("Persona Runner (stub)"));
            rootVisualElement.Add(new Label("Compiling stub — functional in a later pass."));
        }
    }
}
