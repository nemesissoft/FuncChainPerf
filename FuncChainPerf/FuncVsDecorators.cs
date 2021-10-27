using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;


namespace FuncChainPerf
{
    /*
|           Method |        Mean |     Error |    StdDev | Ratio |  Gen 0 | Allocated |
|----------------- |------------:|----------:|----------:|------:|-------:|----------:|
| TransformClasses |   958.10 ns |  5.394 ns |  4.782 ns |  1.00 |      - |         - |
|    TransformFunc |   736.81 ns |  3.840 ns |  3.592 ns |  0.77 |      - |         - |
|  TransformNative |   302.39 ns |  1.040 ns |  0.922 ns |  0.32 |      - |         - |
|                  |             |           |           |       |        |           |
|    CreateClasses | 2,355.82 ns | 15.455 ns | 13.701 ns |  1.00 | 1.9112 |  12,000 B |
|       CreateFunc |    84.95 ns |  0.425 ns |  0.377 ns |  0.04 |      - |         - |
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

        [Benchmark, BenchmarkCategory("Transform")]
        public double TransformNative()
        {
            double d = 0;
            for (int i = 0; i < ITERATIONS; i++)
                d = new Signal(new Signal(new Signal(new Signal(50).Value + 16).Value * 60).Value + 5).Value;
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
