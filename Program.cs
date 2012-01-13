using System;
using Data.Realm;
using Data;

namespace CsClient
{
    /*
    public class Program
    {
        static AgentAPI agentTomek;
        static int energy;
        static WorldParameters cennikSwiata;

        static void Listen(String a, String s) {
            if(a == "superktos") Console.WriteLine("~Słysze własne słowa~");
             Console.WriteLine(a + " krzyczy " + s);
        }
        
        static void Main(string[] args)
        {
            while (true)
            {
                    agentTomek = new AgentAPI(Listen);

                String ip = Settings.serverIP;
                String groupname = Settings.groupname;
                String grouppass = Settings.grouppass;

                if (ip == null)
                {
                    Console.Write("Podaj IP serwera: ");
                    ip = Console.ReadLine();
                }
                if (groupname == null)
                {
                    Console.Write("Podaj nazwe druzyny: ");
                    groupname = Console.ReadLine();
                }
                if (grouppass == null)
                {
                    Console.Write("Podaj haslo: ");
                    grouppass = Console.ReadLine();
                }

                Console.Write("Podaj nazwe swiata: ");
                String worldname = Console.ReadLine();
                    
                Console.Write("Podaj imie: ");    
                String imie = Console.ReadLine();

           
            
                try
                {   
                    cennikSwiata = agentTomek.Connect(ip, 6008, groupname, grouppass, worldname, imie);
                    Console.WriteLine(cennikSwiata.initialEnergy + " - Maksymalna energia");
                    Console.WriteLine(cennikSwiata.maxRecharge + " - Maksymalne doładowanie");
                    Console.WriteLine(cennikSwiata.sightScope + " - Zasięg widzenia");
                    Console.WriteLine(cennikSwiata.hearScope + " - Zasięg słyszenia");
                    Console.WriteLine(cennikSwiata.moveCost + " - Koszt chodzenia");
                    Console.WriteLine(cennikSwiata.rotateCost + " - Koszt obrotu");
                    Console.WriteLine(cennikSwiata.speakCost + " - Koszt mówienia");

                    energy = cennikSwiata.initialEnergy;
                    KeyReader();
                    agentTomek.Disconnect();
                    Console.ReadKey();
                    break;
                }
                catch (NonCriticalException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.ReadKey();
                    break;
                }
            }
        }
        
        static void KeyReader() {
            bool loop = true;
            while(loop) {
                Console.WriteLine("Moja energia: " + energy);
                switch(Console.ReadKey().Key) {
                    case ConsoleKey.Spacebar: Look();
                        break;
                    case ConsoleKey.R: Recharge();
                        break;
                    case ConsoleKey.UpArrow: StepForward();
                        break;
                    case ConsoleKey.LeftArrow: RotateLeft();
                        break;
                    case ConsoleKey.RightArrow: RotateRight();
                        break;
                    case ConsoleKey.Enter: Speak();
                        break;
                    case ConsoleKey.Q: loop = false;
                        break;
                    case ConsoleKey.D: agentTomek.Disconnect();
                        break;
                    default: Console.Beep();
                        break;
                }
            }
        }

        private static void Recharge()
        {
            int added = agentTomek.Recharge();
            energy += added;
            Console.WriteLine("Otrzymano " + added + " energii");
        }

        private static void Speak()
        {
            if (!agentTomek.Speak(Console.ReadLine(), 1))
                Console.WriteLine("Mowienie nie powiodlo sie - brak energii");
            else
                energy -= cennikSwiata.speakCost;
        }

        private static void RotateLeft()
        {
            if (!agentTomek.RotateLeft())
                Console.WriteLine("Obrot nie powiodl sie - brak energii");
            else
                energy -= cennikSwiata.rotateCost;
        }

        private static void RotateRight()
        {
            if (!agentTomek.RotateRight())
                Console.WriteLine("Obrot nie powiodl sie - brak energii");
            else
                energy -= cennikSwiata.rotateCost;
        }

        private static void StepForward()
        {
            if (!agentTomek.StepForward())
                Console.WriteLine("Wykonanie kroku nie powiodlo sie");
            if (energy >= cennikSwiata.moveCost)
                energy -= cennikSwiata.moveCost;
        }

        private static void Look()
        {
            OrientedField[] pola = agentTomek.Look();
            foreach (OrientedField pole in pola)
            {
                Console.WriteLine("-----------------------------");
                Console.WriteLine("POLE " + pole.x + "," + pole.y);
                Console.WriteLine("Wysokosc: " + pole.height);
                if (pole.energy != 0)
                    Console.WriteLine("Energia: " + pole.energy);
                if (pole.obstacle)
                    Console.WriteLine("Przeszkoda");
                if (pole.agent != null)
                    Console.WriteLine("Agent " + pole.agent.fullName + " i jest obrocony na " + pole.agent.direction.ToString());
                Console.WriteLine("-----------------------------");
            }
        }
        
    }
    */
    public class AgentVGRupa1
    {
        static AgentAPI agent;
        static int energy;
        static WorldParameters worldParameters;

