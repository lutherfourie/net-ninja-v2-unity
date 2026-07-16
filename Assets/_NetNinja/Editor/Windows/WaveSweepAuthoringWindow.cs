using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NetNinja.Editor.Shell;

namespace NetNinja.Editor.Windows
{
    /// <summary>
    /// INSPECTOR/IMPORTER (ADR-0001/0019): waves + sweep-policy params are rule-bearing, so they are
    /// AUTHORED in the web workstation and exported as truth (ADR-0011). Despite the legacy "Authoring"
    /// name this window only imports/inspects/previews those specs — hand-authoring rule data here is a
    /// ledgered "Untwinned Port". Stub this pass; rename deferred (no functional work).
    /// </summary>
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
