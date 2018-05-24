using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using static System.Console;
using static System.Math;

namespace GroupingMehods
{
    #region StructsAndEnums
    public struct XY
    {
        public double x;
        public double y;
    }
    public enum Init
    {
        Forgy,
        RandomPartition

    }
    public enum Option
    {
        KMeans,
        NeuralGas,
        Kohonen
    }
    public enum Neighborhood
    {
        close,
        far
    }
    public struct CentreDistancePair
    {
        public Centre centre;
        public double distance;
    }
    #endregion
    public class Program
    {
        #region GeneralSettings
        public static Option option = Option.KMeans;
        public static int ExecutionsCount = 5;
        #endregion
        #region AppliesToKmeans
        public static Init init = Init.RandomPartition;
        #endregion
        #region AppliesToKMeansAndGas
        public static int centresCount = 30;
        #endregion
        #region AppliesToKohonenAndGas
        public static double learningRate = 0.5;
        public static double currentLearningRate = learningRate;
        public static Neighborhood neighborhood = Neighborhood.far;
        public static int SamplesCount = 1000;
        #endregion
        #region AppliesToKohonen
        public static int rows = 5;
        public static int columns = 6;
        #endregion

        #region Main
        static void Main(string[] args)
        {
            List<Point> points = LoadTrainingSamplesFromFile();
            WriteLine("Computing begins ");
            for (int iterator = 0; iterator < ExecutionsCount; iterator++)
            {
                switch (option)
                {
                    case Option.KMeans:
                        {
                            ExecuteKMeansOption(points, iterator);
                            break;
                        }
                    case Option.NeuralGas:
                        {
                            ExecuteNeuralGasOption(points, iterator);
                            break;
                        }
                    case Option.Kohonen:
                        {
                            ExecuteKohonenOption(points, iterator);
                            break;
                        }
                }
                WriteLine($"Execution {iterator}. done ");
            }
        }
        #endregion

        #region Methods
        #region Executions
        public static void ExecuteKMeansOption(List<Point> points, int iterator)
        {
            List<double> diffrences = new List<double>();
            Centre[] centres = InitalizeCentres(points);
            bool isChanged;
            do
            {
                ComputeDistancesAndAllocatePoints(centres, points);
                diffrences.Add(ComputeEpochError(centres, points));
                isChanged = CheckIfChangesOccured(centres);
                UpdateCentresCordinates(centres);
                ResetAllocations(centres);
            } while (isChanged);
            ComputeDistancesAndAllocatePoints(centres, points);
            diffrences.Add(ComputeEpochError(centres, points));
            SaveDiffrencesToFile(centres, diffrences, iterator);
            SaveCentresCordinates(centres, iterator);
        }


        public static void ExecuteNeuralGasOption(List<Point> points, int iterator)
        {
            List<double> diffrence = new List<double>();
            Centre[] centres = InitalizeCentres(points);
            int howManyTens = 1;
            List<int> numbers = GenerateNumbers(points);
            for (int i = 0; i < SamplesCount; i++)
            {
                int randomIndex = numbers[new Random().Next(numbers.Count)];
                numbers.Remove(randomIndex);
                currentLearningRate = learningRate * (SamplesCount - i) / (SamplesCount);
                List<CentreDistancePair> centreDistancePairs = CreateAndSortDistanceCentrePairs(centres, points, randomIndex);
                CentreDistancePair tiredWinner;


                if (i % 5 == 0)
                {
                    InsertIterationErrorIntoList(centres, points, diffrence);
                    if (i == howManyTens * SamplesCount / 10)
                    {
                        WriteLine((howManyTens * 10) + "%");
                        howManyTens++;
                    }
                }


                if (centreDistancePairs[0].centre.activity < 0.5)
                {
                    tiredWinner = centreDistancePairs[0];
                    centreDistancePairs.RemoveAt(0);
                    tiredWinner.centre.activity += 1.0 / centres.Length;
                }
                ComputeNewPositionsAndIncreaseActivityNG(centreDistancePairs, centres, points, randomIndex);
            }
            if (SamplesCount % 5 == 0)
            {
                InsertIterationErrorIntoList(centres, points, diffrence);

            }
            SaveCentresCordinates(centres, iterator);
            SaveDiffrencesToFile(centres, diffrence, iterator);
        }




