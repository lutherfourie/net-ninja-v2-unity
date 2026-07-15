using VContainer;
using VContainer.Unity;

namespace NetNinja.Composition
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Sim, ISweepPolicy, adapters, View as ILateTickable
        }
    }
}
