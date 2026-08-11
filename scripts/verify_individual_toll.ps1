param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$gamePath = "C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\Cities_Data\Managed"
$modAssembly = Join-Path $root "bin\$Configuration\net35\ImprovedPublicTransport4.dll"
$harmonyAssembly = Join-Path $env:USERPROFILE ".nuget\packages\citiesharmony.harmony\2.2.2\lib\net35\CitiesHarmony.Harmony.dll"
$tempRoot = Join-Path $env:TEMP ("ipt4-individual-toll-harness-" + [Guid]::NewGuid().ToString("N"))

foreach ($path in @($modAssembly, $harmonyAssembly, (Join-Path $gamePath "Assembly-CSharp.dll"))) {
    if (!(Test-Path $path)) { throw "Required assembly not found: $path" }
}
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
Copy-Item (Join-Path $gamePath "*.dll") $tempRoot
Copy-Item (Join-Path (Split-Path $modAssembly) "*.dll") $tempRoot -Force
Copy-Item $harmonyAssembly $tempRoot -Force

$source = @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

internal static class IndividualTollHarness
{
    private static int failures;
    private static readonly Type[] Signature = { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(ushort), typeof(ushort), typeof(int) };

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            Console.Error.WriteLine("FAIL: " + message);
            failures++;
        }
    }

    private static MethodInfo Method(Type type, string name)
    {
        return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, Signature, null);
    }

    private static void InvokePatched(MethodInfo method)
    {
        object ai = FormatterServices.GetUninitializedObject(method.DeclaringType);
        object[] args = { (ushort)1, new Vehicle(), (ushort)0, (ushort)1, 100 };
        method.Invoke(ai, args);
    }

    public static int Main()
    {
        Assembly mod = Assembly.LoadFrom(Environment.GetEnvironmentVariable("IPT4_MOD_ASSEMBLY"));
        Type patch = mod.GetType("ImprovedPublicTransport.HarmonyPatches.TollBoothAIPatches.IndividualTollPatch", true);
        BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        MethodInfo basePrefix = patch.GetMethod("EnterTollRoadPrefix", flags);
        MethodInfo passengerPrefix = patch.GetMethod("PassengerCarEnterTollRoadPrefix", flags);
        MethodInfo cargoPrefix = patch.GetMethod("CargoTruckEnterTollRoadPrefix", flags);
        MethodInfo transpiler = patch.GetMethod("UpdatePathCostTranspiler", flags);
        MethodInfo laneCost = patch.GetMethod("GetLaneCost", flags);
        MethodInfo baseMethod = Method(typeof(VehicleAI), "EnterTollRoad");
        MethodInfo passengerMethod = Method(typeof(PassengerCarAI), "EnterTollRoad");
        MethodInfo cargoMethod = Method(typeof(CargoTruckAI), "EnterTollRoad");
        MethodInfo updatePathCost = typeof(TollBoothAI).GetMethod("UpdatePathCost", BindingFlags.Instance | BindingFlags.NonPublic);

        Check(baseMethod.DeclaringType == typeof(VehicleAI), "VehicleAI base method missing");
        Check(passengerMethod.DeclaringType == typeof(PassengerCarAI), "PassengerCarAI override missing");
        Check(cargoMethod.DeclaringType == typeof(CargoTruckAI), "CargoTruckAI override missing");
        Check(new[] { typeof(PassengerCarAI), typeof(CargoTruckAI) }.SequenceEqual(
            typeof(VehicleAI).Assembly.GetTypes().Where(t => t != typeof(VehicleAI) && typeof(VehicleAI).IsAssignableFrom(t))
                .Where(t => { MethodInfo m = Method(t, "EnterTollRoad"); return m != null && m.DeclaringType == t; })
                .OrderBy(t => t == typeof(PassengerCarAI) ? 0 : 1)), "override set changed");

        MethodInfo[] originals = { baseMethod, passengerMethod, cargoMethod };
        MethodInfo[] prefixes = { basePrefix, passengerPrefix, cargoPrefix };
        for (int i = 0; i < originals.Length; i++)
        {
            object[] args = { new Vehicle(), (ushort)0, 100 };
            Check(!(bool)prefixes[i].Invoke(null, args), originals[i].DeclaringType.Name + " prefix must replace vanilla exactly once");
        }

        object[] inactiveArgs = { new Building { m_flags = Building.Flags.None, m_education1 = 255 } };
        object[] freeArgs = { new Building { m_flags = Building.Flags.Active, m_education1 = 0 } };
        object[] pricedArgs = { new Building { m_flags = Building.Flags.Active, m_education1 = 10 } };
        Check((byte)laneCost.Invoke(null, inactiveArgs) == 0, "inactive building lane cost must be zero");
        Check((byte)laneCost.Invoke(null, freeArgs) == 0, "zero-price lane cost must be zero");
        Check((byte)laneCost.Invoke(null, pricedArgs) == 5, "vanilla lane-cost scale changed");

        Type methodPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher", true);
        ConstructorInfo constructor = methodPatcher.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(MethodBase), typeof(MethodBase), typeof(List<MethodInfo>), typeof(List<MethodInfo>), typeof(List<MethodInfo>), typeof(List<MethodInfo>), typeof(bool) }, null);
        object patcher = constructor.Invoke(new object[] { updatePathCost, null, new List<MethodInfo>(), new List<MethodInfo>(), new List<MethodInfo> { transpiler }, new List<MethodInfo>(), false });
        MethodInfo createReplacement = methodPatcher.GetMethod("CreateReplacement", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] replacementArgs = { null };
        MethodInfo replacement = (MethodInfo)createReplacement.Invoke(patcher, replacementArgs);
        IDictionary finalInstructions = (IDictionary)replacementArgs[0];
        Check(replacement != null, "Harmony dynamic replacement missing");
        Check(finalInstructions.Values.Cast<CodeInstruction>().Count(i => Equals(i.operand, laneCost)) == 1, "dynamic replacement does not call GetLaneCost exactly once");
        CodeInstruction helperCall = finalInstructions.Values.Cast<CodeInstruction>().Single(i => Equals(i.operand, laneCost));
        CodeInstruction helperArgument = finalInstructions.Values.Cast<CodeInstruction>().TakeWhile(i => !ReferenceEquals(i, helperCall)).Last();
        Check(helperArgument.opcode == System.Reflection.Emit.OpCodes.Ldarg_2, "dynamic replacement must load ref Building from arg2");

        Harmony harmony = new Harmony("ipt4.individual-toll.harness");
        harmony.Patch(updatePathCost, transpiler: new HarmonyMethod(transpiler, -1, null, null, null));
        Patches applied = Harmony.GetPatchInfo(updatePathCost);
        Check(applied != null && applied.Transpilers.Count(p => p.PatchMethod == transpiler) == 1, "Harmony.Patch did not apply transpiler exactly once");
        harmony.UnpatchAll(harmony.Id);

        if (failures != 0) return 1;
        Console.WriteLine("individual toll harness: PASS");
        return 0;
    }
}
'@

$sourcePath = Join-Path $tempRoot "IndividualTollHarness.cs"
$exePath = Join-Path $tempRoot "IndividualTollHarness.exe"
[System.IO.File]::WriteAllText($sourcePath, $source)
$monoBin = "C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\Mono\bin"
$gmcs = Join-Path $monoBin "gmcs.bat"
$monoExe = Join-Path $monoBin "mono.exe"
if (!(Test-Path $gmcs)) { throw "gmcs.bat not found: $gmcs" }
$bclNames = @("mscorlib")
$references = Get-ChildItem $tempRoot -Filter "*.dll" | Where-Object { $bclNames -notcontains $_.BaseName } | ForEach-Object { "/reference:`"$($_.FullName)`"" }
& $gmcs /nologo "/out:$exePath" $references $sourcePath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$env:IPT4_MOD_ASSEMBLY = Join-Path $tempRoot "ImprovedPublicTransport4.dll"
try {
    & $monoExe $exePath
    exit $LASTEXITCODE
}
finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
