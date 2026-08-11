from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / "bin" / "Release" / "net35" / "ImprovedPublicTransport4.dll"
GAME = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\Cities_Data\Managed")

SOURCE = r'''
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

static class Harness
{
    static object EnumValue(Assembly game, string type, string value) => Enum.Parse(game.GetType(type, true), value);
    static object Or(object left, object right) => Enum.ToObject(left.GetType(), Convert.ToInt64(left) | Convert.ToInt64(right));
    static object Cost(MethodInfo method, object sub, object level, object vehicle, object settings) =>
        method.Invoke(null, new[] { sub, level, vehicle, settings, (object)false });

    static int Main(string[] args)
    {
        var game = Assembly.LoadFrom(args[0]);
        var mod = Assembly.LoadFrom(args[1]);
        var type = mod.GetType("ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPathCost", true);
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var cost = type.GetMethod("GetCost", flags, null, new[] {
            game.GetType("ItemClass+SubService"), game.GetType("ItemClass+Level"), game.GetType("VehicleInfo+VehicleType"),
            mod.GetType("ImprovedPublicTransport.ModSetting+TicketPriceCustomizerSettings"), typeof(bool) }, null);
        var collect = type.GetMethod("AddPathPositionLaneCost", flags);
        var walk = type.GetMethod("WalkPathPositions", flags);
        if (collect == null || walk == null) throw new Exception("compiled safe path traversal seam missing");
        var settings = Activator.CreateInstance(mod.GetType("ImprovedPublicTransport.ModSetting+TicketPriceCustomizerSettings", true));
        void Set(string name, float value) => settings.GetType().GetProperty(name).SetValue(settings, value, null);
        Set("BusMultiplier", 0f); Set("MetroMultiplier", 2f); Set("ShipMultiplier", .5f); Set("FerryMultiplier", 1.5f);
        Set("SightseeingBusMultiplier", 2f); Set("PlaneMultiplier", 1f); Set("BlimpMultiplier", 1f);
        Set("HelicopterMultiplier", 1f); Set("IntercityBusMultiplier", 1f);
        var bus = EnumValue(game, "ItemClass+SubService", "PublicTransportBus");
        var metro = EnumValue(game, "ItemClass+SubService", "PublicTransportMetro");
        var ship = EnumValue(game, "ItemClass+SubService", "PublicTransportShip");
        var plane = EnumValue(game, "ItemClass+SubService", "PublicTransportPlane");
        var tours = EnumValue(game, "ItemClass+SubService", "PublicTransportTours");
        var level1 = EnumValue(game, "ItemClass+Level", "Level1");
        var level2 = EnumValue(game, "ItemClass+Level", "Level2");
        var level3 = EnumValue(game, "ItemClass+Level", "Level3");
        var car = EnumValue(game, "VehicleInfo+VehicleType", "Car");
        var metroVehicle = EnumValue(game, "VehicleInfo+VehicleType", "Metro");
        var shipVehicle = EnumValue(game, "VehicleInfo+VehicleType", "Ship");
        var planeVehicle = EnumValue(game, "VehicleInfo+VehicleType", "Plane");
        var blimp = EnumValue(game, "VehicleInfo+VehicleType", "Blimp");
        var helicopter = EnumValue(game, "VehicleInfo+VehicleType", "Helicopter");
        if ((byte)Cost(cost, bus, level1, car, settings) != 0) throw new Exception("Bus zero");
        if ((byte)Cost(cost, metro, level1, metroVehicle, settings) != 40) throw new Exception("Metro independent");
        if ((byte)Cost(cost, ship, level1, shipVehicle, settings) != 10) throw new Exception("Ship");
        if ((byte)Cost(cost, ship, level2, shipVehicle, settings) != 30) throw new Exception("Ferry");
        if ((byte)Cost(cost, plane, level1, planeVehicle, settings) != 20) throw new Exception("Plane");
        if ((byte)Cost(cost, plane, level2, blimp, settings) != 20) throw new Exception("Blimp");
        if ((byte)Cost(cost, plane, level3, helicopter, settings) != 20) throw new Exception("Helicopter");
        if ((byte)Cost(cost, tours, level3, car, settings) != 40) throw new Exception("Tours");
        if ((byte)Cost(cost, bus, level2, car, settings) != 20) throw new Exception("Intercity");
        var publicTransport = EnumValue(game, "NetInfo+LaneType", "PublicTransport");
        var vehicle = EnumValue(game, "NetInfo+LaneType", "Vehicle");
        var candidates = new Dictionary<uint, byte>();
        void Add(uint lane, object laneType, object laneVehicle, object lineVehicle, byte value) =>
            collect.Invoke(null, new object[] { lane, laneType, laneVehicle, lineVehicle, value, candidates });
        Add(101, vehicle, car, car, 30);
        Add(202, publicTransport, metroVehicle, metroVehicle, 40);
        Add(303, Or(publicTransport, vehicle), car, car, 20);
        Add(202, publicTransport, metroVehicle, metroVehicle, 10);
        if (candidates.Count != 1 || candidates[202] != 10) throw new Exception("multi-segment physical path/shared minimum");
        var pathUnitType = game.GetType("PathUnit", true);
        var positionType = game.GetType("PathUnit+Position", true);
        var units = Array.CreateInstance(pathUnitType, 4);
        object Position(ushort segment, byte lane) {
            var position = Activator.CreateInstance(positionType);
            positionType.GetField("m_segment").SetValue(position, segment);
            positionType.GetField("m_lane").SetValue(position, lane);
            return position;
        }
        void Unit(int id, byte count, uint next, params object[] positions) {
            var unit = Activator.CreateInstance(pathUnitType);
            pathUnitType.GetField("m_positionCount").SetValue(unit, count);
            pathUnitType.GetField("m_nextPathUnit").SetValue(unit, next);
            for (var i = 0; i < positions.Length; i++) pathUnitType.GetField($"m_position{i:00}").SetValue(unit, positions[i]);
            units.SetValue(unit, id);
        }
        Unit(1, 2, 2, Position(10, 0), Position(11, 1));
        Unit(2, 1, 0, Position(12, 0));
        var seen = new List<string>();
        Action<ushort, byte> visit = (segment, lane) => seen.Add(segment + ":" + lane);
        walk.Invoke(null, new object[] { units, (uint)units.Length, (uint)1, visit });
        if (string.Join(",", seen) != "10:0,11:1,12:0") throw new Exception("real multi-unit traversal");
        Unit(2, 1, 1, Position(12, 0));
        seen.Clear();
        walk.Invoke(null, new object[] { units, (uint)units.Length, (uint)1, visit });
        if (seen.Count != 3) throw new Exception("path cycle not stopped");
        seen.Clear();
        walk.Invoke(null, new object[] { units, (uint)units.Length, (uint)99, visit });
        if (seen.Count != 0) throw new Exception("out-of-bounds path accepted");
        Unit(1, 255, 99, Position(10, 0));
        seen.Clear();
        walk.Invoke(null, new object[] { units, (uint)units.Length, (uint)1, visit });
        if (seen.Count != 12) throw new Exception("position count/next path bounds");
        Console.WriteLine("ticket path cost transport lines: PASS");
        return 0;
    }
}
'''

if not DLL.exists():
    raise SystemExit(f"build output missing: {DLL}")
with tempfile.TemporaryDirectory() as directory:
    source = Path(directory) / "Harness.cs"
    project = Path(directory) / "Harness.csproj"
    source.write_text(SOURCE, encoding="utf-8")
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
    subprocess.run(["dotnet", "run", "--project", str(project), "--configuration", "Release", "--", str(GAME / "Assembly-CSharp.dll"), str(DLL)], check=True, timeout=20)
