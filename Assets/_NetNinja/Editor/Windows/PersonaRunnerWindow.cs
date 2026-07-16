using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    /// <summary>
    /// RUNNER (ADR-0001/0019): drives the engine-free personas headless in-editor to generate traces
    /// that CHECK the port against the oracle. Consume/run surface, never rule-authoring. Stub this pass.
    /// </summary>
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
