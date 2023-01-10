[assembly: Rage.Attributes.Plugin("GtaTrainingHelper", Description = "Setups game for training.", Author = "Jochem Forrez")]
namespace GtaTrainingHelper
{
    using Gta5Driver;
    using Newtonsoft.Json;
    using Rage;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    public class Program
    {
        static Blip blip;
        static Timer blipFixTimer;
        static bool isTraining = false;
        static List<Location> locations = new List<Location> { };
        static string locationPath = Path.Combine(Environment.CurrentDirectory, @"./Plugins/Locations.json");
        private static void Main()
        {
            Game.Console.Print("GtaTrainingHelper Starting.");
            Game.MaxWantedLevel = 0;
            locations = GetLocations();
            blipFixTimer = new Timer(BlipFix, null, 0, 5000);
            while (true)
            {
                if (Game.LocalPlayer.Character.CurrentVehicle)
                {
                    if (isTraining)
                    {
                        if (Game.LocalPlayer.Character.CurrentVehicle.Health < 500)
                        {
                            Game.Console.Print("Vehicle is damaged, resetting.");
                            isTraining = false;
                            GTHSetup();
                        }
                        else if ((int)blip.TravelDistanceTo(Game.LocalPlayer.Character.CurrentVehicle.Position) < 30)
                        {
                            Game.Console.Print("GtaTrainingHelper: Arrived at location.");
                            isTraining = false;
                            GTHSetup();
                        }
                    }
                }
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

        // main setup for training
        [Rage.Attributes.ConsoleCommand]
        public static void GTHSetup()
        {
            Vehicle vehicle = Game.LocalPlayer.Character.CurrentVehicle;
            if (vehicle.Exists())
                vehicle.Delete();
            GTHTeleportToRandomLocation();
            Model carModel = new Model("Buffalo2");
            vehicle = new Vehicle(carModel, Game.LocalPlayer.Character.Position, Game.LocalPlayer.Character.Heading)
            {
                LicensePlate = "AiDriver"
            };
            Game.LocalPlayer.Character.WarpIntoVehicle(vehicle, -1);
            Game.LocalPlayer.Character.CanFlyThroughWindshields = false;
            Game.LocalPlayer.Character.CanBePulledOutOfVehicles = false;
            GTHSetupRandomBlip();
            isTraining = true;
        }

        // setup random blip (waypoint)
        [Rage.Attributes.ConsoleCommand]
        public static void GTHSetupRandomBlip()
        {
            if (blip.Exists())
                blip.Delete();
            Random random = new Random();
            blip = new Blip(locations[random.Next(locations.Count)].Position);
            // to check if blip is not to close to startin location
            while ((int)blip.TravelDistanceTo(Game.LocalPlayer.Character.Position) < 60)
            {
                blip.Delete();
                blip = new Blip(locations[random.Next(locations.Count)].Position);
            }
            blip.IsRouteEnabled = true;
        }

        // setup blip (waypoint) with name
        [Rage.Attributes.ConsoleCommand]
        public static void GTHSetupBlip(string location)
        {
            if (blip.Exists())
                blip.Delete();
            foreach (Location loc in locations)
            {
                if (loc.LocationName.ToLower() == location.ToLower())
                {
                    blip = new Blip(loc.Position);
                    blip.IsRouteEnabled = true;
                }
            }
        }
        // weird bug that removes the blip after a while
        private static void BlipFix(object state)
        {
            if (blip.Exists())
            {
                blip.IsRouteEnabled = true;
            }
        }
        // delete vehicle
        [Rage.Attributes.ConsoleCommand]
        public static void DeleteVehicle()
        {
            Vehicle vehicle = Game.LocalPlayer.Character.CurrentVehicle;

            if (vehicle.Exists())
            {
                Game.Console.Print("deleting vehicle");
                vehicle.Delete();
            }
        }
    }
}
