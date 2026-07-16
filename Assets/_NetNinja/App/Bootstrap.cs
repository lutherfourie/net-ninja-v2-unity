using UnityEngine;
using NetNinja.Adapters;
using NetNinja.Contracts;
using NetNinja.Core;

namespace NetNinja.App
{
    /// <summary>
    /// Plain-MonoBehaviour composition root — replaces the VContainer Root/Game/EditorTool
    /// LifetimeScope stack (ADR-0006, superseded by ADR-0019). Owns the deterministic graph and
    /// drives the two seams by hand, exactly where the DI entry points used to sit:
    ///   FixedUpdate → SimPump.Step()  (was SimPump : IFixedTickable, the DT=1/60 owner)
    ///   LateUpdate  → view-apply/projection (was ViewLateTickable : ILateTickable, strictly after the sim)
    /// No DI / Rx / message bus: plain C# construction plus the typed StateProjector seam.
    /// UniTask is re-addable in ONE Packages/manifest.json line the moment an async boot
    /// (e.g. an Addressables load) needs it; nothing here requires it today.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] int _seed = 0;

        Sim _sim;
        IInputReader _input;
        SimPump _pump;
        StateProjector _projector;
        int _tick;

        void Awake()
        {
            // new the graph (was RootLifetimeScope + GameLifetimeScope.Configure)
            var cfg = CoreConfig.CreateDefault();
            _sim = new Sim(cfg, _seed);
            _input = new DeviceInputSource();
            _pump = new SimPump(_sim);
            _projector = new StateProjector();
        }

        // Sim step seam: one deterministic DT=1/60 tick, then publish the snapshot.
        void FixedUpdate()
        {
            var frame = _input.Read(_tick, _sim.Time);
            _projector.Project(_pump.Step(frame));
            _tick++;
        }

        // View-apply seam: runs strictly after the fixed sim step; View reads _projector.Last.
        void LateUpdate()
        {
            _ = _projector.Last;
        }
    }
}
