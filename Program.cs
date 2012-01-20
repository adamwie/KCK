using System;
using Data.Realm;
using Data;
using System.Collections.Generic;
using System.Linq;

namespace CsClient
{
    public class Program
    {
        static void Listen(String a, String s)
        {
            if (a == "superktos") Console.WriteLine("~Słysze własne słowa~");
            Console.WriteLine(a + " krzyczy " + s);
        }

        static void Main(string[] args)
        {
            try
            {
                // Tworzymy instancję naszego agenta
                Sasha agent1 = new Sasha(Listen);
                //Sasha agent2 = new Sasha(Listen);

                // Dane do świata
                String ip = "atlantyda.vm.wmi.amu.edu.pl";
                String groupname = "VGrupa1";
                String grouppass = "enkhey";
                String worldname = "VGrupa1";
                Random r = new Random();
                String imie = "Sasha" + r.Next();

                // Próbujemy się połączyć z serwerem
                agent1.worldParameters = agent1.Connect(ip, 6008, groupname, grouppass, worldname, imie);

                // Inicjalizacja energii
                agent1.SetEnergy(agent1.worldParameters.initialEnergy);

                while (true)
                {
                    try
                    {
                        agent1.DoBestMovement();
                    }
                    catch (NonCriticalException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.ReadKey();
                        break;
                    }
                }

                // Kończymy
                agent1.Disconnect();
            }
            catch
            {
                Console.WriteLine("Wystapily jakies bledy przy podlaczaniu do swiata! :(");
            }

            Console.ReadKey();
        }
    }

    class Sasha : AgentAPI
    {
        private int energy;
        public WorldParameters worldParameters;
        private List<Point> stableEnergyPoints = new List<Point>();

        /**
        * Odpowiada za punkt w układzie współrzędnych
        */
        private struct Point
        {
            public int x;
            public int y;
            public int energy;

            public Point(int x, int y, int energy = 0)
            {
                this.x = x;
                this.y = y;
                this.energy = energy;
            }
        }

        /**
        * Zawiera punkty układu współrzędnych, na których był już agent.
        */
        private List<Point> CoordinateSystem = new List<Point>();

        /**
        * Punkt układu współrzędnych, na którym znajduje się agent.
        */
        private Point CurrentPoint = new Point(0, 0);

        /**
        * Kierunek agenta w stosunku do kierunku początkowego, który wyznacza Północ.
        * Kierunki są powiązane z poruszaniem się po układzie współrzędnych.
        */
        enum Direction
        {
            North,
            South,
            West,
            East
        }

        /**
        * Kierunek agenta na układzie współrzędnych.
        */
        private Direction Dir = Direction.North;

        /**
        * Konstruktor klasy.
        */
        public Sasha(MessageHandler handler) : base(handler) { }

        /**
        * Ustawia energię agenta.
        */
        public void SetEnergy(int energy)
        {
            this.energy = energy;
        }

        /**
        * Sprawdza czy odwiedziliśmy już ten punkt.
        */
        private bool PointIsVisited(Point p)
        {
            return CoordinateSystem.Any(point => point.x == p.x && point.y == p.y);
        }

        public void DisplayVisitedPoints()
        {
            foreach (Point pole in CoordinateSystem)
            {
                Console.WriteLine("(" + pole.x + ", " + pole.y + ")");
            }
        }

