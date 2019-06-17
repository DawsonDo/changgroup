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
            float maxStep = 20f;

            double[] minVals = new double[m_numberSpeedDistrPoints];
            double[] maxVals = new double[m_numberSpeedDistrPoints];
            int[] totalBits = new int[m_numberSpeedDistrPoints];
            int[] fractionDigits = new int[m_numberSpeedDistrPoints];

            for (int i = 0; i < m_numberSpeedDistrPoints; i++)
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
        static string coreName = "MD4_Forestville";
        static string Path_of_network = "C:\\Users\\Changgroup-ready\\Documents\\Dawson Do\\MD4_Forestville\\"; // always use \\ at the end !!

        static Boolean makeSureVISSIMisInitialized = false;

        static int seedForThisEvaluation = 1;

        static IVissim vis;
        static ISimulation sim;
        static System.IO.StreamWriter file_VISSIM_Com;
        string filenameOf_file_VISSIM_Com = "";

        public static double simulationPeriod = 3600;//Set to 3600 after the pilot test

        //// ========================================================================
        // Reads and stores expected distribution information
        //==========================================================================
        public SpeedDistrFitness(string filename, int dataBuckets)
        {
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

            double SimPeriod = sim.get_AttValue("simperiod");
            Console.WriteLine("simPeriod (before setting)" + SimPeriod);
            sim.set_AttValue("simPeriod", simulationPeriod);
            SimPeriod = sim.get_AttValue("simperiod");
            Console.WriteLine("simPeriod (after setting)" + SimPeriod);

            IGraphics igraphic0 = vis.Graphics;
            igraphic0.set_AttValue("QuickMode", 1); //No moving vehicles in VISSIM. 1:activate; 0:deactivate

            //reads raw data and generates observed histogram

            //
        }

        public double[] expectedHistogram { get; private set; }

        //// ========================================================================
        // Evaluates the fitness of the chromosome
        //==========================================================================
        public double Evaluate(IChromosome chromosome)
        {
            // Creates a Desired Speed Distribution based on the chromosome


            double[] speedDistrPoints = new double[chromosome.Length];

            double[] observedHistogram = VissimRunAndResults(speedDistrPoints);

            // Performs a Pearson's Chi-Squared Test to evaluate goodness of fit

            double fitness = 0.0;
            for (int i = 0; i < expectedHistogram.Length; i++)
            {
                fitness += Math.Pow(observedHistogram[i] - expectedHistogram[i], 2) / expectedHistogram[i];
            }

            return fitness;
        }

        private double[] VissimRunAndResults(double[] SpeedDistrPoints)
        {
            Debug.Assert(makeSureVISSIMisInitialized == true);
            sim.set_AttValue("RandSeed", seedNum);

            int seedNumTmp = sim.get_AttValue("RandSeed");
            Console.WriteLine("seed after set in VISSIM " + seedNumTmp);

            sim.set_AttValue("NumRuns", 1);

            int NumRuns = sim.get_AttValue("NumRuns");
            Console.WriteLine("               numRuns" + NumRuns);

            double percentileStep = 100 / SpeedDistrPoints.Length;
            double[] speedDistrPercentiles = new double[SpeedDistrPoints.Length + 1];

            IDesSpeedDistribution desSpeedDistr0 = vis.Net.DesSpeedDistributions.get_ItemByKey(1047);//1047:DDI Urban Desired Spd
            desSpeedDistr0.SpeedDistrDatPts.SetMultipleAttributes("x", SpeedDistrPoints);
            desSpeedDistr0.SpeedDistrDatPts.SetMultipleAttributes("fx", speedDistrPercentiles);

            return observedHistogram;
        }
    }
}