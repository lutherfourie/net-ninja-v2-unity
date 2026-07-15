using UnityEngine.UIElements;

namespace NetNinja.Editor.Controls
{
    public sealed class PersonaPicker : PopupField<string>
    {
        public PersonaPicker() : base(new System.Collections.Generic.List<string> {
            "perfect", "average", "sloppy", "perfect-nocorr"
        }, 0) { }
    }
}
