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

namespace VISSIMCalibrationWithGeneticSharp
{
    class MainClass
    {
        public static void Main(string[] args)
        {

            //// ========================================================================
            // Initialize chromosome structure and fitness
            //==========================================================================
            int m_numberOfBuckets = 10;
            float maxWidth = 998f;
            float maxHeight = 680f;

            double[] minVals = new double[m_numberOfBuckets];
            double[] maxVals = new double[m_numberOfBuckets];
            int[] 

            var chromosome = new FloatingPointChromosome(
                new double[] { 0, 0, 0, 0 },
                new double[] { maxWidth, maxHeight, maxWidth, maxHeight },
                new int[] { 10, 10, 10, 10 },
                new int[] { 0, 0, 0, 0 });

            var fitness = new SpeedDistrFitness(m_numberOfBuckets);

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
                var bestChromosome = m_ga.BestChromosome as SpeedDistrChromosome;
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
}