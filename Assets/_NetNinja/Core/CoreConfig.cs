// Auto-derived from net-lab packages/core/config.ts defaults. Do not hand-edit parity literals.
// Source: config/default.json. configHash target: 6c3a8288f02919a3
using System;
using System.Collections.Generic;
using NetNinja.Contracts;
namespace NetNinja.Core
{
    public sealed class CoreConfig : CoreConfigView
    {
        readonly Dictionary<string, object> _map = new Dictionary<string, object>();

        public CoreConfig() { }

        public static CoreConfig CreateDefault() => FromMap(DefaultMap());

        public CoreConfig Clone() => FromMap(new Dictionary<string, object>(_map));

        public static CoreConfig FromMap(Dictionary<string, object> map)
        {
            var c = new CoreConfig();
            foreach (var kv in map) c._map[kv.Key] = kv.Value;
            return c;
        }

        public IEnumerable<string> SortedKeys()
        {
            var keys = new List<string>(_map.Keys);
            keys.Sort(StringComparer.Ordinal);
            return keys;
        }

        public object Raw(string key) => _map[key];
        public void Set(string key, object value) => _map[key] = value;

        public double GetDouble(string key)
        {
            var v = _map[key];
            if (v is double d) return d;
            if (v is int i) return i;
            if (v is long l) return l;
            return Convert.ToDouble(v);
        }
        public bool GetBool(string key) => (bool)_map[key];
        public string GetString(string key) => Convert.ToString(_map[key]);
        public double D(string key) => GetDouble(key);
        public bool B(string key) => GetBool(key);
        public string S(string key) => GetString(key);
        public int I(string key) => (int)GetDouble(key);

