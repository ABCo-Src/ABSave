
using ABSoftware.ABSave.Mapping;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BinaryPack;
using MessagePack;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

namespace ABSoftware.ABSave.Testing.ConsoleApp
{
    public class TestBenchmark
    {
        public MemoryStream ABSaveResult;
        public MemoryStream NewtonsoftJsonResult;
        public MemoryStream Utf8JsonResult;
        public MemoryStream TextJsonResult;
        public MemoryStream ZeroFormatterResult;
        public MemoryStream XMLResult;
        public MemoryStream MessagePackResult;
        public MemoryStream BinaryPackResult;
        public Universe TestObj;
        public ABSaveSettings Settings = ABSaveSettings.PrioritizeSize;

        [GlobalSetup]
        public void Setup()
        {
            ABSaveResult = new MemoryStream();
            NewtonsoftJsonResult = new MemoryStream();
            Utf8JsonResult = new MemoryStream();
            TextJsonResult = new MemoryStream();
            ZeroFormatterResult = new MemoryStream();
            XMLResult = new MemoryStream();
            MessagePackResult = new MemoryStream();
            BinaryPackResult = new MemoryStream();

            TestObj = Universe.GenerateUniverse();
        }

        //[Benchmark]
        //public void ABSave()
        //{
        //    ABSaveResult.Position = 0;

        //    var writer = new ABSaveWriter(ABSaveResult, Settings);
        //    ABSaveObjectConverter.Serialize(TestObj, typeof(Universe), writer);
        //}


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
        //public void ZeroFormatter()
        //{
        //    ZeroFormatterResult.Position = 0;
        //    ZeroFormatterSerializer.Serialize(TestObj);
        //}

        [Benchmark]
        public void XML()
        {
            XMLResult.Position = 0;

            var serializer = new XmlSerializer(typeof(Universe));
            serializer.Serialize(XMLResult, TestObj);
        }

        [Benchmark]
        public void MessagePack()
        {
            MessagePackResult.Position = 0;
            MessagePackSerializer.Serialize(typeof(Universe), MessagePackResult, TestObj);
        }

        [Benchmark(Baseline = true)]
        public void BinaryPack()
        {
            BinaryPackResult.Position = 0;
            BinaryConverter.Serialize(TestObj, BinaryPackResult);
        }

        [GlobalCleanup]
        public void Finish()
        {
            Console.WriteLine("OUTPUT SIZES:");

            //Print(ABSave, ABSaveResult);
            Print(NewtonsoftJson, NewtonsoftJsonResult);
            Print(UTF8Json, Utf8JsonResult);
            Print(TextJson, TextJsonResult);
            //Print(ZeroFormatter, ZeroFormatterResult);
            Print(XML, XMLResult);
            Print(MessagePack, MessagePackResult);
            Print(BinaryPack, BinaryPackResult);

            void Print(Action a, Stream stream)
            {
                a();
                Console.WriteLine(a.Method.Name + ": " + stream.Length);
            }

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
            BenchmarkRunner.Run<TestBenchmark>();
        }
    }
}