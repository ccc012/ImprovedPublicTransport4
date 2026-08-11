param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$gameRoot = "${env:ProgramFiles(x86)}\Steam\steamapps\common\Cities_Skylines"
$gamePath = Join-Path $gameRoot "Cities_Data\Managed"
$mono = Join-Path $gameRoot "Mono\bin\mono.exe"
$compiler = Join-Path $gameRoot "Mono\bin\gmcs.bat"
$assemblyCSharp = Join-Path $gamePath "Assembly-CSharp.dll"
$harmonyAssembly = Join-Path $env:UserProfile ".nuget\packages\citiesharmony.harmony\2.2.2\lib\net35\CitiesHarmony.Harmony.dll"
$modAssembly = Join-Path $root "bin\$Configuration\net35\ImprovedPublicTransport4.dll"
$tempRoot = Join-Path $env:TEMP "ipt4-taxi-probability-harness"

if (!(Test-Path $assemblyCSharp)) { throw "Assembly-CSharp.dll not found: $assemblyCSharp" }
if (!(Test-Path $mono)) { throw "Cities: Skylines Mono runtime not found: $mono" }
if (!(Test-Path $compiler)) { throw "Cities: Skylines Mono compiler not found: $compiler" }
if (!(Test-Path $harmonyAssembly)) { throw "CitiesHarmony.Harmony.dll not found: $harmonyAssembly" }
if (!(Test-Path $modAssembly)) { throw "Build output not found: $modAssembly" }
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

$source = @'
using System;
using System.Reflection;
using System.Runtime.Serialization;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;

internal static class TaxiProbabilityHarness
{
    private static int failures;
    private static ModSetting setting;
    private static SimulationManager simulation;

    private static bool VanillaFixture(ref int __result)
    {
        __result = 60;
        return false;
    }

    private static bool SettingFixture(ref ModSetting __result)
    {
        __result = setting;
        return false;
    }

