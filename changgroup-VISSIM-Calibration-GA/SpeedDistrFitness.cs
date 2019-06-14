using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Randomizations;

using VISSIMLIB;

namespace VISSIMCalibrationWithGeneticSharp
{
    public class SpeedDistrFitness : IFitness
    {
        public SpeedDistrFitness(int numberOfBuckets)
        {

        }

        public double Evaluate(IChromosome chromosome)
        {
            return fitness;
        }


    }
}
