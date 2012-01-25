using System;
using Data.Realm;
using Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsClient
{
    public class Program
    {
        static AgentAPI agent;

        /*
         * Nazwa agenta.
         */
        static string agentName;

        static void Main(string[] args)
        {
            try
            {
                // Tworzymy instancję naszego agenta
                agent = new AgentAPI(Listen);

                // Dane do świata
                String ip = "atlantyda.vm.wmi.amu.edu.pl";
                String groupname = "VGrupa1";
                String grouppass = "enkhey";
                String worldname = "VGrupa1";
                Random r = new Random();
                String imie = agentName = "Sasha" + r.Next(1,999);

                // Próbujemy się połączyć z serwerem
                worldParameters = agent.Connect(ip, 6008, groupname, grouppass, worldname, imie);

                // Inicjalizacja energii
                energy = worldParameters.initialEnergy;

                // Uruchamiamy agenta bez zbędnych komunikatów
                debugMode = false;

                Console.WriteLine("Wcisnij dowolny klawisz, aby uruchomic agenta.");
                Console.ReadKey();

                while (true)
                {
                    try
                    {
                        DoBestMovement();
                    }
                    catch (NonCriticalException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.ReadKey();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.ReadKey();
                        break;
                    }
                }

                // Kończymy
                agent.Disconnect();
            }
            catch
            {
                Console.WriteLine("Wystapily jakies bledy przy podlaczaniu do swiata! :(");
            }

            Console.ReadKey();
        }

        /*
         * Tryb pracy agenta. Dla wartości true wyświetla dużo danych na temat agenta przy każdym kroku.
         */
        public static bool debugMode = false;

        /*
         * Obecna energia agenta.
         */
        private static int energy;

        /*
         * Parametry świata.
         */
        public static WorldParameters worldParameters;

        /*
         * Zawiera współrzędne niewyczerpalnych źródeł energii,
         */
        private static List<Point> stableEnergyPoints = new List<Point>();

        /**
        * Zawiera punkty układu współrzędnych, na których był już agent.
        */
        private static List<Point> CoordinateSystem = new List<Point>() { new Point(0, 0) };

        /**
        * Kierunek agenta na układzie współrzędnych. Domyślny kierunek do północ, czyli początek osi OY.
        */
        private static Direction Dir = Direction.North;

        /*
        * Punkt układu współrzędnych, na którym znajduje się obecnie agent.
        */
        private static Point CurrentPoint = new Point(0, 0);


        /*
        * Odpowiada za punkt w układzie współrzędnych.
        */
        private struct Point
        {
            public int x;
            public int y;
            public int energy;
            public int visitedTimes;

            public Point(int x, int y, int energy = 0, int visitedTimes = 0)
            {
                this.x = x;
                this.y = y;
                this.energy = energy;
                this.visitedTimes = visitedTimes;
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
         * Nasłuchuje okolice agenta i odpowiada w razie usłyszanego głosu.
         */
        private static void Listen(String a, String s)
        {
            if (a == "superktos") Console.WriteLine("~Słysze własne słowa~");
            Console.WriteLine(a + " krzyczy " + s);

            Reply(s);
        }

        /*
        * Sprawdza czy odwiedziliśmy już ten punkt w układzie współrzędnych.
        */
        private static bool PointIsVisited(Point p)
        {
            return CoordinateSystem.Any(point => point.x == p.x && point.y == p.y);
        }

        /*
         * Wyświetla w konsoli punkty, które odwiedził już agent.
         */
        public static void DisplayVisitedPoints()
        {
            foreach (Point pole in CoordinateSystem)
            {
                Console.WriteLine("(" + pole.x + ", " + pole.y + ")");
            }
        }

        /**
        * Ustawia odpowiedni kierunek w układzie współrzędnych po wykonaniu rotacji.
        */
        private static void SetDirection(Direction RotationDir)
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
        private static void SetProperDirectionToPoint(Point p, string c)
        {
            if (c == "y")
            {
                if (p.y < 0 && Dir == Direction.East)
                {
                    RotateRight();
                }
                else if (p.y > 0 && Dir == Direction.East)
                {
                    RotateLeft();
                }
                else if (p.y < 0 && Dir == Direction.North)
                {
                    RotateLeft();
                    RotateLeft();
                }
                else if (p.y > 0 && Dir == Direction.West)
                {
                    RotateRight();
                }
                else if (p.y > 0 && Dir == Direction.South)
                {
                    RotateRight();
                    RotateRight();
                }
                else if (p.y < 0 && Dir == Direction.West)
                {
                    RotateLeft();
                }
            }
            else
            {
                if (p.x < 0 && Dir == Direction.East)
                {
                    RotateLeft();
                    RotateLeft();
                }
                else if (p.x > 0 && Dir == Direction.West)
                {
                    RotateRight();
                    RotateRight();
                }
                else if (p.x < 0 && Dir == Direction.North)
                {
                    RotateLeft();
                }
                else if (p.x > 0 && Dir == Direction.North)
                {
                    RotateRight();
                }
                else if (p.x < 0 && Dir == Direction.South)
                {
                    RotateRight();
                }
                else if (p.x > 0 && Dir == Direction.South)
                {
                    RotateLeft();
                }
            }
        }

        /*
         * Znajduje najbliższe stałe źródło energii, w założeniu, że taki punkt już znaleźliśmy. 
         */
        private static Point FindClosestStableEnergyPoint()
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
        private static double GetPointDistance(Point p)
        {
            return Math.Abs(CurrentPoint.y - p.y) + Math.Abs(CurrentPoint.y - p.y);
        }

        /*
         * Jak najprościej przenosi agenta do wskazanego punktu układu współrzędnych.
         */
        private static void GoToPoint(Point p)
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
        private static Point GetDestinationPoint()
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
         * Uwzględnia stałe źródła energii.
         */
        private static Point BestPointToMove(Dictionary<Point, int> list)
        {
            int maxVal = int.MinValue;
            Point theKey = default(Point);

            foreach (KeyValuePair<Point, int> pair in list)
            {
                if (pair.Value > maxVal)
                {
                    maxVal = pair.Value;
                    theKey = pair.Key;
                }
            }

            return theKey;
        }

        /*
         * Z podanej listy, zwraca najlepszy punkt, który nie jest stałym źródłem energii.
         */
        private static Point NoStableEnergyPoint(Dictionary<Point, int> list)
        {
            int maxVal = int.MinValue;
            int visitedTimes = int.MaxValue;
            Point theKey = default(Point);

            foreach (KeyValuePair<Point, int> pair in list)
            {
                if (pair.Value > maxVal && pair.Key.visitedTimes < visitedTimes && pair.Value != 900000000)
                {
                    maxVal = pair.Value;
                    visitedTimes = pair.Key.visitedTimes;
                    theKey = pair.Key;
                }
            }

            return theKey;
        }

        /*
         * Wykonuje najlepszy w rozumieniu agenta ruch na mapie.
         */
        public static void DoBestMovement()
        {
            if (debugMode)
            {
                Console.WriteLine("Znalezione zrodla energii: " + stableEnergyPoints.Count + ", obecna energia: " + energy + ".");
                foreach (Point pole in stableEnergyPoints)
                {
                    Console.WriteLine("(" + pole.x + ", " + pole.y + ")");
                }
                Console.WriteLine("Jestem w punkcie (" + CurrentPoint.x + ", " + CurrentPoint.y + ").");
                DisplayVisitedPoints();
            }

            /*
             * Jeżeli mamy w pamięci stałe źródło energii i obecna energia jest mniejsza niż połowa początkowej to idziemy do znanego źródła energii.
             */
            if (stableEnergyPoints.Count > 0 && energy < Convert.ToInt32((worldParameters.initialEnergy / 2)))
            {
                if (debugMode)
                {
                    Console.WriteLine("Ide do punktu ze stala energia.");
                }

                GoToPoint(FindClosestStableEnergyPoint());
                return;
            }
            /*
             * Jeżeli nie znamy żadnego stałe źródła energii to szukamy takiej energii na ślepo (zobacz opis metody).
             */
            else if (energy < Convert.ToInt32((worldParameters.initialEnergy / 2)))
            {
                if (debugMode)
                {
                    Console.WriteLine("Biegne na slepo znalezc stale zrodlo energii.");
                }

                GoFowardToEnergy();
                return;
            }
            /*
             * Agent obraca się wokół siebie i sprawdza, które z 4 pól, na które może przejść
             * jest najlepsze pod względem stracenia energii i zyskania nowej.
             * Po znalezieniu najlepszego pola, przechodzi na nie.
             * Jeżeli znalezione pole to punkt, w którym agent już był, to szukamy drugiego pola. 
             * Jeżeli wszystkie odwiedzono to wybieramy najlepsze z odwiedzonych.
             */
            else
            {
                Dictionary<Point, int> newFields = new Dictionary<Point, int>();
                Dictionary<Point, int> visitedFields = new Dictionary<Point, int>();

                OrientedField field;
                Point bPoint, cPoint;
                int cost;

                for (int i = 0; i < 4; ++i)
                {
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

                /*
                 * Jeżeli jest jakiś punkt, w którym jeszcze nie byliśmy to przechodzimy na nie.
                 * Jeżeli nie ma to wybieramy najlepsze z już odwiedzonych.
                 */
                if (newFields.Count > 0)
                {
                    bPoint = BestPointToMove(newFields);
                }
                else
                {
                    bPoint = NoStableEnergyPoint(visitedFields);

                    if (debugMode)
                    {
                        Console.WriteLine("Wokolo sa tylko odwiedzone juz pola.");
                    }
                }

                // Obracaj dopóki nie znajdziemy się w odpowiednim położeniu.
                while (GetDestinationPoint().x != bPoint.x || GetDestinationPoint().y != bPoint.y)
                {
                    RotateLeft();
                }

                if (debugMode)
                {
                    Console.WriteLine("Punkt wybrany jako najlepszy to (" + bPoint.x + ", " + bPoint.y + ").");

                    Console.ReadKey();
                }

                // Przejdź na najlepsze pole.
                StepForward(GetFirstSeenField());
            }

            if (debugMode)
            {
                Console.WriteLine("Energia: " + energy);
                Console.ReadKey();
            }
        }

        /*
         * Szaleńcza próba znalezienia stałego źródła energii, tzn. agent idzie prosto aż do napotkania przeszkody, następnie idzie w lewo.
         * Zakładamy tutaj, że na mapie nie ma dodatkowych przeszkód, poza jej granicami!
         */
        private static void GoFowardToEnergy()
        {
            OrientedField field;
            do
            {
                field = GetFirstSeenField();
                if (field == null)
                {
                    break;
                }
                StepForward(field);
            }
            while (field.energy != -1);

            RotateLeft();
            do
            {
                field = GetFirstSeenField();
                if (field == null)
                {
                    break;
                }
                StepForward(field);
            }
            while (field.energy != -1);

            RotateLeft();
            StepForward(GetFirstSeenField());

            RotateLeft();
            StepForward(GetFirstSeenField());
        }

        /*
         * Obraca agenta w lewo i zmienia położenie agenta na układzie współrzędnych.
         */
        public static void RotateLeft()
        {
            if (!agent.RotateLeft())
            {
                throw new Exception("Obrot nie powiodl sie - brak energii");
            }
            energy -= worldParameters.rotateCost;
            if (debugMode)
            {
                Console.WriteLine("pobiera energie za obrot");
            }
            SetDirection(Direction.West);

            if (debugMode)
            {
                System.Threading.Thread.Sleep(300);
            }
        }

        /*
         * Obraca agenta w prawo i zmienia położenie agenta na układzie współrzędnych.
         */
        public static void RotateRight()
        {
            if (!agent.RotateRight())
            {
                throw new Exception("Obrot nie powiodl sie - brak energii");
            }

            energy -= worldParameters.rotateCost;
            if (debugMode)
            {
                Console.WriteLine("pobiera energie za obrot");
            }
            SetDirection(Direction.East);

            if (debugMode)
            {
                System.Threading.Thread.Sleep(300);
            }
        }

        /*
         * Wykonuje krok do przodu i zaopatruje agenta w energię jeżeli takowa jest dostępna na polu.
         */
        public static void StepForward(OrientedField poleDocelowe)
        {
            if (!agent.StepForward())
            {
                throw new NonCriticalException("Wykonanie kroku nie powiodlo sie");
            }

            int koszt = GetMovementCost(poleDocelowe.height);

            // Ustawia nowy punkt układu współrzędnych, w którym znajduje się teraz agent.
            CurrentPoint = GetDestinationPoint();
            CurrentPoint.visitedTimes++;

            // Dodajemy do naszej mapki
            CoordinateSystem.Add(CurrentPoint);

            if (energy >= koszt)
            {
                energy -= Math.Abs(koszt);
                if (debugMode)
                {
                    Console.WriteLine("pobiera energie za krok (" + Math.Abs(koszt) + ")");
                }
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
                }

                if (!stableEnergyPoints.Exists(x => x.x == CurrentPoint.x && x.y == CurrentPoint.y))
                {
                    stableEnergyPoints.Add(CurrentPoint);
                }

                Console.WriteLine(stableEnergyPoints.Count);
            }
        }

        /**
         * Oblicza i zwraca koszt wykonania przejścia.
         */
        private static int GetMovementCost(int height)
        {
            return Convert.ToInt32(Math.Ceiling(Convert.ToDouble(worldParameters.moveCost * height) / 100));
        }

        /*
         * Zwraca dane pola (nie punktu), na które może przejść w tym momencie agent.
         * Zwraca null jeżeli dane pole jest przeszkodą, lub stoi na nim jakiś inny agent.
         */
        private static OrientedField GetFirstSeenField()
        {
            OrientedField[] widzianePola = agent.Look();
            foreach (OrientedField pole in widzianePola)
            {
                if (pole.agentId > 0)
                {
                    Say();
                }

                if (pole.x == 0 && pole.y == 1 && pole.obstacle == false && pole.agentId == -1)
                {
                    return pole;
                }
                else if (pole.x == 0 && pole.y == 1 && (pole.obstacle == true || pole.agentId > 0))
                {
                    return null;
                }
            }
            return null;
        }

        /*
         * Ładuje energię agenta jeżeli jest taka możliwość.
         */
        private static void Recharge()
        {
            int added = agent.Recharge();
            if (energy + added > worldParameters.initialEnergy)
            {
                energy = worldParameters.initialEnergy;
            }
            else
            {
                energy += added;
            }
            Console.WriteLine("Otrzymano " + added + " energii");
        }

        /*
         * Wysyła sygnał do otoczenia.
         */
        private static void Say()
        {
            agent.Speak("podaj twoje imie", 1);
        }

        /*
         * Udziela odpowiedzi na usłyszany głos.
         */
        private static void Reply(string s)
        {
            Dictionary<string, string> questiondb = new Dictionary<string, string>();
            questiondb.Add("stale zrodlo energii", "ode mnie w odleglosci " + GetPointDistance(FindClosestStableEnergyPoint()));
            questiondb.Add("twoje imie", "jestem " + agentName);

            string reply = "";

            foreach (KeyValuePair<string, string> pair in questiondb)
            {
                if (Regex.IsMatch(s, pair.Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    reply = pair.Value;
                }
            }

            if (reply.Length > 0)
            {
                agent.Speak(reply, 1);
            }
        }
    }
}