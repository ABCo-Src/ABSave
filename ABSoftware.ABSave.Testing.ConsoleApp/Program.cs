
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ABSoftware.ABSave.Testing.ConsoleApp
{
    [Serializable]
    public class Planet
    {
        public string PlanetName = "Earth";

        public Person[] People = new Person[]
        {
            new Person(),
            new Person(),
            new Person()
        };

        public City[] Towns = new City[]
        {
            new City(),
            new City()
        };
    }
    
    [Serializable]
    public class Person
    {
        public string Name = "Alex";

        public int Age = 15;

        public string[] Hobbies = new string[]
        {
            "Programming",
            "SomethingElse"
        };
    }

    [Serializable]
    public class City
    {
        public string Name = "ABTown";

        public List<Building> Buildings = new List<Building>()
        {
            new Building(),
            new Building()
        };
    }

    [Serializable]
    public class Building
    {
        public string Name = "ABBuilding";

        public Size BuildingSize = new Size()
        {
            Width = 25196.16161d,
            Height = 25681.16141d
        };
    }

    [Serializable]
    public class Size
    {
        public double Width;
        public double Height;
    }

    public class TestBenchmark
    {
        public MemoryStream ABSaveResult;
        public MemoryStream NewtonsoftJsonResult;
        public MemoryStream FormatterResult;
        public Planet TestObj;
        public ABSaveSettings Settings = ABSaveSettings.PrioritizeSize;

        [GlobalSetup]
        public void Setup()
        {
            ABSaveResult = new MemoryStream();
            NewtonsoftJsonResult = new MemoryStream();
            FormatterResult = new MemoryStream();
            TestObj = new Planet();
        }

        [GlobalCleanup]
        public void End()
        {
            //if (ABSaveResult.Length > 0)
            //    File.WriteAllBytes("absave_data", ABSaveResult.ToArray());

            //if (NewtonsoftJsonResult.Length > 0)
            //    File.WriteAllBytes("json_data", NewtonsoftJsonResult.ToArray());

            Console.WriteLine(ABSaveResult.Length);
            Console.WriteLine(NewtonsoftJsonResult.Length);
            Console.WriteLine(FormatterResult.Length);
            ABSaveResult.Close();
            NewtonsoftJsonResult.Close();
        }

        //[Benchmark]
        //public void BinaryFormatter()
        //{
        //    FormatterResult.Position = 0;
        //    var formatter = new BinaryFormatter();
        //    formatter.Serialize(FormatterResult, TestObj);
        //}

        [Benchmark]
        public void ABSave()
        {
            ABSaveResult.Position = 0;

            var writer = new ABSaveWriter(ABSaveResult, Settings);
            ABSaveObjectConverter.Serialize(TestObj, typeof(Planet), writer);
            //var reader = new ABSaveReader(ABSaveResult, Settings);
            //return (Planet)ABSaveObjectConverter.Deserialize(typeof(Planet), reader);
        }

        [Benchmark(Baseline = true)]
        public void NewtonsoftJson()
        {
            NewtonsoftJsonResult.Position = 0;
            JsonSerializer serializer = new JsonSerializer();

            using StreamWriter sr = new StreamWriter(NewtonsoftJsonResult, Encoding.UTF8, 1024, true);
            using JsonWriter writer = new JsonTextWriter(sr);

            serializer.Serialize(writer, TestObj, typeof(Planet));
        }

        //[Benchmark]
        //public void WhoaTest()
        //{
        //    WhoaResult.Position = 0;
        //    Whoa.Whoa.SerialiseObject(WhoaResult, TestObj, SerialisationOptions.None);
        //}
    }

    class Program
    {
        static void Main()
        {
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(new string[0], new DebugInProcessConfig());
            //BenchmarkRunner.Run<TestBenchmark>();
            var t = new TestBenchmark();
            t.Setup();

            Debugger.Break();
            t.ABSave();
            Debugger.Break();

            t.End();
            Console.ReadLine();
        }
    }
}