        public static Dictionary<string, object> DefaultMap()
        {
            return new Dictionary<string, object>
            {
                ["net.capacity"] = 6.0,
                ["net.followLerpEmpty"] = 18.0,
                ["net.followLerpFull"] = 6.0,
                ["net.weightCurvePow"] = 1.5,
                ["net.followAlphaEmpty"] = 0.2591817793182821,
                ["net.followAlphaFull"] = 0.09516258196404048,
                ["net.followAlphaEmptyPointer"] = 0.4511883639059735,
                ["net.followAlphaFullPointer"] = 0.18126924692201818,
                ["net.facingEaseAlpha"] = 0.09516258196404048,
                ["net.facing.weightedSelfRight"] = true,
                ["net.facing.selfRightRate"] = 2.2,
                ["net.facing.clampUp"] = true,
                ["net.restPose.mode"] = "hold",
                ["net.restPose.rate"] = 2.2,
                ["net.restPose.homeX"] = 0.0,
                ["net.flick.cosHalfAngle"] = 0.8870108331782217,
                ["net.pullRadius"] = 4.0,
                ["net.magnet.enabled"] = false,
                ["net.magnet.radius"] = 0.9,
                ["net.magnet.strength"] = 7.0,
                ["net.catchRadius"] = 0.55,
                ["net.catch.directional"] = true,
                ["net.catch.minSpeed"] = 0.0,
                ["net.catch.speedCarryMs"] = 250.0,
                ["net.catch.coneDeg"] = 120.0,
                ["net.catch.mouthRadius"] = 0.68,
                ["net.rim.enabled"] = true,
                ["net.rim.tube"] = 0.05,
                ["net.rim.bounceSpeed"] = 3.2,
                ["net.rim.velTransfer"] = 0.45,
                ["net.fullBounce.enabled"] = true,
                ["juggle.ladder.enabled"] = true,
                ["juggle.rungCap"] = 3.0,
                ["juggle.rungDebounceSec"] = 0.25,
                ["juggle.mult1"] = 1.5,
                ["juggle.mult2"] = 2.0,
                ["juggle.mult3"] = 3.0,
                ["items.taxonomy.enabled"] = true,
                ["items.bouncyPushChance"] = 0.2,
                ["items.goldenPushOdds"] = 0.07,
                ["items.goldenValue"] = 100.0,
                ["items.webBounceSpeed"] = 3.6,
                ["items.webBounceMinLift"] = 2.6,
                ["items.webBounceReadBand"] = 0.25,
                ["items.knifePushChance"] = 0.1,
                ["items.charms.enabled"] = true,
                ["items.charmPushChance"] = 0.04,
                ["charm.payout.lifeOdds"] = 0.1,
                ["charm.barrier.saves"] = 3.0,
                ["charm.barrier.bounceVel"] = 6.0,
                ["charm.slow.factor"] = 0.6,
                ["charm.slow.radius"] = 1.6,
                ["charm.slow.durationSec"] = 6.0,
                ["net.break.repairSeconds"] = 0.6,
                ["net.parry.minSpeed"] = 2.6,
                ["net.parry.launchSpeed"] = 9.0,
                ["net.spike.enabled"] = true,
                ["net.spike.minRungs"] = 2.0,
                ["net.spike.minCatchSpeed"] = 4.6,
                ["net.spike.cooldownSec"] = 20.0,
                ["net.heap.enabled"] = false,
                ["net.heap.max"] = 3.0,
                ["net.heap.calmSpeed"] = 2.2,
                ["net.heap.drainK"] = 0.11,
                ["net.heap.regenPerSec"] = 0.5,
                ["net.heap.shedUpVel"] = 1.4,
                ["net.heap.shedImmunitySec"] = 0.35,
                ["net.heap.pourGreedMult"] = 0.5,
                ["net.catch.maxY"] = 2.0,
                ["net.pour.enabled"] = true,
                ["net.pour.downSpeed"] = 0.6,
                ["net.pour.downSpeedFull"] = 0.28,
                ["net.pour.heightAbove"] = 5.5,
                ["net.pour.faceDownY"] = -0.15,
                ["net.pour.strokeTipY"] = -0.12,
                ["net.pour.facingEaseAlpha"] = 0.2591817793182821,
                ["net.pour.xCapture"] = 1.0,
                ["net.flick.enabled"] = false,
                ["net.dump.dragEnabled"] = false,
                ["net.flick.minSpeed"] = 7.0,
                ["net.flick.coneDeg"] = 55.0,
                ["net.flick.bonusPct"] = 0.15,
                ["temper.perCatch"] = 0.06,
                ["temper.waveBonus"] = 0.15,
                ["temper.trailBonus"] = 0.08,
                ["temper.rimCool"] = 0.15,
                ["temper.missCool"] = 0.5,
                ["temper.decayPerSec"] = 0.03,
                ["temper.scoreBonusMax"] = 0.5,
                ["net.dumpCountBonusPct"] = 0.1,
                ["ceremony.walkArrivalCapSeconds"] = 0.22,
                ["ceremony.anticipateSeconds"] = 0.12,
                ["telegraph.base"] = 0.38,
                ["telegraph.earlyWaveBonus"] = 0.35,
                ["telegraph.lateWaveBonus"] = 0.08,
                ["telegraph.derangementCut"] = 0.18,
                ["telegraph.min"] = 0.32,
                ["telegraph.max"] = 1.15,
                ["spawn.minRatePerSec"] = 0.5,
                ["spawn.baseRatePerSec"] = 1.15,
                ["spawn.warmupGapBoost"] = 1.25,
                ["spawn.warmupDecaySeconds"] = 8.0,
                ["spawn.itemsPerPush"] = 3.0,
                ["spawn.itemsPerPushMax"] = 5.0,
                ["net.capacityFollowsPush"] = true,
                ["spawn.pushSpreadUnits"] = 1.2,
                ["spawn.pushStaggerSeconds"] = 0.12,
                ["world.halfWidth"] = 2.7,
                ["world.shelfY"] = 4.0,
                ["world.floorY"] = -4.2,
                ["world.binX"] = 2.1,
                ["world.binY"] = -3.0,
                ["world.binRadius"] = 0.8,
                ["world.fallGravity"] = -9.5,
                ["world.wallRestitution"] = 0.7,
                ["world.itemRadius"] = 0.22,
                ["input.touchOffsetY"] = 1.15,
                ["input.pointerFollowMul"] = 2.0,
                ["run.derangementRampSeconds"] = 60.0,
                ["run.lives"] = 9.0,
                ["net.rig.hoopRadius"] = 0.5,
                ["net.rig.pouchDepth"] = 1.1,
                ["net.rig.nodesX"] = 8.0,
                ["net.rig.nodesY"] = 6.0,
                ["net.rig.iterations"] = 2.0,
                ["net.rig.damping"] = 0.96,
                ["net.rig.gravity"] = -9.0,
                ["net.rig.taper"] = 0.45,
                ["net.rig.bulgeFromHeld"] = 0.35,
                ["net.rig.swingFromVel"] = 0.14,
                ["net.rig.weightSagFrom01"] = 0.5,
                ["net.rig.heldPullPerItem"] = 0.018,
                ["net.rig.pourTipBias"] = 0.35,
                ["net.rig.breakSlack"] = 0.4,
                ["net.rig.repairSnap"] = 0.2,
                ["net.sweep.policy"] = "fair",
                ["net.sweep.chanceMax"] = 0.35,
                ["net.sweep.countBase"] = 2.0,
                ["net.sweep.countHigh"] = 3.0,
                ["net.sweep.highDerangement"] = 0.6,
                ["net.sweep.staggerSeconds"] = 0.16,
                ["net.sweep.spanUnits"] = 1.8,
                ["net.sweep.cooldownSeconds"] = 9.0,
                ["net.sweep.wavecatchGraceSeconds"] = 1.2,
                ["tempo.director.enabled"] = true,
                ["tempo.baseline"] = 0.35,
                ["tempo.compressThreshold"] = 0.6,
                ["tempo.risePerCatch"] = 0.05,
                ["tempo.risePerWave"] = 0.12,
                ["tempo.risePerTrail"] = 0.08,
                ["tempo.risePerDump"] = 0.04,
                ["tempo.dropPerMiss"] = 0.3,
                ["tempo.rimCool"] = 0.06,
                ["tempo.reboundPerSec"] = 0.06,
                ["tempo.gapCompressMin"] = 0.62,
                ["tempo.gapEaseMax"] = 1.35,
                ["diag.telemetry"] = true,
            };
        }
    }
}