        public static void ExecuteKohonenOption(List<Point> points, int iterator)
        {
            List<double> diffrence = new List<double>();
            Centre[,] centresMatrix = InitalizeCentresMatrix(points);
            Centre[] centresVector = new Centre[rows * columns];
            int howManyTens = 1;
            for (int k = 0; k < rows; k++)
            {
                for (int l = 0; l < columns; l++)
                {
                    centresVector[(k * columns) + l] = centresMatrix[k, l];
                }
            }

            List<int> numbers = GenerateNumbers(points);
            for (int j = 0; j < SamplesCount; j++)
            {
                int randomIndex = numbers[new Random().Next(numbers.Count)];
                numbers.Remove(randomIndex);
                if (j % 5 == 0)
                {
                    InsertIterationErrorIntoList(centresVector, points, diffrence);
                    if (j == howManyTens * SamplesCount / 10)
                    {
                        WriteLine((howManyTens * 10) + "%");
                        howManyTens++;
                    }
                }
                List<CentreDistancePair> centreDistancePairs = CreateAndSortDistanceCentrePairs(centresVector, points, randomIndex);
                currentLearningRate = learningRate * (SamplesCount - j) / SamplesCount;
                Centre winner = centreDistancePairs[0].centre;
                Centre tired = null;
                if (winner.activity < 0.5)
                {
                    tired = winner;
                    winner = centreDistancePairs[1].centre;
                }
                int winI = 0;
                int winJ = 0;
                FindWinnerCordinates(ref winI, ref winJ, centresMatrix, winner);
                UpdateCentresActivitiesAndCordinatesKohonen(winI, winJ, centresMatrix, points, winner, tired, randomIndex);
            }
            if (SamplesCount % 5 == 0)
            {
                InsertIterationErrorIntoList(centresVector, points, diffrence);
            }
            SaveCentresCordinates(centresVector, iterator);
            SaveDiffrencesToFile(centresVector, diffrence, iterator);
        }
        #endregion

        public static List<Point> LoadTrainingSamplesFromFile()
        {
            List<Point> points = new List<Point>();
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            StreamReader streamReader = new StreamReader("../../resources/attract.txt");
            string container = streamReader.ReadToEnd();
            container = container.Replace(",", " ");
            container = container.Replace("\n", " ");
            container = container.Replace(Environment.NewLine, " ");
            container = container.Replace(".", ",");
            string[] samples;
            samples = container.Split(' ');

            for (int i = 0; i < samples.Length - 2; i++)
            {
                if (i % 2 == 0)
                {
                    x.Add(Convert.ToDouble(samples[i]));
                }
                if (i % 2 == 1)
                {
                    y.Add(Convert.ToDouble(samples[i]));
                }
            }
            for (int i = 0; i < x.Count; i++)
            {
                points.Add(new Point(x[i], y[i]));
            }
            return points;
        }

