using System;
using System.IO;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;

using VISSIMLIB;

namespace VISSIMCalibrationWithGeneticSharp
{
    class MainClass
    {
        static string coreName = "MD4_Forestville";
        static string Path_of_network =  "C:\\Users\\Changgroup-ready\\Documents\\Dawson Do\\MD4_Forestville\\"; // always use \\ at the end !!

        static Boolean makeSureVISSIMisInitialized = false;

        static int seedForThisEvaluation = 1;

        static IVissim vis;
        static ISimulation sim;
        static System.IO.StreamWriter file_VISSIM_Com;
        string filenameOf_file_VISSIM_Com = "";

        public static double simulationPeriod = 3600;//Set to 3600 after the pilot test
        public static void Main(string[] args)
        {

            //// ========================================================================
            // Initialize chromosome structure and fitness
            //==========================================================================
            int m_SpeedDistrBuckets = 10;
            float maxStep = 20f;

            double[] minVals = new double[m_SpeedDistrBuckets];
            double[] maxVals = new double[m_SpeedDistrBuckets];
            int[] totalBits = new int[m_SpeedDistrBuckets];
            int[] fractionDigits = new int[m_SpeedDistrBuckets];

            for (int i = 0; i < m_SpeedDistrBuckets; i++)
            {
                minVals[i] = 0;
                maxVals[i] = maxStep;
                totalBits[i] = 10;
                fractionDigits[i] = 4;
            }

            var chromosome = new FloatingPointChromosome(minVals, maxVals, totalBits, fractionDigits);

            // Creates Fitness criterion using raw data
            string observedDataFile = "C:\test.csv";
            int m_testDataBuckets = 20;

            var fitness = new SpeedDistrFitness(observedDataFile, m_testDataBuckets);

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

            string Filename = Path_of_network + coreName + ".inpx";
            vis.LoadNet(Filename, false);

            Filename = Path_of_network + coreName + "COPY.inpx";
            vis.SaveNetAs(Filename);

            sim = vis.Simulation;//save as static variable

            //// ========================================================================
            // Initialize operators and run the algorithm
            //==========================================================================

            var selection = new EliteSelection();
            var crossover = new UniformCrossover(0.5f);
            var mutation = new FlipBitMutation();
            var population = new Population(50, 100, chromosome);

            GeneticAlgorithm m_ga = new GeneticAlgorithm(
                population,
                fitness,
                selection,
                crossover,
                mutation);

            //// ========================================================================
            // Select termination method
            //==========================================================================
            m_ga.Termination = new FitnessStagnationTermination(100);

            Console.WriteLine("Generation: (x1, y1), (x2, y2) = distance");

            var latestFitness = 0.0;

            m_ga.GenerationRan += (sender, e) =>
            {
                var bestChromosome = m_ga.BestChromosome as FloatingPointChromosome;
                var bestFitness = bestChromosome.Fitness.Value;

                if (bestFitness != latestFitness)
                {
                    latestFitness = bestFitness;
                    var phenotype = bestChromosome.ToFloatingPoints();

                    Console.WriteLine(
                        "Generation {0,2}: ({1},{2}),({3},{4}) = {5}",
                        m_ga.GenerationsNumber,
                        phenotype[0],
                        phenotype[1],
                        phenotype[2],
                        phenotype[3],
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
        //// ========================================================================
        // Reads and stores expected distribution information
        //==========================================================================
        public SpeedDistrFitness(string filename, int dataBuckets)
        {
            //reads raw data and generates observed histogram

            //
        }



        //// ========================================================================
        // Evaluates the fitness of the chromosome
        //==========================================================================
        public double Evaluate(IChromosome chromosome)
        {
            // Creates a Desired Speed Distribution based on the chromosome

            // Sets a new vehicle composition based on chromosome Speed Distribution

            // Runs a Vissim simulation and collects experimental data

            // Performs a Pearson's Chi-Squared Test to evaluate goodness of fit


            return fitness;
        }


    }
}