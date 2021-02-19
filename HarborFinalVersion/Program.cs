using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Hamnen
{

    public enum BoatType
    {
        RowBoat,
        MotorBoat,
        SailBoat,
        CargoShip
    }

    class Boat
    {
        public string ID { get; set; }
        public int Weight { get; set; }
        public int MaxSpeed { get; set; }
        public double SlotSize { get; set; }
        public int DockingDays { get; set; }
        public string CurrentSlotID { get; set; }
        public BoatType Type { get; set; }
        public int UniqueProperty { get; set; }

    }

    class Slot
    {
        public string ID { get; set; }
        public int Number { get; set; }
        public double SlotSize { get; set; }
        public int DaysToGo { get; set; }
        public List<Boat> Slots { get; set; } //List of slots, where rowboats can be saved on the same slot
        public Slot(int number)
        {
            Slots = new List<Boat>();
            Number = number;
        }
        public Slot()
        {
            Slots = new List<Boat>();
        }

    }

    class Hamn
    {
        public string Name { get; set; }
        public double Slots { get; set; }
        public List<Slot> SlotBlocks { get; set; }
    }

    class Program
    {
        static Hamn hamn = new Hamn();

        static List<Boat> boats = new List<Boat>();

        static List<Boat> boatsThatDidNotFit = new List<Boat>();

        static Random random = new Random();

        static int daysToSimulate = 30;

        static int boatsAday = 5;

        static double slotsTaken = 0;

        static int currentDay = 1;

        static void Main(string[] args)
        {
            hamn.Name = "Port";
            hamn.Slots = 64.0;
            hamn.SlotBlocks = new List<Slot>();

            //Generate slots
            for (int i = 0; i < 64; i++)
            {
                hamn.SlotBlocks.Add(new Slot(i));
            }

            if(CheckSavedDay() == daysToSimulate)
            {
                currentDay = 1;
                DeleteSavedBoats();
            }

            if (ReadFile() > 0)
            {
                Console.WriteLine($"\nDAY {currentDay}\n");
                Print();
                Console.WriteLine("Press any key to go to the next day");
            }
            else
                Console.WriteLine("Welcome! Press any key to get started.");
            if (currentDay > 1) currentDay++;

            Console.ReadKey();
            for (int i = currentDay; i <= daysToSimulate; i++)
            {
                //Remove boats
                foreach (var item in hamn.SlotBlocks)
                {
                    item.DaysToGo--;
                }

                for (int m = 0; m < 64; m++)
                {
                    if (hamn.SlotBlocks[m].DaysToGo == 0)
                    {
                        hamn.SlotBlocks[m].Slots.Clear();

                        //Leaving boats should be removed from slotsTaken
                        foreach (var boat in hamn.SlotBlocks[m].Slots)
                        {
                            slotsTaken -= boat.SlotSize;
                        }
                    }
                }

                Console.WriteLine($"\nDAY {currentDay}\n");

                //Boats arrival
                for (int b = 0; b < boatsAday; b++)
                {
                    //Create boat
                    Boat newBoat = GetBoat((BoatType)random.Next(0, 4)); //Random boat arrives to the harbor

                    //Checks if the boat fits
                    //Handles rowboats
                    if (newBoat.Type == BoatType.RowBoat)
                    {
                        bool keepGoing = true;
                        foreach (var slotItem in hamn.SlotBlocks)
                        {
                            if (slotItem.Slots.Count() == 1)
                            {
                                if (slotItem.Slots[0].Type == BoatType.RowBoat)
                                {
                                    slotItem.Slots.Add(newBoat);
                                    slotItem.DaysToGo = 1;
                                    keepGoing = false;
                                    break;
                                }
                            }
                        }
                        if (keepGoing)
                        {
                            for (int j = 0; j < hamn.SlotBlocks.Count() - 1; j++)
                            {
                                if (hamn.SlotBlocks[j].Slots.Count() == 0)
                                {
                                    hamn.SlotBlocks[j].Slots.Add(newBoat);
                                    hamn.SlotBlocks[j].DaysToGo = 1;
                                    break;
                                }
                            }
                        }
                    }

                    //Handles other boattypes (NOT rowboats)
                    else
                    {
                        List<Slot> freeSlots = new List<Slot>();

                        bool isAssigned = false;

                        for (int j = 0; j < hamn.SlotBlocks.Count() - newBoat.SlotSize; j++)
                        {
                            freeSlots = hamn.SlotBlocks.GetRange(j, (int)newBoat.SlotSize);

                            //If we'll find an empty slot
                            if (freeSlots.All(s => s.Slots.Count == 0))
                            {
                                foreach (var item in freeSlots)
                                {
                                    item.Slots.Add(newBoat);
                                    item.DaysToGo = newBoat.DockingDays;
                                }
                                isAssigned = true;
                                break;
                            }
                        }

                        //If boat rejected
                        if (!isAssigned)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Boat {newBoat.ID} of {newBoat.Type} did NOT fit!");
                            Console.ForegroundColor = ConsoleColor.White;
                            boatsThatDidNotFit.Add(newBoat);
                        }

                    }

                    //If boat fits
                    if ((slotsTaken + newBoat.SlotSize) < hamn.Slots)
                    {

                        slotsTaken += newBoat.SlotSize;

                        string slotID = Guid.NewGuid().ToString();

                        newBoat.CurrentSlotID = slotID;

                        boats.Add(newBoat);
                    }

                }

                Print();
                WriteToFile();
                Console.WriteLine("Press any key to go to the next day.");

                //Pause (awaiting user click) to go to the next day
                Console.ReadLine();
                currentDay++;
            }
        }

        private static int CheckSavedDay()
        {
            if (!File.Exists(@"VariousInformation.txt")) return 1;
            using (StreamReader sr2 = File.OpenText(@"VariousInformation.txt"))
            {
                string s = sr2.ReadLine();
                if (s != null)
                {
                    currentDay = int.Parse(s);
                }
            }
            return currentDay;
        }

        private static void DeleteSavedBoats()
        {
            File.Delete("HarborTextFile.txt");
            File.Delete("VariousInformation.txt");
            File.Delete("BoatsThatDitNotFit.txt");
        }

        public static int KnotsToKmPerHour(int number)
        {

            double result = number * 1.85200;
            return (int)result;

        }
        public static void WriteToFile()
        {

            using StreamWriter sw = new StreamWriter(@"HarborTextFile.txt", false);

            foreach (var slot in hamn.SlotBlocks)
            {
                if (slot.Slots.Count() > 0)
                {
                    foreach (var boat in slot.Slots)
                    {
                        sw.WriteLine($"{slot.Number}\t{boat.ID}\t{boat.Weight}\t{boat.MaxSpeed}\t{boat.SlotSize}\t{boat.Type}\t{boat.DockingDays}\t" + boat.UniqueProperty.ToString());
                    }
                }
            }

            sw.Close();

            using StreamWriter sw2 = new StreamWriter(@"VariousInformation.txt", false);
            sw2.WriteLine(currentDay);
            sw2.Close();

            using StreamWriter sw3 = new StreamWriter(@"BoatsThatDidNotFit.txt", false);


            foreach (var boat in boatsThatDidNotFit)
            {
                sw3.WriteLine($"{boat.ID}\t{boat.Weight}\t{boat.MaxSpeed}\t{boat.SlotSize}\t{boat.Type}\t{boat.DockingDays}\t" + boat.UniqueProperty.ToString());
            }


            sw3.Close();

        }
        public static int ReadFile()
        {
            if (!File.Exists(@"HarborTextFile.txt")) return 0;
            var textFile = File.ReadAllText(@"HarborTextFile.txt");
            var rows = textFile.Split("\n");

            foreach (var row in rows)
            {
                var newBoat = new Boat();
                var splitLine = row.Split("\t"); //Array of the row

                //If empty
                if (splitLine[0] == "")
                {
                    continue;
                }
                newBoat.CurrentSlotID = splitLine[0];
                newBoat.ID = splitLine[1];
                newBoat.Weight = int.Parse(splitLine[2]);
                newBoat.MaxSpeed = int.Parse(splitLine[3]);
                newBoat.SlotSize = double.Parse(splitLine[4]);
                newBoat.Type = (BoatType)Enum.Parse(typeof(BoatType), splitLine[5]);
                newBoat.DockingDays = int.Parse(splitLine[6]);
                newBoat.UniqueProperty = int.Parse(splitLine[7]);
                boats.Add(newBoat);
                hamn.SlotBlocks[int.Parse(newBoat.CurrentSlotID)].Slots.Add(newBoat);
                hamn.SlotBlocks[int.Parse(newBoat.CurrentSlotID)].DaysToGo = newBoat.DockingDays;
            }

            using (StreamReader sr2 = File.OpenText(@"VariousInformation.txt"))
            {
                string s = sr2.ReadLine();
                if (s != null)
                {
                    currentDay = int.Parse(s);
                }

            }
            using (StreamReader sr2 = File.OpenText(@"BoatsThatDidNotFit.txt"))
            {
                string s2;

                while ((s2 = sr2.ReadLine()) != null)
                {
                    foreach (var item in s2)
                    {
                        var newBoat = new Boat();
                        var splitLine = s2.Split("\t");
                        newBoat.ID = splitLine[0];
                        newBoat.Weight = int.Parse(splitLine[1]);
                        newBoat.MaxSpeed = int.Parse(splitLine[2]);
                        newBoat.SlotSize = double.Parse(splitLine[3]);
                        newBoat.Type = (BoatType)Enum.Parse(typeof(BoatType), splitLine[4]);
                        newBoat.DockingDays = int.Parse(splitLine[5]);
                        newBoat.UniqueProperty = int.Parse(splitLine[6]);
                    }
                }
            }
            return rows.Length;
        }

        public static void Print()
        {

            List<Boat> totalBoats = new List<Boat>();
            Console.WriteLine($"\nSlot No:\tBoat ID:\tWeight:\t\tMaxSpeed:\tSlot Size:\tOther:");

            double count = 1;

            foreach (var item in hamn.SlotBlocks)
            {
                if (item.Slots.Count() > 0)
                {
                    foreach (var boat in item.Slots)
                    {

                        totalBoats.Add(boat);
                        string uniquePropertyString = "";
                        double size = item.SlotSize;
                        if (boat.Type == BoatType.RowBoat)
                        {
                            uniquePropertyString = "passengers: " + boat.UniqueProperty.ToString();
                        }
                        else if (boat.Type == BoatType.MotorBoat)
                        {
                            uniquePropertyString = "horse power: " + boat.UniqueProperty.ToString();
                        }
                        else if (boat.Type == BoatType.SailBoat)
                        {
                            uniquePropertyString = "boat length: " + boat.UniqueProperty.ToString() + " m";
                        }
                        else if (boat.Type == BoatType.CargoShip)
                        {
                            uniquePropertyString = "containers: " + boat.UniqueProperty.ToString();
                        }
                        double roundedSize = Math.Round(size);
                        if (boat.Type == BoatType.RowBoat)
                        {
                            Console.WriteLine($"{item.Number}\t\t{boat.ID}\t\t{boat.Weight}\t\t{boat.MaxSpeed}\t\t{boat.SlotSize}\t\t" + uniquePropertyString);
                            count += size + 0.5;

                        }
                        else
                        {
                            Console.WriteLine($"{item.Number}\t\t{boat.ID}\t\t{boat.Weight}\t\t{boat.MaxSpeed}\t\t{boat.SlotSize}\t\t" + uniquePropertyString);
                            count += size;
                        }
                    }
                }
            }

            int totalWeight2 = 0;
            int maxSpeed2 = 0;
            int boatsCount = 0;

            //HashSet reduces doublettes to keep correct count
            HashSet<string> ids = new HashSet<string>();
            foreach (var boat in totalBoats)
            {
                ids.Add(boat.ID);
            }

            foreach (var boatId in ids)
            {
                foreach (var boat in totalBoats)
                {
                    if (boatId.Equals(boat.ID))
                    {
                        totalWeight2 += boat.Weight;
                        maxSpeed2 += boat.MaxSpeed;
                        boatsCount += 1;
                        break;
                    }
                }
            }

            Console.WriteLine($"\nSUMMARY:");
            Console.WriteLine($"Number of boats of the day parked in the harbor:");
            int count11 = totalBoats.Where(x => x.Type == BoatType.RowBoat).ToList().Count();
            Console.WriteLine($"Rowboats count:\t\t\t{count11}");
            int count22 = totalBoats.Where(x => x.Type == BoatType.MotorBoat).ToList().Count();
            Console.WriteLine($"Motorboat count:\t\t{count22}");
            int count33 = totalBoats.Where(x => x.Type == BoatType.SailBoat).ToList().Count();
            Console.WriteLine($"Sailboat count:\t\t\t{count33 / 2}");
            int count44 = totalBoats.Where(x => x.Type == BoatType.CargoShip).ToList().Count();
            Console.WriteLine($"Cargo Ship count:\t\t{count44 / 4}");
            Console.WriteLine($"Slots available:\t\t{hamn.SlotBlocks.Where(s => s.Slots.Count() == 0).Count()}");

            //Add weight of the boat to total weight
            int averageMaxSpeed = 0;
            try
            {
                averageMaxSpeed = maxSpeed2 / boatsCount;
            }
            catch
            {

            }



            //Print average speed of all boats
            Console.WriteLine($"Max average speed:\t\t" + averageMaxSpeed + " km/h");

            //Print total weight in the harbor
            Console.WriteLine($"Total weight:\t\t\t" + totalWeight2 + " kg");

            Console.WriteLine($"Other:");
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine($"Total number of boats that did not fit: {boatsThatDidNotFit.Count()}");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("--------------------------------------------");

        }

        static Boat GetBoat(BoatType type)
        {

            Boat boat = new Boat();

            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string prefix = "";

            switch (type)
            {
                case BoatType.RowBoat:
                    prefix = "R-";
                    boat.Weight = random.Next(100, 300);
                    boat.MaxSpeed = random.Next(1, 3);
                    boat.DockingDays = 1;
                    boat.SlotSize = 0.5;
                    boat.UniqueProperty = random.Next(1, 6); //Passengers
                    break;
                case BoatType.MotorBoat:
                    prefix = "M-";
                    boat.Weight = random.Next(200, 3000);
                    boat.MaxSpeed = random.Next(1, 60);
                    boat.DockingDays = 3;
                    boat.SlotSize = 1.0;
                    boat.UniqueProperty = random.Next(10, 10000); //Horsepower
                    break;
                case BoatType.SailBoat:
                    prefix = "S-";
                    boat.Weight = random.Next(800, 6000);
                    boat.MaxSpeed = random.Next(1, 12);
                    boat.DockingDays = 4;
                    boat.SlotSize = 2.0;
                    boat.UniqueProperty = KnotsToKmPerHour(random.Next(1, 3));
                    break;
                case BoatType.CargoShip:
                    prefix = "L-";
                    boat.Weight = random.Next(3000, 20000);
                    boat.MaxSpeed = random.Next(1, 20);
                    boat.DockingDays = 6;
                    boat.SlotSize = 4.0;
                    boat.UniqueProperty = random.Next(0, 500); //Containers
                    break;
                default:
                    prefix = "D-";
                    break;
            }


            var list = Enumerable.Repeat(0, 3).Select(x => chars[random.Next(chars.Length)]);

            string result = string.Join("", list);

            boat.ID = prefix + result;
            boat.Type = type;

            return boat;

        }
    }
}

