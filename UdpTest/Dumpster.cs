using System;
using System.Collections.Generic;
using System.Text;

namespace UdpTest
{
    class Dumpster
    {
        private void DoStuff2()
        {

            List<ulong> finds, allFinds = new List<ulong>();

            ulong maxAttempts = 1;

            for (ulong i = 1; i < ulong.MaxValue; i++)
            {
                ulong n = i;
                ulong j = 0;

                finds = new List<ulong>();




                while (!allFinds.Contains(n))
                {
                    allFinds.Add(n);

                    if (finds.Contains(n))
                    {
                        Console.WriteLine($"FOUND: {n}");
                        return;
                    }
                    else
                    {
                        finds.Add(n);
                    }

                    if (n % 2 == 0)
                        n /= 2;
                    else
                    {
                        n = n * 3 + 1;
                        if (n * 3 + 1 < n)
                        {
                            Console.WriteLine("Error!");
                            return;
                        }
                    }

                    j++;
                    if (j >= maxAttempts && j % maxAttempts == 0)
                    {
                        maxAttempts++;
                        Console.WriteLine($"{i} attempts: {j}");
                    }

                }
            }


            return;
        }


        private void DoStuff()
        {
            int maxatt = 0;
            List<int> visited = new List<int>();
            for (int i = 3; i < int.MaxValue; i += 2)
            {
                Colloq(i);
                //var a = AStar(1, i);
                //if (a == null)
                //{
                //    Console.WriteLine($"Failure! {i}");
                //}
                //else
                //{
                //    StringBuilder stringBuilder = new StringBuilder();
                //    foreach (var item in a)
                //    {
                //        stringBuilder.Append(item);
                //        stringBuilder.Append(" ");
                //    }
                //    Console.WriteLine(stringBuilder);
                //}


                //int a = 1, b, att = 0;
                //visited = new List<int>();
                //int aim = i;
                //while (aim % 2 == 0)
                //    aim /= 2;

                //if (aim == 7)
                //    Console.WriteLine();
                //while (a != aim)
                //{


                //    att++;
                //    if (((a - 1) / 3) % 2 == 0)
                //    {
                //        a *= 2;
                //    }
                //    else if ((a - 1) % 3 == 0 && a % 2 == 0 && ((a - 1) / 3 == aim || (a - 1) / 3 > aim))
                //    {
                //        a = (a - 1) / 3;
                //    }
                //    else
                //    {
                //        a *= 2;
                //    }



                //    if (att > maxatt)
                //    {
                //        maxatt = att;
                //        if (maxatt % 100 == 0)
                //            Console.WriteLine($"{i} - {maxatt} tries. A = {a}");
                //    }
                //}
            }
            Console.WriteLine("yep");



            return;
        }


        public static List<int> AStar(int start, int goal)
        {
            List<int> openSet = new List<int>() { start };

            Dictionary<int, int> cameFrom = new Dictionary<int, int>();

            Dictionary<int, int> gScore = new Dictionary<int, int>();
            gScore.Add(start, 0);

            Dictionary<int, int> fScore = new Dictionary<int, int>();
            fScore.Add(start, Heuristic(start));

            while (openSet.Count > 0)
            {

                int current = -1;
                foreach (var item in openSet)
                {
                    if (current == -1 || fScore[item] < fScore[current])
                    {
                        current = item;
                    }
                }

                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                List<int> neighbors = new List<int>();
                if ((current - 1) / 3 != goal && current * 2 <= int.MaxValue && current * 2 > 0)
                    neighbors.Add(current * 2);
                if ((current - 1) % 3 == 0 && current % 2 == 0 && (current - 1) > 0 && current * 2 != goal)
                {
                    neighbors.Add((current - 1) / 3);
                }

                foreach (var neighbor in neighbors)
                {
                    int tentative_gScore = gScore[current] + 1;

                    if (!gScore.ContainsKey(neighbor) || tentative_gScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative_gScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor);
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null;

            List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
            {
                List<int> TotalPath = new List<int>() { current };

                while (cameFrom.ContainsKey(current))
                {
                    current = cameFrom[current];
                    TotalPath.Add(current);
                }
                //TotalPath.Reverse();

                return TotalPath;
            }

            int Heuristic(int node)
            {
                int cost = 0, tempGoal = goal;

                while (tempGoal != node)
                {
                    if (tempGoal % 2 == 0)
                        tempGoal /= 2;
                    else
                        tempGoal = tempGoal * 3 + 1;

                    cost++;

                    if (tempGoal == 0 || tempGoal == 1)
                        return cost + 9999;
                }

                /*while (node < goal)
                {
                    node *= 2;
                    cost++;
                }
                while (node > goal / 2)
                {
                    node -= 1;
                    cost += 1;
                }*/

                return cost;
            }
        }

        public static int Colloq(int n)
        {
            int max = 0;
            StringBuilder stringBuilder = new StringBuilder(n.ToString());
            List<int> numbers = new List<int>();
            while (n != 1)
            {
                if (n % 2 == 0)
                {
                    n /= 2;
                }
                else
                {
                    n = n * 5 + 1;
                }
                if (n > max)
                    max = n;

                if (numbers.Contains(n))
                {
                    Console.WriteLine($"error: {n} found!");
                    return 0;
                }
                else
                {
                    numbers.Add(n);
                }

                //stringBuilder.Append(' ');
                //stringBuilder.Append(n);
            }

            Console.WriteLine($"Max: {max} Path: \n{stringBuilder}");

            return 0;
        }

    }
}
