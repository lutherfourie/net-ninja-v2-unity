// Persona specs + BotInput at the IInputReader seam.
// Port of net-lab src/adapters/bots.ts (intentMotor path used by golden cells).
using NetNinja.Contracts;

namespace NetNinja.Core.Personas
{
    public sealed class PersonaSpec
    {
        public string Name;
        public double ReactionSeconds;
        public double MaxSpeed;
        public double Noise;
        public bool PointerLike;
        public bool UseIntentMotor;
        public MotorParams Motor;
    }

    public static class Personas
    {
        public static readonly PersonaSpec Perfect = new PersonaSpec
        {
            Name = "perfect",
            ReactionSeconds = 0,
            MaxSpeed = 999,
            Noise = 0,
            PointerLike = true,
            UseIntentMotor = true,
            Motor = new MotorParams
            {
                Dfloor = 0.15, Djitter = 0.05, FittsA = 0.08, FittsB = 0.12, SdnK = 0.02,
                LeadWu = 0.5, MisorderRate = 0,
                CorrectionRate = 0.12, PrimaryUndershoot = 0.08, CorrectionGain = 0.80,
                PressureCoupling = 0.5, MaxCorrections = 1,
            },
        };

        public static readonly PersonaSpec PerfectNoCorr = new PersonaSpec
        {
            Name = "perfect-nocorr",
            ReactionSeconds = 0,
            MaxSpeed = 999,
            Noise = 0,
            PointerLike = true,
            UseIntentMotor = true,
            Motor = new MotorParams
            {
                Dfloor = 0.15, Djitter = 0.05, FittsA = 0.08, FittsB = 0.12, SdnK = 0.02,
                LeadWu = 0.5, MisorderRate = 0,
            },
        };

        public static readonly PersonaSpec Average = new PersonaSpec
        {
            Name = "average",
            ReactionSeconds = 0.08,
            MaxSpeed = 12,
            Noise = 0.02,
            UseIntentMotor = true,
            Motor = new MotorParams
            {
                Dfloor = 0.30, Djitter = 0.12, FittsA = 0.12, FittsB = 0.17, SdnK = 0.07,
                LeadWu = 0.50, MisorderRate = 0.06,
                CorrectionRate = 0.90, PrimaryUndershoot = 0.34, CorrectionGain = 1.35,
                PressureCoupling = 1.8, MaxCorrections = 5,
            },
        };

        public static readonly PersonaSpec Sloopy = new PersonaSpec
        {
            Name = "sloppy",
            ReactionSeconds = 0.34,
            MaxSpeed = 6.5,
            Noise = 0.14,
            UseIntentMotor = true,
            Motor = new MotorParams
            {
                Dfloor = 0.48, Djitter = 0.25, FittsA = 0.15, FittsB = 0.26, SdnK = 0.13,
                LeadWu = 0.42, MisorderRate = 0.14,
                CorrectionRate = 1.0, PrimaryUndershoot = 0.50, CorrectionGain = 1.9,
                PressureCoupling = 2.6, MaxCorrections = 9,
            },
        };

        // Alias fixing typo
        public static PersonaSpec Sloppy => Sloopy;

        public static PersonaSpec Get(string id)
        {
            switch (id)
            {
                case "perfect": return Perfect;
                case "perfect-nocorr": return PerfectNoCorr;
                case "average": return Average;
                case "sloppy": return Sloppy;
                default: throw new System.ArgumentException("unknown persona: " + id);
            }
        }
    }

    /// <summary>
    /// Engine-free persona driver implementing IInputReader.
    /// drive(sim) then returns the target as InputFrame (pointerLike for perfect).
    /// </summary>
    public sealed class PersonaDriver : IInputReader
    {
        readonly PersonaSpec _spec;
        readonly IntentMotorDriver _motor;
        int _tick;

        public PersonaDriver(string personaId, int seed)
        {
            _spec = Personas.Get(personaId);
            if (!_spec.UseIntentMotor || _spec.Motor == null)
                throw new System.InvalidOperationException("golden personas require intentMotor");
            _motor = new IntentMotorDriver(_spec.Motor, seed);
        }

        public PersonaDriver(PersonaSpec spec, int seed)
        {
            _spec = spec;
            _motor = new IntentMotorDriver(spec.Motor, seed);
        }

        /// <summary>TS path: bot.drive(sim, dt) then sim.step(). Sets net.target in place.</summary>
        public void Drive(Sim sim, double dt) => _motor.Drive(sim, dt);

        public InputFrame Read(int tick, double simTime)
        {
            // Standalone Read requires a sim reference; use Drive(Sim) for parity harness.
            return new InputFrame(0, -2.5, _spec.PointerLike, tick);
        }

        public InputFrame DriveAndRead(Sim sim)
        {
            _motor.Drive(sim, Sim.FixedDt);
            _tick++;
            return new InputFrame(sim.Net.Target.X, sim.Net.Target.Y, _spec.PointerLike, _tick);
        }
    }
}