    private static bool SimulationFixture(ref SimulationManager __result)
    {
        __result = simulation;
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            Console.Error.WriteLine("FAIL: " + message);
            failures++;
        }
    }

    private static int Invoke(MethodInfo target, object instance, Type citizenInstance, object ageGroup)
    {
        object citizen = Activator.CreateInstance(citizenInstance);
        object[] args = ageGroup == null ? new object[] { (ushort)1, citizen } : new object[] { (ushort)1, citizen, ageGroup };
        return (int)target.Invoke(instance, args);
    }

    private static void CheckPatch(MethodInfo target, MethodInfo postfix, string owner)
    {
        Patches patches = Harmony.GetPatchInfo(target);
        Check(patches != null, target.DeclaringType.Name + " PatchInfo missing");
        bool found = false;
        if (patches != null)
        {
            foreach (Patch patch in patches.Postfixes)
            {
                if (patch.owner == owner && patch.PatchMethod == postfix)
                {
                    found = true;
                    break;
                }
            }
        }
        Check(found, target.DeclaringType.Name + " postfix owner/method mismatch");
    }

    public static int Main()
    {
        Assembly game = Assembly.LoadFrom(Environment.GetEnvironmentVariable("IPT4_ASSEMBLY_CSHARP"));
        Type resident = game.GetType("ResidentAI", true);
        Type tourist = game.GetType("TouristAI", true);
        BindingFlags targetFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        MethodInfo residentTarget = resident.GetMethod("GetTaxiProbability", targetFlags, null, new[] { typeof(ushort), game.GetType("CitizenInstance").MakeByRefType(), game.GetType("Citizen").GetNestedType("AgeGroup") }, null);
        MethodInfo touristTarget = tourist.GetMethod("GetTaxiProbability", targetFlags, null, new[] { typeof(ushort), game.GetType("CitizenInstance").MakeByRefType() }, null);
        Check(residentTarget != null, "ResidentAI.GetTaxiProbability real signature missing");
        Check(touristTarget != null, "TouristAI.GetTaxiProbability real signature missing");

        Assembly mod = Assembly.LoadFrom(Environment.GetEnvironmentVariable("IPT4_MOD_ASSEMBLY"));
        Type patch = mod.GetType("ImprovedPublicTransport.Integration.TicketPriceCustomizer.TaxiProbabilityPatch", true);
        MethodInfo postfix = patch.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
        Check(postfix != null, "Taxi probability postfix missing");

        const string owner = "ipt4.taxi-probability.harness";
        Harmony harmony = new Harmony(owner);
        try
        {
            MethodInfo vanillaFixture = typeof(TaxiProbabilityHarness).GetMethod("VanillaFixture", BindingFlags.Static | BindingFlags.NonPublic);
            harmony.Patch(residentTarget, prefix: new HarmonyMethod(vanillaFixture, 0, null, null, null), postfix: new HarmonyMethod(postfix, 0, null, null, null));
            harmony.Patch(touristTarget, prefix: new HarmonyMethod(vanillaFixture, 0, null, null, null), postfix: new HarmonyMethod(postfix, 0, null, null, null));
            harmony.Patch(typeof(ModSetting).GetProperty("Instance").GetGetMethod(), prefix: new HarmonyMethod(typeof(TaxiProbabilityHarness).GetMethod("SettingFixture", BindingFlags.Static | BindingFlags.NonPublic), 0, null, null, null));
            Type singleton = typeof(Singleton<>).MakeGenericType(typeof(SimulationManager));
            harmony.Patch(singleton.GetProperty("instance").GetGetMethod(), prefix: new HarmonyMethod(typeof(TaxiProbabilityHarness).GetMethod("SimulationFixture", BindingFlags.Static | BindingFlags.NonPublic), 0, null, null, null));
            CheckPatch(residentTarget, postfix, owner);
            CheckPatch(touristTarget, postfix, owner);

            setting = new ModSetting();
            setting.TicketPriceCustomizerMode = ModSetting.TicketPriceCustomizerModes.Enabled;
            setting.TicketPriceCustomizer = new ModSetting.TicketPriceCustomizerSettings();
            setting.TicketPriceCustomizer.TaxiMultiplier = 2.5f;
            setting.TicketPriceCustomizer.TaxiNightMultiplier = 0f;
            simulation = (SimulationManager)FormatterServices.GetUninitializedObject(typeof(SimulationManager));
            object residentAI = FormatterServices.GetUninitializedObject(resident);
            object touristAI = FormatterServices.GetUninitializedObject(tourist);
            Type citizenInstance = game.GetType("CitizenInstance");
            Array ages = Enum.GetValues(game.GetType("Citizen").GetNestedType("AgeGroup"));

            simulation.m_isNightTime = false;
            Check(Invoke(residentTarget, residentAI, citizenInstance, ages.GetValue(3)) == 24, "ResidentAI patched day result must use 2.5x");
            Check(Invoke(touristTarget, touristAI, citizenInstance, null) == 24, "TouristAI patched day result must use 2.5x");
            simulation.m_isNightTime = true;
            Check(Invoke(residentTarget, residentAI, citizenInstance, ages.GetValue(3)) == 100, "ResidentAI patched night result must use zero fare");
            Check(Invoke(touristTarget, touristAI, citizenInstance, null) == 100, "TouristAI patched night result must use zero fare");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL: patched real method execution threw: " + exception);
            failures++;
        }
        finally
        {
            harmony.UnpatchAll(owner);
        }

        if (failures != 0) return 1;
        Console.WriteLine("taxi probability harness: PASS");
        return 0;
    }
}
'@

$sourcePath = Join-Path $tempRoot "TaxiProbabilityHarness.cs"
$exePath = Join-Path $tempRoot "TaxiProbabilityHarness.exe"
[System.IO.File]::WriteAllText($sourcePath, $source)
$colossalManaged = Join-Path $gamePath "ColossalManaged.dll"
Copy-Item -LiteralPath $harmonyAssembly -Destination $tempRoot -Force
$compile = Start-Process -FilePath $compiler -ArgumentList @("-out:`"$exePath`"", "-r:`"$harmonyAssembly`"", "-r:`"$assemblyCSharp`"", "-r:`"$colossalManaged`"", "-r:$modAssembly", "`"$sourcePath`"") -Wait -PassThru -NoNewWindow
if ($compile.ExitCode -ne 0) { exit $compile.ExitCode }
$env:IPT4_ASSEMBLY_CSHARP = $assemblyCSharp
$env:IPT4_MOD_ASSEMBLY = $modAssembly
$env:MONO_PATH = "$(Join-Path $gameRoot 'Mono\lib\mono\2.0');$gamePath;$(Split-Path -Parent $modAssembly);$tempRoot"
& $mono $exePath
exit $LASTEXITCODE
