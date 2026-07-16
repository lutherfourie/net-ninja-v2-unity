using UnityEngine.UIElements;

namespace NetNinja.Editor.Shell
{
    public static class WorkstationShell
    {
        public static VisualElement CreateRoot(string title)
        {
            var root = new VisualElement();
            root.AddToClassList("nn-shell");
            var h = new Label(title);
            h.AddToClassList("nn-shell__title");
            root.Add(h);
            return root;
        }
    }
}
