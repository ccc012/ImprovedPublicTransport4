using System;
using System.IO;
using ImprovedPublicTransport.Util;
using Random = UnityEngine.Random;

namespace AutoLineColor.Naming
{
    public static class GenericNames
    {
        private static string[] _names;
        private const string DefaultNames =
            "Alpha,Bravo,Charlie,Delta,Echo,Foxtrot,Golf,Hotel,India,Juliet,Kilo,Lima,Mike," +
            "November,Oscar,Papa,Quebec,Romeo,Sierra,Tango,Uniform,Victor,Whiskey,Yankee,Zulu," +
            "Adams,Boston,Chicago,Denver,Easy,Frank,George,Henry,Ida,John,King,Lincoln,Mary," +
            "New,Ocean,Peter,Queen,Roger,Sugar,Thomas,Union,Victor,William,Young,Zero";

        public static void Initialize()
        {
            var logger = Console.Instance;
            var modSettingsPath = Path.Combine(Path.Combine(Utils.AssemblyPath, "Resources"), "AutoLineColor");
            var fullPath = Path.Combine(modSettingsPath, "GenericLineNames.txt");
            var unparsedNames = DefaultNames;

            try
            {
                if (File.Exists(fullPath))
                {
                    unparsedNames = File.ReadAllText(fullPath);
                }
                else
                {
                    logger.Message("No names found, writing default values to " + fullPath);
                    Directory.CreateDirectory(modSettingsPath);
                    File.WriteAllText(fullPath, unparsedNames);
                }
            }
            catch (Exception ex)
            {
                logger.Error("error reading names from disk " + ex);
            }

            _names = unparsedNames.Split(new[] { "\n", "\r", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string GetGenericName(int count)
        {
            var name = _names[Random.Range(0, _names.Length)];
            while (count > 1)
            {
                name += " " + _names[Random.Range(0, _names.Length)];
                count--;
            }
            return name;
        }
    }
}
