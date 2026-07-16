using VContainer.Unity;

namespace NetNinja.Composition
{
    /// <summary>Structural view-after-core: ILateTickable after IFixedTickable sim.</summary>
    public sealed class ViewLateTickable : ILateTickable
    {
        public void LateTick() { }
    }
}
