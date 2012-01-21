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

                Console.ReadKey();
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
        /*
         * Tryb pracy agenta. Dla wartości true wyświetla dużo danych na temat agenta przy każdym kroku.
         */
        public bool debugMode = false;

        /*
         * Obecna energia agenta.
         */
        private int energy;

        /*
         * Parametry świata.
         */
        public WorldParameters worldParameters;

        /*
         * Zawiera współrzędne niewyczerpalnych źródeł energii,
         */
        private List<Point> stableEnergyPoints = new List<Point>();

        /**
        * Zawiera punkty układu współrzędnych, na których był już agent.
        */
        private List<Point> CoordinateSystem = new List<Point>();

        /**
        * Kierunek agenta na układzie współrzędnych. Domyślny kierunek do północ, czyli początek osi OY.
        */
        private Direction Dir = Direction.North;

        /*
        * Punkt układu współrzędnych, na którym znajduje się obecnie agent.
        */
        private Point CurrentPoint = new Point(0, 0);


        /*
        * Odpowiada za punkt w układzie współrzędnych.
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

        /*
        * Konstruktor klasy.
        */
        public Sasha(MessageHandler handler) : base(handler) { }

        /*
        * Ustawia energię agenta.
        */
        public void SetEnergy(int energy)
        {
            this.energy = energy;
        }

        /*
        * Sprawdza czy odwiedziliśmy już ten punkt w układzie współrzędnych.
        */
        private bool PointIsVisited(Point p)
        {
            return CoordinateSystem.Any(point => point.x == p.x && point.y == p.y);
        }

        /*
         * Wyświetla w konsoli punkty, które odwiedził już agent.
         */
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
        }

        /*
         * Ustala poprawny kierunek agenta w odniesieniu do zadanego pola.
         * Tzn. obraca agenta tak, aby był od przodem do wskazanego jako argument punktu układu współrzędnych.
         * 
         * @param c string ustala według, której współrzędnej ma zostać ustawiony agent (x|y). 
         * Tzn. jeżeli chcemy przejść z punktu (0,0) na punkt (2,2) to jeżeli obecny kierunek to wschód (agent patrzy na x=2),
         * a chcemy iść w kierunku początka osi OY (do y=2) to agent jest obracany w lewo.
         */
        private void SetProperDirectionToPoint(Point p, string c)
        {
            if (c == "y")
            {
                if (p.y > 0 && Dir == Direction.East)
                {
                    RotateLeft();
                }
                else if (p.y < 0 && Dir == Direction.East)
                {
                    RotateRight();
                }
                else if (p.y > 0 && Dir == Direction.West)
                {
                    RotateRight();
                }
                else if (p.y < 0 && Dir == Direction.West)
                {
                    RotateLeft();
                }
            }
            else
            {
                if (p.x > 0 && Dir == Direction.North)
                {
                    RotateRight();
                }
                else if (p.x < 0 && Dir == Direction.North)
                {
                    RotateLeft();
                }
                else if (p.x > 0 && Dir == Direction.South)
                {
                    RotateLeft();
                }
                else if (p.x < 0 && Dir == Direction.South)
                {
                    RotateRight();
                }
            }
        }

        /*
         * Znajduje najbliższe stałe źródło energii, w założeniu, że taki punkt już znaleźliśmy. 
         */
        private Point FindClosestStableEnergyPoint()
        {
            if (stableEnergyPoints.Count == 0)
            {
                return CurrentPoint;
            }

            Point energyPoint = CurrentPoint;
            double distance = int.MaxValue;

            foreach (Point p in stableEnergyPoints)
            {
                if (distance > GetPointDistance(p))
                {
                    energyPoint = p;
                }
            }
            return energyPoint;
        }

        /*
         * Zwraca "umowną" odległość agenta od wskazanego punktu.
         */
        private double GetPointDistance(Point p)
        {
            return Math.Abs(CurrentPoint.y - p.y) + Math.Abs(CurrentPoint.y - p.y);
        }

        /*
         * Jak najprościej przenosi agenta do wskazanego punktu układu współrzędnych.
         */
        private void GoToPoint(Point p)
        {
            // Jeżeli jesteśmy w tym punkcie to nie robimy nic.
            if (CurrentPoint.x == p.x && CurrentPoint.y == p.y)
            {
                return;
            }

            OrientedField field;

            // Zapamiętujemy, z którego punktu wyszliśmy.
            Point startPoint = CurrentPoint;

            // Ustawiamy się tak, aby mieć przed sobą współrzędną y.
            SetProperDirectionToPoint(p, "y");

            // Idziemy prosto wzdłuż osi OY
            for (int i = 0; i < Math.Abs(startPoint.y - p.y); ++i)
            {
                field = GetFirstSeenField();
                if (field == null)
                {
                    return;
                }
                StepForward(field);
            }

            // Ustawiamy się tak, aby mieć przed sobą współrzędną x.
            SetProperDirectionToPoint(p, "x");

            // Idziemy prosto wzdłuż osi OX
            for (int i = 0; i < Math.Abs(startPoint.x - p.x); ++i)
            {
                field = GetFirstSeenField();
                if (field == null)
                {
                    return;
                }
                StepForward(field);
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

        /*
         * Ze wskazanej listy punktów, zwraca najlepsze (strata energii na przejście + pobór energii z pola).
         */
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

        /*
         * Metoda sprawia, że agent obraca się wokół siebie i sprawdza, które z 4 pól, na które może przejść
         * jest najlepsze pod względem stracenia energii i zyskania nowej.
         * Po znalezieniu najlepszego pola, przechodzi na nie.
         * Jeżeli znalezione pole to punkt, w którym agent już był, to szukamy drugiego pola. 
         * Jeżeli wszystkie odwiedzono to wybieramy najlepsze z odwiedzonych.
         */
        public void DoBestMovement()
        {
            if (debugMode)
            {
                Console.WriteLine("Znalezione zrodla energii: " + stableEnergyPoints.Count + ", obecna energia: " + energy + ".");
            }

            if (stableEnergyPoints.Count > 0 && energy < Convert.ToInt32((worldParameters.initialEnergy / 3)))
            {
                if (debugMode)
                {
                    Console.WriteLine("Ide do punktu ze stala energia.");
                }

                GoToPoint(FindClosestStableEnergyPoint());
                return;
            }
            else if (energy < Convert.ToInt32((worldParameters.initialEnergy / 3)))
            {
                if (debugMode)
                {
                    Console.WriteLine("Biegne na slepo znalezc stale zrodlo energii.");
                }

                GoFowardToEnergy();
                return;
            }
            else
            {
                Dictionary<Point, int> newFields = new Dictionary<Point, int>();
                Dictionary<Point, int> visitedFields = new Dictionary<Point, int>();

                OrientedField field;
                Point bPoint, cPoint;
                int cost;

                for (int i = 0; i < 4; ++i)
                {
                    System.Threading.Thread.Sleep(100);

                    field = GetFirstSeenField();

                    if (field != null)
                    {
                        cPoint = GetDestinationPoint();
                        cPoint.energy = field.energy;
                        cost = GetMovementCost(field.height);

                        if (!PointIsVisited(cPoint))
                        {
                            newFields.Add(cPoint, (field.energy != -1) ? field.energy - cost : 900000000);
                        }
                        else
                        {
                            visitedFields.Add(cPoint, (field.energy != -1) ? field.energy - cost : 900000000);
                        }
                    }

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

                    if (debugMode)
                    {
                        Console.WriteLine("Wokolo sa tylko odwiedzone juz pola.");
                    }
                }

                // Obracaj dopóki nie znajdziemy się w odpowiednim położeniu.
                while (GetDestinationPoint().x != bPoint.x || GetDestinationPoint().y != bPoint.y)
                {
                    RotateLeft();

                    if (debugMode)
                    {
                        Console.WriteLine("Obecnie widze punkt (" + GetDestinationPoint().x + ", " + GetDestinationPoint().y + ").");
                    }
                }

                if (debugMode)
                {
                    Console.WriteLine("Punkty w zasiegu wzroku:");
                    foreach (KeyValuePair<Point, int> pole in newFields)
                    {
                        Console.WriteLine("(" + pole.Key.x + ", " + pole.Key.y + ") - oplacalnosc przejscia: " + pole.Value);
                    }
                    Console.WriteLine("Punkt wybrany jako najlepszy to (" + bPoint.x + ", " + bPoint.y + ").");

                    Console.ReadKey();
                }

                // Przejdź na najlepsze pole.
                StepForward(GetFirstSeenField());
            }
        }

        /*
         * Szaleńcza próba znalezienia stałego źródła energii.
         */
        private void GoFowardToEnergy()
        {
            RotateLeft();

            OrientedField field;
            do
            {
                field = GetFirstSeenField();
                StepForward(field);
                System.Threading.Thread.Sleep(100);
            }
            while (field.energy != -1);

            RotateLeft();
            StepForward(GetFirstSeenField());

            RotateLeft();
            StepForward(GetFirstSeenField());
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

            if (debugMode)
            {
                System.Threading.Thread.Sleep(300);
            }
        }

        new public void RotateRight()
        {
            if (!base.RotateRight())
            {
                throw new Exception("Obrot nie powiodl sie - brak energii");
            }

            energy -= worldParameters.rotateCost;
            SetDirection(Direction.East);

            if (debugMode)
            {
                System.Threading.Thread.Sleep(300);
            }
        }

        public void StepForward(OrientedField poleDocelowe)
        {
            if (!base.StepForward())
            {
                throw new NonCriticalException("Wykonanie kroku nie powiodlo sie");
            }

            int koszt = GetMovementCost(poleDocelowe.height);

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
                    System.Threading.Thread.Sleep((debugMode) ? 400 : 200);
                }

                if (!stableEnergyPoints.Exists(x=>x.x == CurrentPoint.x && x.y == CurrentPoint.y))
                {
                    stableEnergyPoints.Add(CurrentPoint);
                }

                Console.WriteLine(stableEnergyPoints.Count);
            }

            // Ustawia nowy punkt układu współrzędnych, w którym znajduje się teraz agent.
            CurrentPoint = GetDestinationPoint();

            // Dodajemy do naszej mapki
            CoordinateSystem.Add(CurrentPoint);

            System.Threading.Thread.Sleep((debugMode) ? 400 : 200);
        }

        /**
         * Oblicza i zwraca koszt wykonania przejścia.
         */
        private int GetMovementCost(int height)
        {
            return Convert.ToInt32(Math.Ceiling(Convert.ToDouble(worldParameters.moveCost * height) / 100));
        }

        /*
         * Zwraca dane pola (nie punktu), na które może przejść w tym momencie agent.
         */
        private OrientedField GetFirstSeenField()
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

        new private void Recharge()
        {
            int added = base.Recharge();
            energy += added;
            Console.WriteLine("Otrzymano " + added + " energii");
        }
    }
}