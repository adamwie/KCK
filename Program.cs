using System;
using Data.Realm;
using Data;
using System.Collections.Generic;

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
                Sasha agent = new Sasha(Listen);

                // Dane do świata
                String ip = "atlantyda.vm.wmi.amu.edu.pl";
                String groupname = "VGrupa1";
                String grouppass = "enkhey";
                String worldname = "VGrupa1";
                String imie = "Sasha";

                // Próbujemy się połączyć z serwerem
                agent.worldParameters = agent.Connect(ip, 6008, groupname, grouppass, worldname, imie);

                // Inicjalizacja energii
                agent.SetEnergy(agent.worldParameters.initialEnergy);

                // Działanie agenta
                agent.Launch();

                // Kończymy
                agent.Disconnect();
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
         * Logika działania agenta.
         */
        public void Launch()
        {
            while (true)
            {
                try
                {
                    DoBestMovement();
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
            return CoordinateSystem.Contains(p);
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

        /**
         * Metoda sprawia, że agent obraca się wokół siebie i sprawdza, które z 4 pól, na które może przejść
         * jest najlepsze pod względem stracenia energii.
         * Po znalezieniu najlepszego pola, przechodzi na nie.
         * Jeżeli znalezione pole to punkt, w którym agent już był, to szukamy drugiego pola. 
         * Jeżeli wszystkie odwiedzono to wybieramy najlepsze z nich.
         */
        private void DoBestMovement()
        {
            // Najlepszy punkt na układzie współrzędnych.
            Point bestPoint = new Point(0, 0);

            // Dane najlepszego pola wg. Atlantydy
            OrientedField bestField = new OrientedField();
            bestField.height = 123456789;
            bestField.energy = 0;

            // Pole widziane w danym momencie.
            OrientedField field = new OrientedField();
            OrientedField[] fields = new OrientedField[4];
            field.energy = 0;
            field.height = -12345679;
            
            fields[0] = field = GetFirstSeenField();
            if (field != null)
            {
                bestPoint = GetDestinationPoint();
                bestField = field;
            }

            RotateLeft();
            System.Threading.Thread.Sleep(500);
            fields[1] = field = GetFirstSeenField();
            if (CompareFields(bestField, field) || PointIsVisited(bestPoint))
            {
                bestPoint = GetDestinationPoint();
                bestField = field;
            }

            RotateLeft();
            System.Threading.Thread.Sleep(500);
            fields[2] = field = GetFirstSeenField();
            if (CompareFields(bestField, field) || PointIsVisited(bestPoint))
            {
                bestPoint = GetDestinationPoint();
                bestField = field;
            }

            RotateLeft();
            System.Threading.Thread.Sleep(500);
            fields[3] = field = GetFirstSeenField();
            if (CompareFields(bestField, field) || PointIsVisited(bestPoint))
            {
                bestPoint = GetDestinationPoint();
                bestField = field;
            }

            if (bestField.height == 123456789)
            {
                foreach (OrientedField f in fields)
                {
                    if (f != null)
                    {
                        bestField = f;
                        break;
                    }
                }
            }

            // Obraca agenta dopóki nie znajdziemy się w odpowiednim położeniu.
            while (GetDestinationPoint().x != bestPoint.x && GetDestinationPoint().y != bestPoint.y)
            {
                RotateLeft();
                System.Threading.Thread.Sleep(500);
            }

            // Przejdź na najlepsze pole.
            StepForward(bestField);
        }

        /**
         * Zwraca true jeżeli warto zmienić stare pole na nowe pole.
         */
        public bool CompareFields(OrientedField oldfield, OrientedField newfield)
        {
            if (oldfield == null)
            {
                return true;
            }
            else if (newfield == null)
            {
                return false;
            }

            return ((getMovementCost(newfield.height) + newfield.energy) > (getMovementCost(oldfield.height) + oldfield.energy) || oldfield.energy < 0 || newfield.energy < 0);
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
                Console.WriteLine("Obrot nie powiodl sie - brak energii");
                return;
            }
            energy -= worldParameters.rotateCost;
            SetDirection(Direction.West);
        }

        new public void RotateRight()
        {
            if (!base.RotateRight())
            {
                Console.WriteLine("Obrot nie powiodl sie - brak energii");
                return;
            }

            energy -= worldParameters.rotateCost;
            SetDirection(Direction.East);
        }

        public void StepForward(OrientedField poleDocelowe)
        {
            if (!base.StepForward())
            {
                Console.WriteLine("Wykonanie kroku nie powiodlo sie");
                return;
            }

            int koszt = getMovementCost(poleDocelowe.height);
            if (energy >= koszt)
            {
                energy -= koszt;
            }

            if (poleDocelowe.energy != 0)
            {
                Recharge();
            }

            // Ustawia nowy punkt układu współrzędnych, w którym znajduje się teraz agent.
            CurrentPoint = GetDestinationPoint();

            // Dodajemy do naszej mapki
            CoordinateSystem.Add(CurrentPoint);

            Console.WriteLine("Roznica wysokosci:  " + poleDocelowe.height);
            Console.WriteLine("Koszt:  " + koszt);
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
                if (pole.x == 0 && pole.y == 1 && pole.obstacle == false)
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