        static OrientedField aktualnePole;

        enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        static Direction direction = Direction.Up;

        static void Listen(String a, String s)
        {
            if (a == "superktos") Console.WriteLine("~Słysze własne słowa~");
            Console.WriteLine(a + " krzyczy " + s);
        }

        private static void RotateLeft()
        {
            if (!agent.RotateLeft())
                Console.WriteLine("Obrot nie powiodl sie - brak energii");
            else
                energy -= worldParameters.rotateCost;
        }

        private static void RotateRight()
        {
            if (!agent.RotateRight())
                Console.WriteLine("Obrot nie powiodl sie - brak energii");
            else
                energy -= worldParameters.rotateCost;
        }

        private static void StepForward(OrientedField poleDocelowe)
        {
            if (!agent.StepForward())
                Console.WriteLine("Wykonanie kroku nie powiodlo sie");
           /*
            if (energy >= worldParameters.moveCost)
                energy -= worldParameters.moveCost;
            */
            int koszt = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(worldParameters.moveCost * poleDocelowe.height) / 100));
            if (energy >= koszt)
                energy -= koszt;
            
            Console.WriteLine("Roznica wysokosci:  " + poleDocelowe.height);
            Console.WriteLine("Koszt:  " + koszt);
        }

        private static OrientedField GetFirstSeenField()
        {
            OrientedField[] widzianePola = agent.Look();
            foreach (OrientedField pole in widzianePola)
            {
                if (pole.x == 0 && pole.y == 1 && pole.obstacle == false)
                {
                    return pole;
                }
            }
            return null;
        }

        private static bool FirstStep()
        {
            OrientedField firstSeenField = GetFirstSeenField();

            Console.WriteLine("First step");
            Console.WriteLine("Energia: " + energy);
            for (int i = 0; i < 4; i++)
            {
                if (firstSeenField.obstacle == false)
                {
                    Console.WriteLine("Ide do przodu");
                    StepForward(firstSeenField);
                    break;
                }
                else
                {
                    RotateRight();
                    Console.WriteLine("Obracam sie w prawo");
                }
                if (i == 3)
                    return false;
            }
            Console.WriteLine("Energia: " + energy);
            return true;
        }

        /*
        private static void GoUp()
        {
            if (direction == Direction.Up)
                StepForward();
            else
            {
                if (direction == Direction.Left)
                    RotateRight();
                else if (direction == Direction.Right)
                    RotateLeft();
                else
                {
                    RotateLeft();
                    RotateLeft();
                }
                StepForward();
            }
        }
         * */

        private static void GoDown()
        {
        }


        private static void init()
        {
            OrientedField[] widzianePola = agent.Look();

            foreach (OrientedField pole in widzianePola)
            {
                if (pole.energy != 0)
                {
                    

                }
                else
                {
                }
            }
        }



        private static void Connect()
        {
            try
            {
                agent = new AgentAPI(Listen);

                String ip = "atlantyda.vm.wmi.amu.edu.pl";
                String groupname = "VGrupa1";
                String grouppass = "enkhey";

                String worldname = "VGrupa1";
                String imie = "Sasha";

                worldParameters = agent.Connect(ip, 6008, groupname, grouppass, worldname, imie);
                energy = worldParameters.initialEnergy;

            }
            catch
            {
                Console.WriteLine("Wystapily jakies bledy przy podlaczaniu do swiata! :(");
            }
        }


        private static void Work()
        {
            //TU logika agenta
            while (true)
            {
                try
                {
                    Console.WriteLine("Yo! Jestem w swiece!");
                    FirstStep();
                    Console.ReadKey();
                }
                catch (NonCriticalException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.ReadKey();
                    break;
                }
            }
        }

        private static void Disconnect()
        {
            agent.Disconnect();
            Console.ReadKey();
        }


        static void Main(string[] args)
        {
            Connect();
            Work();
            Disconnect();
        }
    }
}
