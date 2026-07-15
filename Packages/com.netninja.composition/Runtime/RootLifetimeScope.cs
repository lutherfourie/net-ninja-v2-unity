using VContainer;
using VContainer.Unity;

namespace NetNinja.Composition
{
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // ConfigService, telemetry bus, input backend
        }
    }
}
