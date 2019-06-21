using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;//Allow assert function. Allow GetTimestamp

using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;

using VISSIMLIB;

using Microsoft.VisualBasic.FileIO;
using MathNet.Numerics.Distributions;

namespace VISSIMCalibrationWithGeneticSharp
{
    class Program
    {
        public static void Main(string[] args)
        {

            //// ========================================================================
            // Initialize chromosome structure and fitness
            //==========================================================================
            int m_numberSpeedDistrPoints = 10;
            float maxSpeedStep = 20f;

            double[] minVals = new double[m_numberSpeedDistrPoints];
            double[] maxVals = new double[m_numberSpeedDistrPoints];
            int[] totalBits = new int[m_numberSpeedDistrPoints];
            int[] fractionDigits = new int[m_numberSpeedDistrPoints];

            for (int i = 0; i < m_numberSpeedDistrPoints; i++)
            {
                minVals[i] = 0;
                maxVals[i] = maxSpeedStep;
                totalBits[i] = 10;
                fractionDigits[i] = 4;
            }

            var chromosome = new FloatingPointChromosome(minVals, maxVals, totalBits, fractionDigits);

            // Creates Fitness criterion using raw data
            string observedDataFilePath = "C:\test.csv";
            int m_testDataBuckets = 20;

            var fitness = new SpeedDistrFitness(observedDataFilePath, m_testDataBuckets, maxSpeedStep, m_numberSpeedDistrPoints);

            //// ========================================================================
            // Initialize operators and run the algorithm
            //==========================================================================

            var selection = new EliteSelection();
            var crossover = new UniformCrossover(0.5f);
            var mutation = new FlipBitMutation();
            var population = new Population(10, 20, chromosome);

            // Terminate once the two distributions are homogeneous up to a specified significance level
            var significance = .05;// Percent significance
            
            ChiSquared c = new ChiSquared(m_testDataBuckets - 1);
            double threshold = c.InverseCumulativeDistribution(1 - significance);

            GeneticAlgorithm m_ga = new GeneticAlgorithm(
                population,
                fitness,
                selection,
                crossover,
                mutation)
            {
                //// ========================================================================
                // Select termination method
                //==========================================================================
                Termination = new FitnessThresholdTermination(-threshold)// MUST BE NEGATIVE !!
            };

            Console.WriteLine("Generation: Significance");

            var latestFitness = 0.0;

            m_ga.GenerationRan += (sender, e) =>
            {
                var bestChromosome = m_ga.BestChromosome as FloatingPointChromosome;
                var bestFitness = bestChromosome.Fitness.Value;

                if (bestFitness != latestFitness)
                {
                    latestFitness = bestFitness;

                    Console.WriteLine(
                        "Generation {0,2}: {5}",
                        m_ga.GenerationsNumber,
                        bestFitness
                    );
                }
            };

            m_ga.Start();

            Console.ReadKey();
        }
    }

    class SpeedDistrFitness : IFitness
    {
        static readonly string coreName = "MD4_Forestville";
        static readonly string Path_of_network = "C:\\Users\\Changgroup-ready\\Documents\\Dawson Do\\MD4_Forestville\\"; // always use \\ at the end !!

        static Boolean makeSureVISSIMisInitialized = false;

        static int seedForThisEvaluation = 1;

        static IVissim vis;
        static ISimulation sim;
        static System.IO.StreamWriter file_VISSIM_Com;
        readonly string filenameOf_file_VISSIM_Com = "";

        public static double simulationPeriod = 3600;//Set to 3600 after the pilot test

        static int m_num_dataBuckets;
        static double histogramMax;
        static double bucketSize;

        //// ========================================================================
        // Initializes the optimization problem
        // Loads Vissim files and sets parameters for simulation
        //==========================================================================
        public SpeedDistrFitness(string dataFilePath, int num_dataBuckets, float maxSpeedStep, int num_SpeedDistrPoints)
        {

            m_num_dataBuckets = num_dataBuckets;

            file_VISSIM_Com = new System.IO.StreamWriter(filenameOf_file_VISSIM_Com + "VISSIM_COM.txt");

            //// ========================================================================
            // Opens Vissim
            //==========================================================================

            Console.WriteLine("Now, initializing VISSIM.");
            //Open the file... and all the trivial things
            //0. Trivial busisness:
            makeSureVISSIMisInitialized = true;// This is to flip the flag.

            //0b: Some trivial tasks:

            Console.WriteLine("Simulation Period:" + simulationPeriod);//!!!! Do not delete this. This is a reminder.

            //1. Open the file
            //1a)Open the VISSIM
            Console.WriteLine("Prepare to open VISSIM...");

            vis = new Vissim();// Software VISSIM ... the static variable is assigned.

            Console.WriteLine("...VISSIM opened");

            //1b)Open the FILE (in additional to the software)

            string visFilename = Path_of_network + coreName + ".inpx";
            vis.LoadNet(visFilename, false);

            visFilename = Path_of_network + coreName + "COPY.inpx";
            vis.SaveNetAs(visFilename);

            sim = vis.Simulation;//save as static variable

            double SimPeriod = sim.get_AttValue("simperiod");
            Console.WriteLine("simPeriod (before setting)" + SimPeriod);
            sim.set_AttValue("simPeriod", simulationPeriod);
            SimPeriod = sim.get_AttValue("simperiod");
            Console.WriteLine("simPeriod (after setting)" + SimPeriod);

            IGraphics igraphic0 = vis.Graphics;
            igraphic0.set_AttValue("QuickMode", 1); //No moving vehicles in VISSIM. 1:activate; 0:deactivate
            vis.SuspendUpdateGUI(); ;

            //reads raw data and generates observed histogram
            ExpectedHistogram = new double[m_num_dataBuckets];
            histogramMax = maxSpeedStep * num_SpeedDistrPoints;
            bucketSize = histogramMax / m_num_dataBuckets;

            using (TextFieldParser csvParser = new TextFieldParser(dataFilePath))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    int bucketIndex = 0;
                    string[] fields = csvParser.ReadFields();
                    double speedValue = fields[0];

                    bucketIndex = (int)Math.Ceiling(speedValue / bucketSize) - 1;
                    if (bucketIndex < 0)
                        continue;
                    ExpectedHistogram[bucketIndex]++;
                }
            }