        /**
        * Ustawia odpowiedni kierunek w układzie współrzędnych po wykonaniu rotacji.
        */
        private void SetDirection(Direction RotationDir)
        {
            switch (Dir)
            {
                case Direction.North:
                    if (RotationDir == Direction.West)
                    {
                        Dir = Direction.West;
                    }
                    else if (RotationDir == Direction.East)
                    {
                        Dir = Direction.East;
                    }
                    break;
                case Direction.South:
                    if (RotationDir == Direction.West)
                    {
                        Dir = Direction.East;
                    }
                    else if (RotationDir == Direction.East)
                    {
                        Dir = Direction.West;
                    }
                    break;
                case Direction.West:
                    if (RotationDir == Direction.West)
                    {
                        Dir = Direction.South;
                    }
                    else if (RotationDir == Direction.East)
                    {
                        Dir = Direction.North;
                    }
                    break;
                case Direction.East:
                    if (RotationDir == Direction.West)
                    {
                        Dir = Direction.North;
                    }
                    else if (RotationDir == Direction.East)
                    {
                        Dir = Direction.South;
                    }
                    break;
            }

           /* switch (Dir)
            {
                case Direction.North:
                    Console.WriteLine("Polnoc");
                    break;
                case Direction.West:
                    Console.WriteLine("Zachod");
                    break;
                case Direction.East:
                    Console.WriteLine("Wschod");
                    break;
                case Direction.South:
                    Console.WriteLine("Poludnie");
                    break;
            }*/
        }

        /**
        * Oblicza punkt układu współrzędnych w jakim się znajdziemy po wykonaniu kroku.
        */
        private Point GetDestinationPoint()
        {
            switch (Dir)
            {
                case Direction.North:
                    return new Point(CurrentPoint.x, CurrentPoint.y + 1);

                case Direction.South:
                    return new Point(CurrentPoint.x, CurrentPoint.y - 1);

                case Direction.West:
                    return new Point(CurrentPoint.x - 1, CurrentPoint.y);

                case Direction.East:
                    return new Point(CurrentPoint.x + 1, CurrentPoint.y);

                default:
                    return new Point(0, 0);
            }
        }

        private static Point BestPointToMove(Dictionary<Point, int> list)
        {
            int maxVal = int.MinValue;
            Point theKey = default(Point);

            foreach (KeyValuePair<Point, int> pair in list)
            {
                int curVal = Convert.ToInt32(pair.Value);
                if (curVal > maxVal)
                {
                    maxVal = curVal;
                    theKey = pair.Key;
                }
            }

            return theKey;
        }

        /**
         * Metoda sprawia, że agent obraca się wokół siebie i sprawdza, które z 4 pól, na które może przejść
         * jest najlepsze pod względem stracenia energii.
         * Po znalezieniu najlepszego pola, przechodzi na nie.
         * Jeżeli znalezione pole to punkt, w którym agent już był, to szukamy drugiego pola. 
         * Jeżeli wszystkie odwiedzono to wybieramy najlepsze z nich.
         */
        public void DoBestMovement()
        {
            Dictionary<Point, int> newFields = new Dictionary<Point, int>();
            Dictionary<Point, int> visitedFields = new Dictionary<Point, int>();

            OrientedField field;
            Point bPoint, cPoint;
            int cost;

            for (int i = 0; i < 4; ++i)
            {
                System.Threading.Thread.Sleep(500);

                field = GetFirstSeenField();

                if (field != null)
                {
                    cPoint = GetDestinationPoint();
                    cPoint.energy = field.energy;
                    cost = getMovementCost(field.height);

                    if (!PointIsVisited(cPoint))
                    {
                        newFields.Add(cPoint, (field.energy != -1) ? field.energy - cost : 900000000);
                    }
                    else
                    {
                        visitedFields.Add(cPoint, (field.energy != -1) ? field.energy - cost : 900000000);
                    }
                }
               /* else
                {
                    RotateLeft();
                    GoFowardToEnergy();
                    RotateLeft();
                    StepForward(GetFirstSeenField());
                    RotateLeft();
                    StepForward(GetFirstSeenField());
                    return;
                }*/

                if (i <= 3)
                {
                    RotateLeft();
                }
            }

            if (newFields.Count > 0)
            {
                bPoint = BestPointToMove(newFields);
            }
            else
            {
                bPoint = BestPointToMove(visitedFields);
                Console.WriteLine("Bylo to pole ale idziemy");
            }
            //Console.WriteLine("Obecne to (" + GetDestinationPoint().x + ", " + GetDestinationPoint().y + ")");
            // Obraca agenta dopóki nie znajdziemy się w odpowiednim położeniu.
            while (GetDestinationPoint().x != bPoint.x || GetDestinationPoint().y != bPoint.y)
            {
                
                RotateLeft();
                //Console.WriteLine("Obecne to (" + GetDestinationPoint().x + ", " + GetDestinationPoint().y + ")");
            }
            /*
            foreach (KeyValuePair<Point, int> pole in newFields)
            {
                Console.WriteLine("(" + pole.Key.x + ", " + pole.Key.y + ") - oplacalnosc: " + pole.Value + " / energia: " + pole.Key.energy);
            }
             */

            Console.WriteLine("Najlepsze to (" + bPoint.x + ", " + bPoint.y + ")");

            
            //Console.ReadKey();

            // Przejdź na najlepsze pole.
            StepForward(GetFirstSeenField());
        }

