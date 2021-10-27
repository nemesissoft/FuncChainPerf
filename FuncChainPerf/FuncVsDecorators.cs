using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;


namespace FuncChainPerf
{
    /*
|           Method |        Mean |     Error |    StdDev | Ratio |  Gen 0 | Allocated |
|----------------- |------------:|----------:|----------:|------:|-------:|----------:|
| TransformClasses |   965.90 ns | 17.717 ns | 16.572 ns |  1.00 |      - |         - |
|    TransformFunc |   686.95 ns |  2.579 ns |  2.412 ns |  0.71 |      - |         - |
|                  |             |           |           |       |        |           |
|    CreateClasses | 2,334.20 ns | 15.797 ns | 14.003 ns |  1.00 | 1.9112 |  12,000 B |
|       CreateFunc |    65.82 ns |  0.624 ns |  0.521 ns |  0.03 |      - |         - |
     */

    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    //[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions, HardwareCounter.Timer)]
    public class FuncVsDecorators
    {
        private const int ITERATIONS = 100;

        private static readonly ITransformer _transformer = new AddTransformer(5, new MulTransformer(60, new AddTransformer(16)));

        private static readonly Func<Signal, double, Signal> _adder = (signal, term) => signal with { Value = signal.Value + term };
        private static readonly Func<Signal, double, Signal> _mul = (signal, factor) => signal with { Value = signal.Value * factor };
        private static readonly Func<Signal, Signal> _func = signal => _adder(_mul(_adder(signal, 16), 60), 5);


        [Benchmark(Baseline = true), BenchmarkCategory("Transform")]
        public double TransformClasses()
        {
            double d = 0;
            for (int i = 0; i < ITERATIONS; i++)
                d = _transformer.Transform(new(50)).Value;
            return d;
        }

        [Benchmark, BenchmarkCategory("Transform")]
        public double TransformFunc()
        {
            double d = 0;
            for (int i = 0; i < ITERATIONS; i++)
                d = _func(new(50)).Value;
            return d;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Create")]
        public double CreateClasses()
        {
            ITransformer t = null;
            for (int i = 0; i < ITERATIONS; i++)
                t = new AddTransformer(5, new MulTransformer(60, new AddTransformer(16)));
            return t.Transform(new(50)).Value;
        }

        [Benchmark, BenchmarkCategory("Create")]
        public double CreateFunc()
        {
            Func<Signal, Signal> func = null;
            for (int i = 0; i < ITERATIONS; i++)
                func = signal => _adder(_mul(_adder(signal, 16), 60), 5);
            return func(new(50)).Value;
        }
    }

    record struct Signal(double Value);

    interface ITransformer
    {
        Signal Transform(Signal signal);
    }

    class IdentityTransformer : ITransformer
    {
        public Signal Transform(Signal signal) => signal;
    }

    class AddTransformer : ITransformer
    {
        private readonly double _term;
        private readonly ITransformer _transformer;

        public AddTransformer(double term, ITransformer transformer = null)
        {
            _term = term;
            _transformer = transformer ?? new IdentityTransformer();
        }

        public Signal Transform(Signal signal) => signal with { Value = _transformer.Transform(signal).Value + _term };
    }

    class MulTransformer : ITransformer
    {
        private readonly double _factor;
        private readonly ITransformer _transformer;

        public MulTransformer(double factor, ITransformer transformer = null)
        {
            _factor = factor;
            _transformer = transformer ?? new IdentityTransformer();
        }

        public Signal Transform(Signal signal) => signal with { Value = _transformer.Transform(signal).Value * _factor };
    }
}
