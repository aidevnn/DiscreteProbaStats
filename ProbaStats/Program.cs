using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbaStats
{
    class Program
    {
        static Random random = new Random();

        static Dictionary<int, int[]> DicoCombinaisons = new Dictionary<int, int[]>();

        // Maximum N=30
        const int MAXN = 30;
        static void CreateCombinaisonsPascal()
        {
            DicoCombinaisons[0] = new int[1] { 1 };
            for (int k = 1; k <= MAXN; ++k)
            {
                var ar = DicoCombinaisons[k] = new int[k + 1];
                var up = DicoCombinaisons[k - 1];
                ar[0] = ar[k] = 1;
                for (int i = 1; i < k; ++i)
                    ar[i] = up[i] + up[i - 1];
            }
        }

        static int Comb(int k, int n) => k > n || n > MAXN ? 0 : DicoCombinaisons[n][k];
        static string CombStr(int k, int n) => $"({k,-2} {n,2}) = {Comb(k, n)}";

        static void DisplayPascalTriangle()
        {
            Console.WriteLine("Pascal Triangle");
            int mx = Math.Min(12, DicoCombinaisons.Count);
            Func<int, string> spaces = i => string.Join(" ", Enumerable.Repeat(" ", mx + 1 - i));
            for (int k = 0; k <= mx; ++k)
                Console.WriteLine("{0}{1}", spaces(k), string.Join("", DicoCombinaisons[k].Select(j => $"{j,4}")));
        }

        static void TestCombinaisons()
        {
            DisplayPascalTriangle();

            Console.WriteLine(CombStr(3, 6));
            Console.WriteLine(CombStr(2, 5));
            Console.WriteLine(CombStr(5, 10));
        }

        static double Uniform(int n) => 1.0 / (double)n;
        static double Binomial(int k, int n, double p) => Comb(k, n) * Math.Pow(p, k) * Math.Pow(1 - p, n - k);

        static double Poisson(int k, double l)
        {
            double p = Math.Exp(-l);
            for (double i = 1; i <= k; ++i) p *= l / i;
            return p;
        }

        static double[] ProbaDensityFuncBernoulli(double p) => new double[2] { 1.0 - p, p };
        static double[] ProbaDensityFuncUniform(int n) => Enumerable.Repeat(Uniform(n + 1), n + 1).ToArray();
        static double[] ProbaDensityFuncBinomial(int n, double p) => Enumerable.Range(0, n + 1).Select(k => Binomial(k, n, p)).ToArray();
        static double[] ProbaDensityFuncPoisson(double l) => Enumerable.Range(0, (int)(l + 1) * 10).Select(k => Poisson(k, l)).ToArray();
        static double[] CumulativeDensistyFunction(double[] pdf)
        {
            double[] cdf = new double[pdf.Length];
            cdf[0] = pdf[0];
            for (int k = 0; k < pdf.Length - 1; ++k)
                cdf[k + 1] = cdf[k] + pdf[k + 1];

            return cdf;
        }

        static int DistribGen(double[] cdf)
        {
            double p = random.NextDouble();
            int k = 0;
            for (k = 0; k < cdf.Length; ++k)
                if (cdf[k] > p) return k;

            return cdf.Length - 1;
        }

        static int[] GenSample(int size, double[] cdf)
        {
            List<int> l0 = new List<int>();
            while (l0.Count < size)
                l0.Add(DistribGen(cdf));

            return l0.ToArray();
        }

        static double TestDiscreteKolmogorovSmirnov(int[] sample, double[] cdf)
        {
            double n = sample.Length;
            var cdf0 = Enumerable.Range(0, cdf.Length).Select(i => sample.Count(j => j <= i) / n).ToArray();
            var maxF = Enumerable.Range(0, cdf.Length).Select(i => Math.Abs(cdf[i] - cdf0[i])).Max();

            double test = 1.36 / Math.Sqrt(n);
            string result = maxF < test ? "Accepted" : "Rejected";

            Console.WriteLine($"Print[\"Discrete KolmogorovSmirnov:{maxF:F9} Test:{test:F6} => {result} alpha=0.05\"]");

            return maxF;
        }

        static int PoissonGen1(double l)
        {
            var L = Math.Exp(-l);
            int k = 0;
            double p = 1.0;
            while (p > L)
            {
                ++k;
                p *= random.NextDouble();
            }

            return k - 1;
        }

        static int PoissonGen2(double l)
        {
            var p = Math.Exp(-l);
            int k = 0;
            double s = p;
            double u = random.NextDouble();

            while (u > s)
            {
                ++k;
                p *= l / k;
                s += p;
            }

            return k;
        }

        static int[] SamplePoisson(int size, Func<int> gen)
        {
            List<int> l0 = new List<int>();
            while (l0.Count < size)
                l0.Add(gen());

            return l0.ToArray();
        }

        static void DisplayMathematicaInfos(int[] sample, string distribution)
        {
            Console.WriteLine($"data={{{string.Join(",", sample)}}};");
            Console.WriteLine("Histogram[data,{1}]");
            Console.WriteLine($"ProbabilityPlot[data,{distribution}]");
            Console.WriteLine($"DistributionFitTest[data,{distribution},{{\"TestStatisticTable\", \"KolmogorovSmirnov\"}}]");
        }

        static void TestSampleBernoulli(int size, double p)
        {
            var pdf = ProbaDensityFuncBernoulli(p);
            var cdf = CumulativeDensistyFunction(pdf);

            var sample = GenSample(size, cdf);
            DisplayMathematicaInfos(sample, $"BernoulliDistribution[{p}]");
            TestDiscreteKolmogorovSmirnov(sample, cdf);
        }

        static void TestSampleUniform(int size, int n)
        {
            var pdf = ProbaDensityFuncUniform(n);
            var cdf = CumulativeDensistyFunction(pdf);

            var sample = GenSample(size, cdf);
            DisplayMathematicaInfos(sample, $"DiscreteUniformDistribution[{{{0},{n}}}]");
            TestDiscreteKolmogorovSmirnov(sample, cdf);
        }

        static void TestSampleBinomial(int size, int n, double p)
        {
            n = Math.Min(MAXN, n);
            var pdf = ProbaDensityFuncBinomial(n, p);
            var cdf = CumulativeDensistyFunction(pdf);

            var sample = GenSample(size, cdf);
            DisplayMathematicaInfos(sample, $"BinomialDistribution[{n},{p}]");
            TestDiscreteKolmogorovSmirnov(sample, cdf);
        }

        static void TestSamplesPoisson(int size, double lambda)
        {
            var pdf = ProbaDensityFuncPoisson(lambda);
            var cdf = CumulativeDensistyFunction(pdf);

            var sample = GenSample(size, cdf);
            DisplayMathematicaInfos(sample, $"PoissonDistribution[{lambda}]");
            TestDiscreteKolmogorovSmirnov(sample, cdf);
        }

        static void TestSamplesPoisson2(int size, double lambda)
        {
            var sample = SamplePoisson(size, () => PoissonGen2(lambda));
            DisplayMathematicaInfos(sample, $"PoissonDistribution[{lambda}]");

            var pdf = ProbaDensityFuncPoisson(lambda);
            var cdf = CumulativeDensistyFunction(pdf);
            TestDiscreteKolmogorovSmirnov(sample, cdf);
        }

        static void Main(string[] args)
        {
            CreateCombinaisonsPascal();

            //TestCombinaisons();

            TestSampleBernoulli(1000, 0.2);
            //TestSampleUniform(1000, 10);
            //TestSampleBinomial(1000, 10, 0.7);
            //TestSamplesPoisson(1000, 10.0);
            //TestSamplesPoisson2(2000, 5.0);

            Console.ReadKey();
        }
    }
}