        public static void SaveCentresCordinates(Centre[] centres, int iterator)
        {
            List<XY> centresCordinatesToSerialize = new List<XY>();
            for (int i = 0; i < centres.Length; i++)
            {
                centresCordinatesToSerialize.Add(new XY()
                {
                    x = centres[i].x,
                    y = centres[i].y
                }
                                                );
            }
            String fileName = "";
            if (option == Option.KMeans)
            {
                fileName = option.ToString() + "_" + init.ToString() + "_przebieg" + iterator + "_Centres.xml";
            }
            else
            {
                fileName = option.ToString() + "_" + "NeuralGasOrKohonenInit" + "_przebieg" + iterator + "_" + neighborhood.ToString() + "_Centres.xml";
            }
            using (StreamWriter streamWriter = new StreamWriter(fileName, false))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<XY>));
                xmlSerializer.Serialize(streamWriter, centresCordinatesToSerialize);
            }
        }


        public static void SaveDiffrencesToFile(Centre[] centres, List<double> diffrence, int iterator)
        {
            String fileName = "";
            if (option == Option.KMeans)
            {
                fileName = option.ToString() + "_" + init.ToString() + "_przebieg" + iterator + "_Diffrences.xml";
            }
            else
            {
                fileName = option.ToString() + "_" + "NeuralGasOrKohonenInit" + "_przebieg" + iterator + "_" + neighborhood.ToString() + "_Diffrences.xml";
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<double>));
                xmlSerializer.Serialize(sw, diffrence);
            }
        }


        public static Centre[] InitalizeCentres(List<Point> points)
        {
            Random random = new Random();
            Centre[] centres = new Centre[centresCount];
            List<int> numbers = GenerateNumbers(points);
            for (int i = 0; i < centres.Length; i++)
            {
                centres[i] = new Centre();
            }
            if (option == Option.KMeans)
            {
                if (init == Init.Forgy)
                {
                    for (int i = 0; i < centres.Length; i++)
                    {
                        int randomIndex = numbers[new Random().Next(numbers.Count)];
                        numbers.Remove(randomIndex);
                        centres[i].x = points[randomIndex].x;
                        centres[i].y = points[randomIndex].y;
                    }
                }
                if (init == Init.RandomPartition)
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        centres[random.Next(centresCount)].points.Add(points[i]);
                    }
                    for (int i = 0; i < centres.Length; i++)
                    {
                        centres[i].SetNewCordinates();
                        centres[i].points.Clear();
                        centres[i].points.TrimExcess();
                    }
                }
            }
            else if (option == Option.NeuralGas)
            {
                for (int i = 0; i < centres.Length; i++)
                {
                    centres[i] = new Centre()
                    {
                        x = (random.NextDouble() - 0.5) * 20,
                        y = (random.NextDouble() - 0.5) * 20
                    };
                }

            }
            return centres;
        }
        public static Centre[,] InitalizeCentresMatrix(List<Point> points)
        {
            Random random = new Random();
            Centre[,] centres = new Centre[rows, columns];
            List<int> numbers = GenerateNumbers(points);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    centres[i, j] = new Centre();
                }
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    centres[i, j] = new Centre()
                    {
                        x = (random.NextDouble() - 0.5) * 20,
                        y = (random.NextDouble() - 0.5) * 20
                    };
                }
            }

            return centres;
        }



        public static double ComputeEpochError(Centre[] centres, List<Point> points)
        {
            double epochDiffrence = 0;
            for (int i = 0; i < centres.Length; i++)
            {
                for (int j = 0; j < centres[i].points.Count; j++)
                {
                    epochDiffrence += (Pow(centres[i].points[j].x - centres[i].x, 2) + Pow(centres[i].points[j].y - centres[i].y, 2));
                }
            }
            epochDiffrence /= points.Count;
            return epochDiffrence;
        }



        public static void ComputeDistancesAndAllocatePoints(Centre[] centres, List<Point> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                double minValue = 30;
                int minIndex = 0;
                for (int j = 0; j < centres.Length; j++)
                {
                    double distance = Sqrt(Pow(points[i].x - centres[j].x, 2) + Pow(points[i].y - centres[j].y, 2));
                    if (distance < minValue)
                    {
                        minValue = distance;
                        minIndex = j;
                    }
                }
                centres[minIndex].points.Add(points[i]);
            }
        }



        public static bool CheckIfChangesOccured(Centre[] centres)
        {
            bool diffrence = false;
            for (int i = 0; i < centres.Length; i++)
            {
                if (!centres[i].IsSame)
                {
                    diffrence = true;
                }
            }
            return diffrence;
        }



        public static void UpdateCentresCordinates(Centre[] centres)
        {
            for (int i = 0; i < centres.Length; i++)
            {
                centres[i].SetNewCordinates();
            }
        }



        public static void ComputeNewPositionsAndIncreaseActivityNG(List<CentreDistancePair> centreDistancePairs, Centre[] centres, List<Point> points, int randomIndex)
        {
            for (int i = 0; i < centreDistancePairs.Count; i++)
            {
                if (i == 0)
                {
                    centreDistancePairs[i].centre.activity -= 0.5;
                }
                else
                {
                    centreDistancePairs[i].centre.activity += 1.0 / centres.Length;
                }
                if ((neighborhood == Neighborhood.close && i <= 4))
                {
                    double deltaX = 0;
                    double deltaY = 0;
                    deltaX = points[randomIndex].x - centreDistancePairs[i].centre.x;
                    deltaY = points[randomIndex].y - centreDistancePairs[i].centre.y;
                    centreDistancePairs[i].centre.x += currentLearningRate * Exp(-i) * deltaX;
                    centreDistancePairs[i].centre.y += currentLearningRate * Exp(-i) * deltaY;
                }
                if ((neighborhood == Neighborhood.far && i <= 8))
                {
                    double deltaX = 0;
                    double deltaY = 0;
                    deltaX = points[randomIndex].x - centreDistancePairs[i].centre.x;
                    deltaY = points[randomIndex].y - centreDistancePairs[i].centre.y;
                    centreDistancePairs[i].centre.x += currentLearningRate * Exp(-i) * deltaX;
                    centreDistancePairs[i].centre.y += currentLearningRate * Exp(-i) * deltaY;
                }
            }
        }
        public static List<CentreDistancePair> CreateAndSortDistanceCentrePairs(Centre[] centres, List<Point> points, int randomIndex)
        {
            List<CentreDistancePair> toReturn = new List<CentreDistancePair>();
            for (int i = 0; i < centres.Length; i++)
            {
                toReturn.Add(new CentreDistancePair()
                {
                    distance = Sqrt(Pow(points[randomIndex].x - centres[i].x, 2) + Pow(points[randomIndex].y - centres[i].y, 2)),
                    centre = centres[i]
                }
                    );
            }
            toReturn = (from c in toReturn
                        orderby c.distance ascending
                        select c).ToList();
            return toReturn;
        }




        public static List<int> GenerateNumbers(List<Point> points)
        {
            List<int> numbers = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                numbers.Add(i);
            }
            return numbers;
        }



        public static void FindWinnerCordinates(ref int x, ref int y, Centre[,] centresMatrix, Centre winner)
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (centresMatrix[i, j] == winner)
                    {
                        x = i;
                        y = j;
                    }
                }
            }
        }

        public static void UpdateCentresActivitiesAndCordinatesKohonen(int minX, int minY, Centre[,] centresMatrix, List<Point> points, Centre winner, Centre tired, int randomIndex)
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    double deltaX = 0;
                    double deltaY = 0;
                    if (((i == (minX - 1) && j == minY)
                         || (i == (minX + 1) && j == minY)
                         || (i == minX && j == (minY - 1))
                         || (i == minX && j == (minY - 1)))
                         && centresMatrix[i, j] != tired
                        )
                    {
                        deltaX = points[randomIndex].x - centresMatrix[i, j].x;
                        deltaY = points[randomIndex].y - centresMatrix[i, j].y;
                        centresMatrix[i, j].x += currentLearningRate * 0.5 * deltaX;
                        centresMatrix[i, j].y += currentLearningRate * 0.5 * deltaY;
                    }

                    if (neighborhood == Neighborhood.far)
                    {
                        if (((i == (minX - 1) && j == (minY - 1))
                                || (i == (minX - 1) && j == (minY + 1))
                                || (i == (minX + 1) && j == (minY - 1))
                                || (i == (minX - 1) && j == (minY - 1)))
                                 && centresMatrix[i, j] != tired
                            )
                        {
                            deltaX = points[randomIndex].x - centresMatrix[i, j].x;
                            deltaY = points[randomIndex].y - centresMatrix[i, j].y;
                            centresMatrix[i, j].x += currentLearningRate * 0.25 * deltaX;
                            centresMatrix[i, j].y += currentLearningRate * 0.25 * deltaY;
                        }
                    }
                    if (centresMatrix[i, j] == winner)
                    {
                        deltaX = points[randomIndex].x - winner.x;
                        deltaY = points[randomIndex].y - winner.y;
                        winner.x += currentLearningRate * 1 * deltaX;
                        winner.y += currentLearningRate * 1 * deltaY;
                        winner.activity -= 0.5;
                    }

                    if (centresMatrix[i, j] != winner)
                    {
                        centresMatrix[i, j].activity += 1.0 / (rows * columns);
                    }
                }
            }
        }

        public static void ResetAllocations(Centre[] centres)
        {
            foreach (Centre c in centres)
            {
                c.ResetPoints();
            }
        }

        public static void InsertIterationErrorIntoList(Centre[] centres, List<Point> points, List<double> diffrence)
        {
            ComputeDistancesAndAllocatePoints(centres, points);
            diffrence.Add(ComputeEpochError(centres, points));
            ResetAllocations(centres);
        }
    }
    #endregion
}