            // Normalize Histogram--turns into probability mass function that sums to 1
            var sum = ExpectedHistogram.Sum();
            ExpectedHistogram = (double[])ExpectedHistogram.Select(d => d / sum);

        }

        public double[] ExpectedHistogram { get; private set; }

        //// ========================================================================
        // Evaluates the fitness of the chromosome
        //==========================================================================
        public double Evaluate(IChromosome chromosome)
        {
            // Create a Desired Speed Distribution based on the chromosome
            double[] speedDistrPoints = new double[chromosome.Length];
            var fc = chromosome as FloatingPointChromosome;
            var values = fc.ToFloatingPoints();

            speedDistrPoints[0] = values[0];

            for (int i = 1; i < speedDistrPoints.Length; i++)
            {
                speedDistrPoints[i] = speedDistrPoints[i - 1] + values[i];
            }

            // Run a simulation based on the chromosome Desired Speed Distribution
            double[] ObservedHistogram = VissimRunAndResults(speedDistrPoints);
            var sum = ObservedHistogram.Sum();

            // Fitness value is an increasing negative function, approaching zero error.
            // Negative of the Pearson's Chi Square Test Statistic
            double fitness = 0.0;
            for (int i = 0; i < ExpectedHistogram.Length; i++)
            {
                fitness -= Math.Pow(ObservedHistogram[i] / sum - ExpectedHistogram[i], 2) / ExpectedHistogram[i];
            }

            fitness *= sum;

            return fitness;
        }

        //// ========================================================================
        // Outputs histogram of observed counts
        //==========================================================================
        private double[] VissimRunAndResults(double[] SpeedDistrPoints)
        {
            Debug.Assert(makeSureVISSIMisInitialized == true);

            seedForThisEvaluation++;
            sim.set_AttValue("RandSeed", seedForThisEvaluation);
            sim.set_AttValue("NumRuns", 1);

            //// ========================================================================
            // Sets Desired Speed Distribution for testing
            //==========================================================================
            double percentileStep = 100 / (SpeedDistrPoints.Length - 1);
            double[] SpeedDistrPercentiles = new double[SpeedDistrPoints.Length];

            SpeedDistrPercentiles[0] = 0.0;
            SpeedDistrPercentiles[SpeedDistrPoints.Length] = 100.0;

            for (int i = 1; i < SpeedDistrPoints.Length - 1; i++)
            {
                SpeedDistrPercentiles[i] = i * percentileStep;
            }

            IDesSpeedDistribution desSpeedDistr0 = vis.Net.DesSpeedDistributions.get_ItemByKey(1047);//1047:DDI Urban Desired Spd
            desSpeedDistr0.SpeedDistrDatPts.SetMultipleAttributes("X", SpeedDistrPoints);
            desSpeedDistr0.SpeedDistrDatPts.SetMultipleAttributes("FX", SpeedDistrPercentiles);

            vis.SaveNet();

            double[] ObservedHistogram = new double[m_num_dataBuckets];

            // Run the simulation
            vis.Simulation.set_AttValue("UseMaxSimSpeed", true);
            // To change the speed use: Vissim.Simulation.set_AttValue("SimSpeed", 10); // 10 => 10 Sim. sec. / s
            vis.Simulation.RunContinuous();


            using (TextFieldParser csvParser = new TextFieldParser(dataFilePath))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    int bucketIndex = 0;
                    string[] fields = csvParser.ReadFields();
                    double speedValue = fields[0];

                    bucketIndex = (int)Math.Ceiling(speedValue / bucketSize) - 1;
                    if (bucketIndex < 0)
                        continue;
                    ObservedHistogram[bucketIndex]++;
                }
            }

            return ObservedHistogram;
        }
    }
}