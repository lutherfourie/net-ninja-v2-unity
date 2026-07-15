using System;
using NetNinja.Core;
using NetNinja.Core.Personas;
var sim = new Sim(CoreConfig.CreateDefault(), 42);
var bot = new PersonaDriver(""perfect"", 42);
for (int i = 0; i < 5; i++) {
  bot.Drive(sim, Sim.FixedDt);
  sim.Step();
  Console.WriteLine($""{i} {sim.HashState()} t={sim.Time} phase={WaveManager.PhaseName(sim.Cat.Phase)} net=({sim.Net.Pos.X},{sim.Net.Pos.Y}) tgt=({sim.Net.Target.X},{sim.Net.Target.Y})"");
}
