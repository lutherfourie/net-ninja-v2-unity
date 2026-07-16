using UnityEngine.UIElements;

namespace NetNinja.Editor.Controls
{
    public sealed class KeyRow : VisualElement
    {
        public readonly TextField KeyField = new TextField();
        public readonly DoubleField ValueField = new DoubleField();
        public KeyRow()
        {
            AddToClassList("nn-key-row");
            KeyField.AddToClassList("nn-key");
            ValueField.AddToClassList("nn-val");
            Add(KeyField);
            Add(ValueField);
        }
    }
}
