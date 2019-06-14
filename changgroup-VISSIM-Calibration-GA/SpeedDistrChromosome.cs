using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace VISSIMCalibrationWithGeneticSharp
{
    public class SpeedDistrChromosome : ChromosomeBase
    {
        public SpeedDistrChromosome(int numberOfBuckets) : base(numberOfBuckets)
        {
            throw new NotImplementedException();
        }

        public double Distance { get; internal set; }

        public override Gene GenerateGene(int geneIndex)
        {
            throw new NotImplementedException();
        }

        public override IChromosome CreateNew()
        {
            throw new NotImplementedException();
        }

        public override IChromosome Clone()
        {
            var clone = base.Clone() as DesSpeedDistrChromosome;
            clone.Distance = Distance;

            return clone;
        }
    }
}
