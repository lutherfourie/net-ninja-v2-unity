using UnityEngine;
using NetNinja.Contracts;
using NetNinja.Core.SweepPolicy;

namespace NetNinja.Adapters
{
    public abstract class SweepPolicySO : ScriptableObject
    {
        public abstract ISweepPolicy Create();
    }

    [CreateAssetMenu(menuName = "Net Ninja/Sweep Policy/Fair")]
    public class FairSweepPolicySO : SweepPolicySO
    {
        public override ISweepPolicy Create() => new FairSweepPolicy();
    }
}
