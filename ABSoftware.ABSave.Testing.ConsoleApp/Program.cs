using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.IO;

namespace ABSoftware.ABSave.Testing.ConsoleApp
{
    public class TestBenchmark
    {

        [IterationSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public void MemoryWriterTest()
        {
                
        }

        [Benchmark]
        public void StreamWriterTest()
        {
        }
    }

    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<TestBenchmark>();
        }
    }
}
