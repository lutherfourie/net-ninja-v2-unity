using UnityEngine.UIElements;

namespace NetNinja.Editor.Controls
{
    public sealed class HashBadge : Label
    {
        public HashBadge()
        {
            AddToClassList("nn-hash-badge");
            text = "configHash: —";
        }
        public void SetHash(string hex) => text = "configHash: " + hex;
    }
}
