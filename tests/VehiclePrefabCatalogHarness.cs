using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ImprovedPublicTransport.Data;

internal static class VehiclePrefabCatalogHarness
{
    private static int _index;

    private static void Main()
    {
        var bus = Make<BusAI>("bus", ItemClass.SubService.PublicTransportBus, ItemClass.Level.Level1, VehicleInfo.VehicleType.Car, TransportInfo.TransportType.Bus);
        var busClassTram = Make<BusAI>("bus-class-tram", ItemClass.SubService.PublicTransportTram, ItemClass.Level.Level1, VehicleInfo.VehicleType.Tram, TransportInfo.TransportType.Bus);
        var tramClassBus = Make<TramAI>("tram-class-bus", ItemClass.SubService.PublicTransportBus, ItemClass.Level.Level1, VehicleInfo.VehicleType.Car, TransportInfo.TransportType.Tram);
        var tram = Make<TramAI>("tram", ItemClass.SubService.PublicTransportTram, ItemClass.Level.Level1, VehicleInfo.VehicleType.Tram, TransportInfo.TransportType.Tram);
        var trailer = Make<BusAI>("external-trailer", ItemClass.SubService.PublicTransportBus, ItemClass.Level.Level1, VehicleInfo.VehicleType.Car, TransportInfo.TransportType.Bus);
        var articulated = Make<BusAI>("articulated", ItemClass.SubService.PublicTransportBus, ItemClass.Level.Level1, VehicleInfo.VehicleType.Car, TransportInfo.TransportType.Bus, trailer);
        var self = Make<PassengerTrainAI>("self", ItemClass.SubService.PublicTransportTrain, ItemClass.Level.Level1, VehicleInfo.VehicleType.Train, TransportInfo.TransportType.Train);
        self.m_trailers = new[] { new VehicleInfo.VehicleTrailer { m_info = self } };
        var shared = Make<PassengerTrainAI>("shared", ItemClass.SubService.PublicTransportTrain, ItemClass.Level.Level1, VehicleInfo.VehicleType.Train, TransportInfo.TransportType.Train);
        var middle = Make<PassengerTrainAI>("middle", ItemClass.SubService.PublicTransportTrain, ItemClass.Level.Level1, VehicleInfo.VehicleType.Train, TransportInfo.TransportType.Train, shared);
        var train = Make<PassengerTrainAI>("train", ItemClass.SubService.PublicTransportTrain, ItemClass.Level.Level1, VehicleInfo.VehicleType.Train, TransportInfo.TransportType.Train, middle, shared);
        var cycleA = Make<PassengerTrainAI>("cycle-a", ItemClass.SubService.PublicTransportTrain, ItemClass.Level.Level1, VehicleInfo.VehicleType.Train, TransportInfo.TransportType.Train);
        var cycleB = Make<PassengerTrainAI>("cycle-b", ItemClass.SubService.PublicTransportTrain, ItemClass.Level.Level1, VehicleInfo.VehicleType.Train, TransportInfo.TransportType.Train, cycleA);
        cycleA.m_trailers = new[] { new VehicleInfo.VehicleTrailer { m_info = cycleB } };
        var procedural = Make<BusAI>("procedural", ItemClass.SubService.PublicTransportBus, ItemClass.Level.Level1, VehicleInfo.VehicleType.Car, TransportInfo.TransportType.Bus);
        procedural.m_placementStyle = ItemClass.Placement.Procedural;
        var cargo = Make<CargoTrainAI>("cargo", ItemClass.SubService.PublicTransportTrain, ItemClass.Level.Level1, VehicleInfo.VehicleType.Train, TransportInfo.TransportType.Train);
        var ferry = Make<PassengerShipAI>("ferry", ItemClass.SubService.PublicTransportShip, ItemClass.Level.Level2, VehicleInfo.VehicleType.Ferry, TransportInfo.TransportType.Ship);
        var blimp = Make<PassengerPlaneAI>("blimp", ItemClass.SubService.PublicTransportPlane, ItemClass.Level.Level2, VehicleInfo.VehicleType.Blimp, TransportInfo.TransportType.HotAirBalloon);
        var heli = Make<PassengerHelicopterAI>("heli", ItemClass.SubService.PublicTransportPlane, ItemClass.Level.Level3, VehicleInfo.VehicleType.Helicopter, TransportInfo.TransportType.Helicopter);
        var evacuation = Make<BusAI>("evacuation", ItemClass.SubService.None, ItemClass.Level.Level4, VehicleInfo.VehicleType.Car, TransportInfo.TransportType.EvacuationBus);
        evacuation.m_class.m_service = ItemClass.Service.Disaster;
        ((BusAI)evacuation.m_vehicleAI).m_transportInfo.m_class.m_service = ItemClass.Service.Disaster;
        var legacyEvacuation = Make<BusAI>("legacy-evacuation", ItemClass.SubService.None, ItemClass.Level.Level4, VehicleInfo.VehicleType.Car, TransportInfo.TransportType.Bus);
        legacyEvacuation.m_class.m_service = ItemClass.Service.Disaster;
        ((BusAI)legacyEvacuation.m_vehicleAI).m_transportInfo.m_class.m_service = ItemClass.Service.Disaster;
        var all = new List<VehicleInfo> { null, bus, busClassTram, tramClassBus, tram, trailer, articulated, self, shared, middle, train, cycleA, cycleB, procedural, cargo, ferry, blimp, heli, evacuation, legacyEvacuation };
        var components = VehiclePrefabCatalog.FindComponents(all);
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(bus, components), "bus");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(busClassTram, components), "BusAI Bus never Tram");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(tramClassBus, components), "Tram never Bus");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(articulated, components), "articulated root");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(trailer, components), "external trailer");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(self, components), "self root");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(middle, components), "intermediate");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(shared, components), "shared");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(cycleA, components), "reversible cycle a head");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(cycleB, components), "reversible cycle b head");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(procedural, components), "procedural");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(cargo, components), "cargo separation");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(ferry, components), "ferry");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(blimp, components), "blimp");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(heli, components), "heli");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(evacuation, components), "evacuation bus");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(legacyEvacuation, components), "unproven legacy Bus excluded");
        Expect(false, VehiclePrefabCatalog.IsEligibleRoot(null, components), "null");
        Expect(true, VehiclePrefabCatalog.IsEligibleRoot(bus, null), "null components empty");
        ExpectThrows<ArgumentNullException>(() => VehiclePrefabCatalog.FindComponents(null), "null prefab list");
        var roots = all.Where(x => VehiclePrefabCatalog.IsEligibleRoot(x, components)).ToArray();
        var random = new Random(17);
        for (var i = 0; i < 100; i++)
        {
            Expect(true, VehiclePrefabCatalog.IsEligibleRoot(roots[random.Next(roots.Length)], components), "random root only");
        }
        VerifyProductionStructure();
        Console.WriteLine("VehiclePrefabCatalog harness: PASS");
    }

    private static VehicleInfo Make<TAi>(string name, ItemClass.SubService subService, ItemClass.Level level, VehicleInfo.VehicleType vehicleType, TransportInfo.TransportType transportType, params VehicleInfo[] trailers) where TAi : VehicleAI
    {
        var itemClass = New<ItemClass>();
        itemClass.m_service = ItemClass.Service.PublicTransport;
        itemClass.m_subService = subService;
        itemClass.m_level = level;
        var transportClass = New<ItemClass>();
        transportClass.m_service = itemClass.m_service;
        transportClass.m_subService = subService;
        transportClass.m_level = level;
        var transport = New<TransportInfo>();
        transport.m_class = transportClass;
        transport.m_transportType = transportType;
        transport.m_vehicleType = vehicleType;
        var ai = New<TAi>();
        var field = typeof(TAi).GetField("m_transportInfo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        while (field == null && typeof(TAi).BaseType != null)
        {
            var type = typeof(TAi).BaseType;
            while (type != null && field == null)
            {
                field = type.GetField("m_transportInfo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                type = type.BaseType;
            }
            break;
        }
        field.SetValue(ai, transport);
        var prefab = New<VehicleInfo>();
        prefab.m_prefabDataIndex = _index++;
        prefab.m_class = itemClass;
        prefab.m_vehicleType = vehicleType;
        prefab.m_vehicleAI = ai;
        if (trailers.Length > 0)
        {
            prefab.m_trailers = Array.ConvertAll(trailers, x => new VehicleInfo.VehicleTrailer { m_info = x });
        }
        return prefab;
    }

    private static T New<T>() where T : class
    {
        return (T)FormatterServices.GetUninitializedObject(typeof(T));
    }

    private static void VerifyProductionStructure()
    {
        var root = Path.GetFullPath(Path.Combine(Path.Combine(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."), ".."), ".."), ".."));
        var registry = File.ReadAllText(Path.Combine(Path.Combine(root, "Data"), "VehiclePrefabs.cs"));
        var editor = File.ReadAllText(Path.Combine(Path.Combine(root, "UI"), "VehicleEditor.cs"));
        Expect(true, registry.Contains("private void RegisterPrefab([NotNull] List<PrefabData> list"), "list-aware registration");
        Expect(true, registry.Contains("Rejected conflicting vehicle prefab"), "collision log");
        Expect(true, registry.Contains("!ReferenceEquals(byName.Info, prefab)"), "name collision rejects foreign data");
        Expect(true, registry.Contains("!ReferenceEquals(byIndex.Info, prefab)"), "index collision rejects foreign data");
        Expect(true, registry.Contains("if (!list.Contains(prefabData))"), "same reference reused once");
        Expect(true, registry.Contains("_busPrefabData = busList.ToArray()"), "registered arrays published");
        Expect(true, registry.Contains("return _busPrefabData"), "GetPrefabs integration");
        Expect(true, registry.Contains("return _evacuationBusPrefabData"), "GetPrefabs Disaster integration");
        Expect(true, registry.Contains("service == ItemClass.Service.Disaster && subService == ItemClass.SubService.None"), "GetPrefabs Disaster triplet");
        Expect(true, editor.Contains("VehiclePrefabs.instance.GetPrefabs"), "VehicleEditor catalog consumer");
        Expect(false, editor.Contains("VehiclePrefabCatalog.IsEligibleRoot"), "no duplicate editor filter");
    }

    private static void ExpectThrows<T>(Action action, string name) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception(name + ": expected " + typeof(T).Name);
    }

    private static void Expect(bool expected, bool actual, string name)
    {
        if (expected != actual)
        {
            throw new Exception(name + ": expected " + expected + ", actual " + actual);
        }
    }
}
