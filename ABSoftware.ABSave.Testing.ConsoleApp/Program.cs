
using ABSoftware.ABSave.Mapping;
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
   

    public class TestBenchmark
    {
        public MemoryStream ABSaveResult;
        public MemoryStream NewtonsoftJsonResult;
        public MemoryStream Utf8JsonResult;
        public MemoryStream TextJsonResult;
        public Universe TestObj;
        public ABSaveSettings Settings = ABSaveSettings.PrioritizeSize;

        [GlobalSetup]
        public void Setup()
        {
            ABSaveResult = new MemoryStream();
            NewtonsoftJsonResult = new MemoryStream();
            Utf8JsonResult = new MemoryStream();
            TextJsonResult = new MemoryStream();

            TestObj = Universe.GenerateUniverse();
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
            Console.WriteLine(Utf8JsonResult.Length);
            Console.WriteLine(TextJsonResult.Length);
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
            ABSaveObjectConverter.Serialize(TestObj, typeof(Universe), writer);
            //var reader = new ABSaveReader(ABSaveResult, Settings);
            //return (Planet)ABSaveObjectConverter.Deserialize(typeof(Planet), reader);
        }

        [Benchmark]
        public void NewtonsoftJson()
        {
            NewtonsoftJsonResult.Position = 0;
            JsonSerializer serializer = new JsonSerializer();

            using StreamWriter sr = new StreamWriter(NewtonsoftJsonResult, Encoding.UTF8, 1024, true);
            using JsonWriter writer = new JsonTextWriter(sr);

            serializer.Serialize(writer, TestObj, typeof(Universe));
        }

        [Benchmark]
        public void UTF8Json()
        {
            Utf8JsonResult.Position = 0;
            Utf8Json.JsonSerializer.Serialize(Utf8JsonResult, TestObj);
            Utf8JsonResult.Flush();
        }

        [Benchmark]
        public void TextJson()
        {
            TextJsonResult.Position = 0;

            using var writer = new System.Text.Json.Utf8JsonWriter(TextJsonResult, new System.Text.Json.JsonWriterOptions());
            System.Text.Json.JsonSerializer.Serialize(writer, TestObj);

            TextJsonResult.Flush();
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

            t.ABSave();
            Debugger.Break();
            t.ABSave();
            Debugger.Break();

            t.End();
            Console.ReadLine();
        }
    }
}
