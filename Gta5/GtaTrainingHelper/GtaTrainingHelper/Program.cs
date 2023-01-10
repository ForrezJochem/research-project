[assembly: Rage.Attributes.Plugin("GtaTrainingHelper", Description = "Setups game for training.", Author = "Jochem Forrez")]
namespace GtaTrainingHelper
{
    using Gta5Driver;
    using Newtonsoft.Json;
    using Rage;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Program
    {
        static List<Location> locations = new List<Location> { };
        static string locationPath = Path.Combine(Environment.CurrentDirectory, @"./Plugins/Locations.json");
        private static void Main()
        {
            Game.Console.Print("GtaTrainingHelper Starting.");
            locations = GetLocations();
            while (true)
            {
                GameFiber.Yield();
            }
        }

        // add location to a json file
        [Rage.Attributes.ConsoleCommand]
        public static void GTHAddLocation(string name)
        {
            Game.Console.Print("Adding location: " + name);
            Game.Console.Print(locationPath);
            Location location = new Location();
            location.LocationName = name;
            location.Position = Game.LocalPlayer.Character.Position;
            location.Heading = Game.LocalPlayer.Character.Heading;
            locations.Add(location);
            Game.Console.Print(locations.Count.ToString());
            SaveLocations(locations);
        }

        // load all the location from json file 
        private static List<Location> GetLocations()
        {
            List<Location> locations = new List<Location>();
            if (File.Exists(locationPath))
            {
                var json = File.ReadAllText(locationPath);
                locations = JsonConvert.DeserializeObject<List<Location>>(json);
            }
            return locations;
        }

        // save all the location to json file
        private static void SaveLocations(List<Location> locations)
        {
            var json = JsonConvert.SerializeObject(locations);
            if (File.Exists(locationPath))
            {
                File.WriteAllText(locationPath, json);
            }
        }

        // teleport to a random location
        [Rage.Attributes.ConsoleCommand]
        public static void GTHTeleportToRandomLocation()
        {
            Game.Console.Print("Teleporting to random location.");
            Game.Console.Print(locations.Count.ToString());
            Random random = new Random();
            int index = random.Next(locations.Count);
            Game.LocalPlayer.Character.Position = locations[index].Position;
            Game.LocalPlayer.Character.Heading = locations[index].Heading;
        }

        // teleport to a location
        [Rage.Attributes.ConsoleCommand]
        public static void GTHTeleportToLocation(string location)
        {
            Game.Console.Print("Teleporting to location: " + location);
            Game.Console.Print(locations.Count.ToString());
            foreach (Location loc in locations)
            {
                if (loc.LocationName.ToLower() == location.ToLower())
                {
                    Game.LocalPlayer.Character.Position = loc.Position;
                    Game.LocalPlayer.Character.Heading = loc.Heading;
                }
            }
        }

    }
}
