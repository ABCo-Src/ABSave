
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Whoa;

namespace ABSoftware.ABSave.Testing.ConsoleApp
{
    [Serializable]
    public class Planet
    {
        [Order(0)]
        public string PlanetName = "Earth";

        [Order(1)]
        public Person[] People = new Person[]
        {
            new Person(),
            new Person(),
            new Person()
        };

        [Order(2)]
        public City[] Towns = new City[]
        {
            new City(),
            new City()
        };
    }
    
    [Serializable]
    public class Person
    {
        [Order(0)]
        public string Name = "Alex";

        [Order(1)]
        public int Age = 15;

        [Order(2)]
        public string[] Hobbies = new string[]
        {
            "Programming",
            "SomethingElse"
        };
    }

    [Serializable]
    public class City
    {
        [Order(0)]
        public string Name = "ABTown";

        [Order(1)]
        public List<Building> Buildings = new List<Building>
        {
            new Building(),
            new Building()
        };
    }

    [Serializable]
    public class Building
    {
        [Order(0)]
        public string Name = "ABBuilding";

        [Order(1)]
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

    public static class StreamHolders
    {

    }

    public class TestBenchmark
    {
        public FileStream ABSaveResult;
        public MemoryStream FormatterResult;
        public FileStream NewtonsoftJsonResult;
        public MemoryStream WhoaResult;
        public Planet TestObj;

        [GlobalSetup]
        public void Setup()
        {
            ABSaveResult = File.Open("absave_data", FileMode.Create);
            NewtonsoftJsonResult = File.Open("json_data.txt", FileMode.Create);
            FormatterResult = new MemoryStream();
            WhoaResult = new MemoryStream();
            TestObj = new Planet();
        }

        [Benchmark]
        public void BinaryFormatter()
        {
            FormatterResult.Position = 0;
            var formatter = new BinaryFormatter();
            formatter.Serialize(FormatterResult, TestObj);
        }

        [Benchmark]
        public void ABSave()
        {
            ABSaveResult.Position = 0;
            var writer = new ABSaveWriter(ABSaveResult, new ABSaveSettings());
            ABSaveObjectConverter.Serialize(TestObj, typeof(Planet), writer);
        }

        [Benchmark]
        public void NewtonsoftJson()
        {
            NewtonsoftJsonResult.Position = 0;
            JsonSerializer serializer = new JsonSerializer();

            using StreamWriter sw = new StreamWriter(NewtonsoftJsonResult, Encoding.UTF8, 1024, true);
            using JsonWriter writer = new JsonTextWriter(sw);

            serializer.Serialize(writer, TestObj);
        }

        [Benchmark]
        public void WhoaTest()
        {
            WhoaResult.Position = 0;
            Whoa.Whoa.SerialiseObject(WhoaResult, TestObj, SerialisationOptions.None);
        }
    }

    class Program
    {
        static void Main()
        {
            var t = new TestBenchmark();

            t.Setup();
            t.BinaryFormatter();
            t.NewtonsoftJson();
            t.ABSave();
            t.WhoaTest();
            t.ABSaveResult.Close();
            t.NewtonsoftJsonResult.Close();

            //BenchmarkRunner.Run<TestBenchmark>();
            Console.ReadLine();
        }
    }
}
