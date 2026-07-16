using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    /// <summary>
    /// INSPECTOR/IMPORTER (ADR-0001/0019): tempo + DDA tunables are rule-bearing, AUTHORED in the web
    /// workstation and imported as truth (ADR-0007/0011). Despite the legacy "Tuner" name this window
    /// only inspects/previews the imported curves — it does not author rule data. Stub this pass.
    /// </summary>
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
