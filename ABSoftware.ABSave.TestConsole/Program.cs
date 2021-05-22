using ABSoftware.ABSave.Mapping;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BinaryPack;
using MessagePack;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using ABSoftware.ABSave.Serialization;
using System.Reflection;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace ABSoftware.ABSave.Testing.ConsoleApp
{
    public class TestBenchmark
    {
        public MemoryStream ABSaveResult;
        public MemoryStream ABSaveOldResult;
        public MemoryStream WhoaResult;
        public MemoryStream NewtonsoftJsonResult;
        public MemoryStream Utf8JsonResult;
        public MemoryStream TextJsonResult;
        public MemoryStream BinaryFormatterResult;
        public MemoryStream ZeroFormatterResult;
        public MemoryStream XMLResult;
        public MemoryStream MessagePackResult;
        public MemoryStream BinaryPackResult;
        public JsonResponseModel TestObj;
        public ABSaveMap Map;

        [GlobalSetup]
        public void Setup()
        {
            ABSaveOldResult = new MemoryStream();
            ABSaveResult = new MemoryStream();
            //WhoaResult = new MemoryStream();
            //NewtonsoftJsonResult = new MemoryStream();
            Utf8JsonResult = new MemoryStream();
            TextJsonResult = new MemoryStream();
            //BinaryFormatterResult = new MemoryStream();
            //XMLResult = new MemoryStream();
            MessagePackResult = new MemoryStream();
            BinaryPackResult = new MemoryStream();

            Map = ABSaveMap.Get<JsonResponseModel>(ABSaveSettings.ForSpeed);
            TestObj = JsonSerializer.Deserialize<JsonResponseModel>(File.ReadAllText($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\model.txt"));
        }

        //[Benchmark]
        //public object FastGen()
        //{
        //    var prop = MapGenerator.GenerateFastPropertyGetter(ref ParentType, ref ItemType, Getter);
        //    return prop(V);
        //}

        //[Benchmark]
        //public object SlowRun()
        //{
        //    return Getter.Invoke(V, Array.Empty<object>());
        //}

        //[Benchmark]
        //public void Mapping()
        //{
        //    Map = ABSaveMap.Get<JsonResponseModel>(ABSaveSettings.GetSpeedFocus(true));
        //}

        [Benchmark]
        public void ABSave()
        {
            ABSaveMap.Get<JsonResponseModel>(ABSaveSettings.ForSpeed);
        }

        //[Benchmark]
        //public void NewtonsoftJson()
        //{
        //    NewtonsoftJsonResult.Position = 0;
        //    Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

        //    using StreamWriter sr = new StreamWriter(NewtonsoftJsonResult, Encoding.UTF8, 1024, true);
        //    using Newtonsoft.Json.JsonWriter writer = new Newtonsoft.Json.JsonTextWriter(sr);

        //    serializer.Serialize(writer, TestObj, typeof(JsonResponseModel));
        //}

        //        [Benchmark]
        //        public void BinaryFormat()
        //        {
        //            BinaryFormatterResult.Position = 0;
        //            var formatter = new BinaryFormatter();
        //#pragma warning disable SYSLIB0011 // Type or member is obsolete
        //            formatter.Serialize(BinaryFormatterResult, TestObj);
        //#pragma warning restore SYSLIB0011 // Type or member is obsolete
        //        }

        //[Benchmark]
        //public void UTF8Json()
        //{
        //    Utf8JsonResult.Position = 0;
        //    Utf8Json.JsonSerializer.Serialize(Utf8JsonResult, TestObj);
        //}

        ////[Benchmark]
        ////public void TextJson()
        ////{
        ////    TextJsonResult.Position = 0;

        ////    using var writer = new Utf8JsonWriter(TextJsonResult, new System.Text.Json.JsonWriterOptions());
        ////    JsonSerializer.Serialize(writer, TestObj);
        ////}

        ////        //[Benchmark]
        ////        //public void XML()
        ////        //{
        ////        //    XMLResult.Position = 0;

        ////        //    var serializer = new XmlSerializer(typeof(JsonResponseModel));
        ////        //    serializer.Serialize(XMLResult, TestObj);
        ////        //}

        //[Benchmark]
        //public void MessagePack()
        //{
        //    MessagePackResult.Position = 0;
        //    MessagePackSerializer.Serialize(typeof(JsonResponseModel), MessagePackResult, TestObj);
        //}

        //[Benchmark(Baseline = true)]
        //public void BinaryPack()
        //{
        //    BinaryPackResult.Position = 0;
        //    BinaryConverter.Serialize(TestObj, BinaryPackResult);
        //}


        //        [GlobalCleanup]
        //        public void Finish()
        //        {
        //            Console.WriteLine("OUTPUT SIZES:");

        //            Print(ABSaveNew, ABSaveNewResult);
        //            Print(UTF8Json, Utf8JsonResult);
        //            Print(TextJson, TextJsonResult);
        //            Print(BinaryFormat, BinaryFormatterResult);
        //            Print(MessagePack, MessagePackResult);
        //            Print(BinaryPack, BinaryPackResult);
        //            //Print(NewtonsoftJson, NewtonsoftJsonResult);
        //            //Print(XML, XMLResult);

        //            void Print(Action a, Stream stream)
        //            {
        //                a();
        //                Console.WriteLine(a.Method.Name + ": " + stream.Length);
        //            }
        //        }
    }


    class Program
    {
        static void Main()
        {
            //GenerateAndSaveNewModel();
            //TestOutputSize();
            BenchmarkRunner.Run<TestBenchmark>();
            Console.ReadLine();


            var benchmarks = new TestBenchmark();
            benchmarks.Setup();

            for (int i = 0; i < 4; i++)
                benchmarks.ABSave();

            GC.Collect();

            Debugger.Break();

            for (int i = 0; i < 47629; i++)
                benchmarks.ABSave();

            Debugger.Break();
        }

        public static void TestOutputSize()
        {
            //var benchmarks = new TestBenchmark();
            //benchmarks.Setup();
            //benchmarks.Finish();
        }

        public static void GenerateAndSaveNewModel()
        {
            JsonResponseModel model = new JsonResponseModel();
            model.Initialize();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            File.WriteAllText("model.txt", JsonSerializer.Serialize(model, options));
        }
    }
}