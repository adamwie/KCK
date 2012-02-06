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
                String imie = agentName = "Sasha" + r.Next(1, 999);

                // Próbujemy się połączyć z serwerem
                worldParameters = agent.Connect(ip, 6008, groupname, grouppass, worldname, imie);

                // Inicjalizacja energii
                energy = worldParameters.initialEnergy;

                // Uruchamiamy agenta bez zbędnych komunikatów
                debugMode = true;

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
                    //System.Threading.Thread.Sleep(2000);
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

        public static int count = 0;

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
         * Pole na którym aktualnie znajduje się agent.
         */
        private static OrientedField CurrentField = new OrientedField();


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
            double newDistance, distance = int.MaxValue;

            foreach (Point p in stableEnergyPoints)
            {
                newDistance = GetPointDistance(p);

                if (distance > newDistance)
                {
                    distance = newDistance;
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
            return Math.Abs(CurrentPoint.y - p.y) + Math.Abs(CurrentPoint.x - p.x);
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
                    RotateLeft();
                    field = GetFirstSeenField();
                    if (field != null)
                    {
                        StepForward(field);
                    }
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
                    RotateLeft();
                    field = GetFirstSeenField();
                    if (field != null)
                    {
                        StepForward(field);
                    }
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
                Point p = FindClosestStableEnergyPoint();
                if (GetPointDistance(p) >= 10 && stableEnergyPoints.Count != 4)
                {
                    GoFowardToEnergy();
                }
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
            *Agent sprawdza czy w pobliżu znajduje sie pole z energia, jezeli tak idzie do niego(po pierwsze przezyc).
            *Jezeli nie znajdzie energi, stawia krok do przodu, chyba ze pole przednim bylo juz odwiedzone.
            *W takim wypadku obraca sie w lewo i powtarza cala czynnosc.
            */
            else
            {
                //Dictionary<Point, int> newFields = new Dictionary<Point, int>();
                //Dictionary<Point, int> visitedFields = new Dictionary<Point, int>();

                //OrientedField field;
                //Point bPoint, cPoint;
                //int cost;

                //for (int i = 0; i < 4; ++i)
                //{
                //    field = GetFirstSeenField();

                //    if (field != null)
                //    {
                //        cPoint = GetDestinationPoint();
                //        cPoint.energy = field.energy;
                //        cost = GetMovementCost(field.height);

                //        if (!PointIsVisited(cPoint))
                //        {
                //            newFields.Add(cPoint, (field.energy != -1) ? field.energy - cost : 900000000);
                //        }
                //        else
                //        {
                //            visitedFields.Add(cPoint, (field.energy != -1) ? field.energy - cost : 900000000);
                //        }
                //    }

                //    if (i <= 3)
                //    {
                //        RotateLeft();
                //    }
                //}

                /*
                * Jeżeli jest jakiś punkt, w którym jeszcze nie byliśmy to przechodzimy na nie.
                * Jeżeli nie ma to wybieramy najlepsze z już odwiedzonych.
                */
                
                OrientedField energyField = isEnergy();
                if (energy > Convert.ToInt32((worldParameters.initialEnergy * 0.6)))
                {
                    if (!(energyField == null))
                    {
                        Point p = GetPoint(energyField);
                        if (PointIsVisited(p))
                        {
                            energyField = null;
                        }
                    }
                }
                if (!(energyField == null))
                {
                    Console.WriteLine("Znalazlem energie!");
                    Point p = GetPoint(energyField);
                    Console.WriteLine(energyField.x);
                    Console.WriteLine(energyField.y);
                    Console.WriteLine(energyField.energy);
                    Console.WriteLine(energyField.height);
                    Console.WriteLine(energyField.obstacle);
                    Console.WriteLine(energyField.IsStepable());
                    //GoToPoint(p);
                    if (!GoToField(energyField))
                    {
                        RotateLeft();
                        return;
                    }
                    Recharge();
                    return;
                }
                else
                {
                    Console.WriteLine("Niestety brak energii!");
                    OrientedField pole = GetFirstSeenField();
                    if (pole == null)
                    {
                        RotateLeft();
                        Console.WriteLine("Nie mam pola do ruchu! Obracam sie w lewo i od nowa!");
                        return;
                    }
                    else
                    {
                        Point p = GetPoint(pole);
                        if (!PointIsVisited(p))
                        {
                            count = 0;
                            Console.WriteLine("Mam pole!");
                            StepForward(pole);
                        }
                        else
                        {
                            
                            if (count == 3)
                            {
                                StepForward(pole);
                                //count = 0;
                                count++;
                                return;
                            }
                            if (count == 7)
                            {
                                Random r = new Random();
                                StepForward(pole);
                                int b = r.Next(1, 8);
                                for (int i = 0; i <= b; i++)
                                {
                                    pole = GetFirstSeenField();
                                    if (pole != null)
                                    {
                                        StepForward(pole);
                                    }
                                    if (b % 3 == 0)
                                    {
                                        RotateLeft();
                                    }
                                    if (b % 2 == 0)
                                    {
                                        RotateRight();
                                    }
                                }
                                count = 0;
                                return;
                            }
                            pole = GetFirstNotVisitedField();
                            if (pole != null)
                            {
                                Console.WriteLine("Znalazlem nie odwiedzone pole!");
                                GoToField(pole);
                                count = 0;
                                return;
                            }
                            RotateLeft();
                            count++;
                            return;
                        }
                    }
                }
            }

              
        }

        /*
        * Szaleńcza próba znalezienia stałego źródła energii, tzn. agent idzie prosto aż do napotkania przeszkody, następnie idzie w lewo.
        * Zakładamy tutaj, że na mapie nie ma dodatkowych przeszkód, poza jej granicami!
        */
        private static void GoFowardToEnergy()
        {
            Console.WriteLine("MADNESSSS!");
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

            //RotateLeft();
            //StepForward(GetFirstSeenField());

            //RotateLeft();
            //StepForward(GetFirstSeenField());
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

                energy -= Math.Abs(koszt);
                if (debugMode)
                {
                    Console.WriteLine("pobiera energie za krok (" + Math.Abs(koszt) + ")");
                }
            

            if (poleDocelowe.energy > 0)
            {
                Recharge();
            }
            else if (poleDocelowe.energy == -1)
            {
                while (energy < worldParameters.initialEnergy - 20)
                {
                    Console.WriteLine(energy);
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
            //return Convert.ToInt32(Math.Ceiling(Convert.ToDouble(worldParameters.moveCost * height) / 100));
            return Convert.ToInt32(Math.Ceiling(Convert.ToDouble(worldParameters.moveCost * (1 + (height - CurrentField.height) / 100))));
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
                Point p = GetPoint(pole);
                Boolean isVisited = PointIsVisited(p); 
                
                if (pole.x == 0 && pole.y == 0)
                    CurrentField = pole;

                if (pole.agentId > 0)
                {
                    //Say();
                    Console.WriteLine("Agent");
                    continue;
                }

                if (pole.x == 0 && pole.y == 1 && pole.obstacle == false)
                {
                    return pole;
                }
                else if (pole.x == 0 && pole.y == 1 && (pole.obstacle == true || pole.agentId > 0))
                {
                    Console.WriteLine("przeszkoda!");
                    return null;
                }
            }
            Console.WriteLine("Nie ma nic!");
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

        
        /*
         * Metoda sprawdza, czy w polu widzenia agenta znajduje się jakieś pole z energią.
         * Jeżeli tak, zwraca to pole.
         * Jeżeli nie, zwraca null.
         */
        protected static OrientedField isEnergy()
        {
            OrientedField[] polaEnergii = agent.Look();
            OrientedField pole = new OrientedField();
            int energia = 0;

            foreach (OrientedField field in polaEnergii)
            {
                if (field.x == 0 && field.y == 0)
                {
                    CurrentField = field;
                    continue;
                }


                if (!field.IsStepable() || field.agentId > 0)
                {
                    continue;
                }

                if (field.energy == -1)
                {
                    Point p = GetPoint(field);
                    if (!PointIsVisited(p))
                    {
                        return field;
                    }
                }

                if (field.energy > energia)
                {
                    energia = pole.energy;
                    pole = field;
                }
            }
            if (energia > 0)
            {
                return pole;
            }
            return null;
        }

        /*
         * Metoda pobiera OreintedField, i zwraca Point.
         * Dzięki temu można nanieść pole na stworząną przez agenta mape.
         */ 
        private static Point GetPoint(OrientedField field)
        {
            switch (Dir)
            {
                case Direction.North:
                    return new Point(CurrentPoint.x + field.x, CurrentPoint.y + field.y);

                case Direction.South:
                    return new Point(CurrentPoint.x - field.x, CurrentPoint.y - field.y);

                case Direction.West:
                    return new Point(CurrentPoint.x - field.y, CurrentPoint.y + field.x);

                case Direction.East:
                    return new Point(CurrentPoint.x + field.y, CurrentPoint.y - field.x);

                default:
                    return new Point(0, 0);
            }
        }

        private static Boolean GoToField(OrientedField field)
        {
            for (int i = 0; i < field.y; i++)
            {
                OrientedField pole = GetFirstSeenField();
                if(pole == null)
                {
                    return false;
                }
                StepForward(pole);
            }
            
            if(field.x > 0)
            {
                RotateRight();
                for (int i = 0; i < field.x; i++)
                {
                    OrientedField pole = GetFirstSeenField();
                    if(pole == null)
                    {
                        return false;
                    }
                    StepForward(pole);
                }
            }
            else if (field.x < 0)
            {
                RotateLeft();
                for (int i = 0; i < (-field.x); i++)
                {
                    OrientedField pole = GetFirstSeenField();
                    if (pole == null)
                    {
                        return false;
                    }
                    StepForward(pole);
                }
            }
            else if (field.x == 0)
            {
                return true;
            }
            return true;
            
        }

        private static OrientedField GetFirstNotVisitedField()
        {
            OrientedField fieldy = new OrientedField();
            OrientedField field = new OrientedField();
            OrientedField[] widzianePola = agent.Look();

            field = null;
            fieldy = null;

            foreach (OrientedField pole in widzianePola)
            {
                if (pole.x == 0 && pole.y == 0)
                {
                    continue;
                }

                if (pole.obstacle || pole.IsStepable() || pole.agentId > 0)
                {
                    continue;
                }

                Point p = GetPoint(pole);

                if (!PointIsVisited(p) && field.y == 1 && field.x == 0)
                {
                    return pole;
                }

                if (!PointIsVisited(p) && field.y == 1)
                {
                    fieldy = pole;
                }

                if (!PointIsVisited(p))
                {
                    field = pole;
                }

            }
            if (fieldy != null)
            {
                return fieldy;
            }
            if (field != null)
            {
                return field;
            }
            return null;
        }
    }
}