        private void GoFowardToEnergy()
        {
            OrientedField field;
            do
            {
                field = GetFirstSeenField();
                StepForward(field);
                System.Threading.Thread.Sleep(100);
            }
            while (field.energy != -1);
        }

        public void Listen(String a, String s)
        {
            if (a == "superktos") Console.WriteLine("~Słysze własne słowa~");
            Console.WriteLine(a + " krzyczy " + s);
        }

        new public void RotateLeft()
        {
            if (!base.RotateLeft())
            {
                throw new Exception("Obrot nie powiodl sie - brak energii");
            }
            energy -= worldParameters.rotateCost;
            SetDirection(Direction.West);
            //System.Threading.Thread.Sleep(500);
        }

        new public void RotateRight()
        {
            if (!base.RotateRight())
            {
                throw new Exception("Obrot nie powiodl sie - brak energii");
            }

            energy -= worldParameters.rotateCost;
            SetDirection(Direction.East);
        }

        public void StepForward(OrientedField poleDocelowe)
        {
            if (!base.StepForward())
            {
                throw new NonCriticalException("Wykonanie kroku nie powiodlo sie");
            }

            int koszt = getMovementCost(poleDocelowe.height);
            if (energy >= koszt)
            {
                energy -= koszt;
            }

            if (poleDocelowe.energy > 0)
            {
                Recharge();
            }
            else if (poleDocelowe.energy == -1)
            {
                while (energy < worldParameters.initialEnergy)
                {
                    Recharge();
                    System.Threading.Thread.Sleep(500);
                }

                if (!stableEnergyPoints.Contains(CurrentPoint))
                {
                    stableEnergyPoints.Add(CurrentPoint);
                }
            }

            // Ustawia nowy punkt układu współrzędnych, w którym znajduje się teraz agent.
            CurrentPoint = GetDestinationPoint();

            // Dodajemy do naszej mapki
            CoordinateSystem.Add(CurrentPoint);

            //Console.WriteLine("Roznica wysokosci:  " + poleDocelowe.height);
            //Console.WriteLine("Koszt:  " + koszt);
        }

        /**
         * Oblicza i zwraca koszt wykonania przejścia.
         */
        private int getMovementCost(int height)
        {
            return Convert.ToInt32(Math.Ceiling(Convert.ToDouble(worldParameters.moveCost * height) / 100));
        }

        public OrientedField GetFirstSeenField()
        {
            OrientedField[] widzianePola = base.Look();
            foreach (OrientedField pole in widzianePola)
            {
                if (pole.x == 0 && pole.y == 1 && pole.obstacle == false && pole.agentId == -1)
                {
                    return pole;
                }
            }
            return null;
        }

        public bool FirstStep()
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
        public void GoUp()
        {
            if (Dir == Direction.Up)
                StepForward();
            else
            {
                if (Dir == Direction.Left)
                    RotateRight();
                else if (Dir == Direction.Right)
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

        new private void Recharge()
        {
            int added = base.Recharge();
            energy += added;
            Console.WriteLine("Otrzymano " + added + " energii");
        }

        public void GoDown()
        {
        }


        public void init()
        {
            OrientedField[] widzianePola = base.Look();

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
    }
}