from pathlib import Path
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE_FILE = ROOT / "Integration" / "TicketPriceCustomizer" / "TicketPathCost.cs"
DLL = ROOT / "bin" / "Release" / "net35" / "ImprovedPublicTransport4.dll"
GAME = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\Cities_Data\Managed")
HARMONY = Path.home() / ".nuget" / "packages" / "citiesharmony.harmony" / "2.2.2" / "lib" / "net35" / "CitiesHarmony.Harmony.dll"
GAME_ROOT = GAME.parents[1]
COMPILER = GAME_ROOT / "Mono" / "bin" / "gmcs.bat"
MONO = GAME_ROOT / "Mono" / "bin" / "mono.exe"

SOURCE = r'''
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using ColossalFramework;
using HarmonyLib;

static class Harness
{
    static SimulationManager simulation;
    static Action queued;

    static bool SimulationFixture(ref SimulationManager __result)
    {
        __result = simulation;
        return false;
    }

    static bool AddActionFixture(Action action)
    {
        queued = action;
        return false;
    }

    static void RunQueued()
    {
        var action = queued;
        if (action == null) throw new Exception("simulation action not queued");
        queued = null;
        try
        {
            action();
        }
        catch (MissingMethodException)
        {
        }
    }

    static int Main(string[] args)
    {
        Assembly.LoadFrom(args[0]);
        var mod = Assembly.LoadFrom(args[1]);
        var type = mod.GetType("ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPathCost", true);
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var harmony = new Harmony("ipt4.ticket-path-cost.lifecycle");
        simulation = (SimulationManager)FormatterServices.GetUninitializedObject(typeof(SimulationManager));
        var singleton = typeof(Singleton<>).MakeGenericType(typeof(SimulationManager));
        harmony.Patch(singleton.GetProperty("instance").GetGetMethod(), prefix: new HarmonyMethod(typeof(Harness).GetMethod("SimulationFixture", flags), 0, null, null, null));
        harmony.Patch(typeof(SimulationManager).GetMethod("AddAction", new[] { typeof(Action) }), prefix: new HarmonyMethod(typeof(Harness).GetMethod("AddActionFixture", flags), 0, null, null, null));

        try
        {
            var begin = type.GetMethod("BeginLevel", BindingFlags.Public | BindingFlags.Static);
            var end = type.GetMethod("EndLevel", BindingFlags.Public | BindingFlags.Static);
            if (begin == null || end == null) throw new Exception("public lifecycle API missing");

            begin.Invoke(null, null);
            var timer = Stopwatch.StartNew();
            end.Invoke(null, null);
            timer.Stop();
            if (timer.ElapsedMilliseconds > 100) throw new Exception("EndLevel blocked: " + timer.ElapsedMilliseconds);
            if ((bool)type.GetField("_active", flags).GetValue(null)) throw new Exception("lifecycle ran outside simulation action");
            begin.Invoke(null, null);
            RunQueued();
            if (!(bool)type.GetField("_active", flags).GetValue(null)) throw new Exception("stale EndLevel deactivated newer lifecycle");

            Console.WriteLine("ticket path cost lifecycle: PASS");
            return 0;
        }
        finally
        {
            harmony.UnpatchAll("ipt4.ticket-path-cost.lifecycle");
        }
    }
}
'''

source_text = SOURCE_FILE.read_text(encoding="utf-8-sig")
for forbidden in ("WaitOne(", "Thread.Sleep(", "EndLevelWaitMilliseconds", "CompletionToken"):
    if forbidden in source_text:
        raise SystemExit(f"blocking lifecycle primitive remains: {forbidden}")
stale_end = source_text[source_text.index("case CommandType.End:"):source_text.index("case CommandType.Apply:")]
if "AppliedCosts.Clear()" in stale_end or "PreviousCosts.Clear()" in stale_end:
    raise SystemExit("stale End command clears ownership dictionaries")

if not DLL.exists():
    raise SystemExit(f"build output missing: {DLL}")
with tempfile.TemporaryDirectory() as directory:
    source = Path(directory) / "Harness.cs"
    executable = Path(directory) / "Harness.exe"
    source.write_text(SOURCE, encoding="utf-8")
    subprocess.run([str(COMPILER), f"-out:{executable}", f"-r:{GAME / 'Assembly-CSharp.dll'}", f"-r:{GAME / 'ColossalManaged.dll'}", f"-r:{HARMONY}", str(source)], check=True)
    environment = os.environ.copy()
    environment["MONO_PATH"] = f"{GAME_ROOT / 'Mono' / 'lib' / 'mono' / '2.0'};{GAME};{DLL.parent};{HARMONY.parent}"
    subprocess.run([str(MONO), str(executable), str(GAME / "Assembly-CSharp.dll"), str(DLL)], check=True, env=environment)
