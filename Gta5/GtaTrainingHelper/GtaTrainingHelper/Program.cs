[assembly: Rage.Attributes.Plugin("GtaTrainingHelper", Description = "Setups game for training.", Author = "Jochem Forrez")]
namespace GtaTrainingHelper
{
    using Gta5Driver;
    using Newtonsoft.Json;
    using Rage;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class Program
    {
        static Blip blip;
        static Timer blipFixTimer;
        static bool isTraining = false;
        static List<Location> locations = new List<Location> { };
        static string locationPath = Path.Combine(Environment.CurrentDirectory, @"./Plugins/Locations.json");
        static string statsPath = Path.Combine(Environment.CurrentDirectory, @"./Plugins/Stats.json");
        static PythonData pythonData = new PythonData();
        static int vehicleHealth;
        static int distance;
        static IPEndPoint ipEndPoint;
        static DateTime startTime;
        static Stats stats;
        private static void Main()
        {
            Game.Console.Print("GtaTrainingHelper Starting.");
            LoadStats();
            Game.MaxWantedLevel = 0;
            Game.LocalPlayer.IsInvincible = true;
            Game.LocalPlayer.IsIgnoredByEveryone = true;
            locations = GetLocations();
            blipFixTimer = new Timer(BlipFix, null, 0, 5000);
            
            while (true)
            {
                if (Game.LocalPlayer.Character.CurrentVehicle)
                {
                    if (isTraining)
                    {
                        int healthDifference = vehicleHealth - Game.LocalPlayer.Character.CurrentVehicle.Health;
                        pythonData.Distance += distance - (int)blip.TravelDistanceTo(Game.LocalPlayer.Character.CurrentVehicle.Position);
                        distance = (int)blip.TravelDistanceTo(Game.LocalPlayer.Character.CurrentVehicle.Position);
                        if (healthDifference > 0)
                        {
                            stats.TotalDamage += healthDifference;
                            pythonData.Damage += healthDifference;
                            Game.Console.Print($"GtaTrainingHelper: Vehicle has taken damage: {healthDifference}.");
                        }
                        vehicleHealth = Game.LocalPlayer.Character.CurrentVehicle.Health;
                        if (Game.LocalPlayer.Character.CurrentVehicle.Health < 500)
                        {
                            stats.TotalFailedRuns++;
                            Game.Console.Print("GtaTrainingHelper: vehicle has to much damage");
                            isTraining = false;
                            pythonData.HardReset = true;
                            GTHSetup();
                        }
                        else if (Game.LocalPlayer.Character.CurrentVehicle.HasBeenDisabledByWater)
                        {
                            stats.TotalFailedRuns++;
                            stats.TotalWaterDamage++;
                            Game.Console.Print("GtaTrainingHelper: Vehicle has been disabled by water, resetting.");
                            pythonData.HardReset = true;
                            GTHSetup();
                        }
                        else if (Game.LocalPlayer.Character.CurrentVehicle.IsUpsideDown)
                        {
                            stats.TotalFailedRuns++;
                            stats.TotalUpsideDown++;
                            Game.Console.Print("GtaTrainingHelper: Vehicle is upside down, resetting.");
                            pythonData.HardReset = true;
                            GTHSetup();
                        }
                        else if ((int)blip.TravelDistanceTo(Game.LocalPlayer.Character.CurrentVehicle.Position) < 50)
                        {
                            Game.Console.Print("GtaTrainingHelper: Arrived at location.");
                            isTraining = false;
                            pythonData.HardReset = true;
                            pythonData.Success = true;
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
        public static Location GTHTeleportToRandomLocation()
        {
            Game.Console.Print("Teleporting to random location.");
            Random random = new Random();
            int index = random.Next(locations.Count);
            Game.LocalPlayer.Character.Position = locations[index].Position;
            Game.LocalPlayer.Character.Heading = locations[index].Heading;
            return locations[index];
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
            pythonData.Success = false;
            pythonData.Damage = 0;
            pythonData.HardReset = false;
            if (startTime.ToString() == "1/1/0001 12:00:00 AM")
            {
                startTime = DateTime.UtcNow;
                ThreadStart socketThreadStart = new ThreadStart(SocketLoop);
                GameFiber.StartNew(socketThreadStart);
            }
            Vehicle vehicle = Game.LocalPlayer.Character.CurrentVehicle;
            if (vehicle.Exists())
                vehicle.Delete();
            World.CleanWorld(true, true, true, true, true, true);
            Location location = GTHTeleportToRandomLocation();
            Model carModel = new Model("Buffalo2");
            vehicle = new Vehicle(carModel, Game.LocalPlayer.Character.Position, Game.LocalPlayer.Character.Heading)
            {
                LicensePlate = "AiDriver"
            };
            Game.LocalPlayer.Character.WarpIntoVehicle(vehicle, -1);
            Game.LocalPlayer.Character.CanFlyThroughWindshields = false;
            Game.LocalPlayer.Character.CanBePulledOutOfVehicles = false;
            
            GTHSetupRandomBlip();
            vehicleHealth = vehicle.Health;
            distance = (int)blip.TravelDistanceTo(Game.LocalPlayer.Character.CurrentVehicle.Position);
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
            blip.Color = Color.Red;
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
        public static void GTHDeleteVehicle()
        {
            Vehicle vehicle = Game.LocalPlayer.Character.CurrentVehicle;
            if (vehicle.Exists())
            {
                Game.Console.Print("deleting vehicle");
                vehicle.Delete();
            }
        }

        [Rage.Attributes.ConsoleCommand]
        public static void GTHCleanUp()
        {
            DateTime endTime = DateTime.UtcNow;
            TimeSpan ts = endTime - startTime;
            double timeMinutes = ts.TotalMinutes;
            stats.TimeRan = timeMinutes + stats.TimeRan;
            SaveStats();
            Game.Console.Print(stats.TimeRan.ToString());
            if (Game.LocalPlayer.Character.CurrentVehicle.Exists())
                Game.LocalPlayer.Character.CurrentVehicle.Delete();
            isTraining = false;
            Blip[] blips = World.GetAllBlips();
            foreach (Blip blip in blips)
            {
                blip.Delete();
            }
            World.CleanWorld(true, true, true, true, true, true);
            Game.UnloadActivePlugin();
        }

        private static void SaveStats()
        {
            var json = JsonConvert.SerializeObject(stats);
            File.WriteAllText(statsPath, json);
        }
        
        private static void LoadStats()
        {
            JsonSerializer serializer = new JsonSerializer();
            if (!File.Exists(statsPath))
            {
                stats = new Stats();
                stats.TimeRan = 0;
                stats.TotalSuccessfulRuns = 0;
                stats.TotalFailedRuns = 0;
                stats.TotalDamage = 0;
                stats.TotalWaterDamage = 0;
                stats.TotalUpsideDown = 0;
                stats.DistanceTraveled = 0;
            }
            else
            {
                if (File.Exists(statsPath))
                {
                    var json = File.ReadAllText(statsPath);
                    stats = JsonConvert.DeserializeObject<Stats>(json);
                }
            }
        }
        
        private static Socket CreateSocket()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            ipEndPoint = new IPEndPoint(ipAddress, 5000);
            Socket client = new Socket(
                    ipEndPoint.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp);
            client.Connect(ipEndPoint);
            return client;
        }
        
        private static void SendData(Socket client, PythonData pythonData)
        {
            string json = JsonConvert.SerializeObject(pythonData);
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(json);
            _ = client.Send(messageBytes);
        }

        private static void SocketLoop()
        {
            Socket client = CreateSocket();
            while (true)
            {
                var buffer = new List<byte>();
                while (client.Available > 0)
                {
                    var currByte = new Byte[1];
                    var byteCounter = client.Receive(currByte, currByte.Length, SocketFlags.None);

                    if (byteCounter.Equals(1))
                    {
                        buffer.Add(currByte[0]);
                    }
                }
                if (buffer.Count > 0)
                {
                    string data = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
                    if (data == "true")
                    {
                        SendData(client, pythonData);
                        pythonData.Success = false;
                        pythonData.Distance = 0;
                        pythonData.Damage = 0;
                        pythonData.HardReset = false;
                    }
                }
                GameFiber.Yield();
            }
        }
    }
}
