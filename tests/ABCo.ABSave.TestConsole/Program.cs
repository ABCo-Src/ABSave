using ABCo.ABSave.Configuration;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BinaryPack;
using MessagePack;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ABCo.ABSave.Testing.ConsoleApp
{

    public class TestBenchmark
    {
        public MemoryStream ABSaveResult;
        public MemoryStream WhoaResult;
        public MemoryStream NewtonsoftJsonResult;
        public MemoryStream Utf8JsonResult;
        public MemoryStream TextJsonResult;
        public MemoryStream BinaryFormatterResult;
        public MemoryStream ZeroFormatterResult;
        public MemoryStream XMLResult;
        public MemoryStream MessagePackResult;
        public MemoryStream BinaryPackResult;
        public byte[] JsonBytes;
        public JsonResponseModel TestObj;
        public JsonResponseModel ABSaveRes;
        public ABSaveMap Map;
        public ABSaveSerializer Serializer;

        [GlobalSetup]
        public void Setup()
        {
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
            Serializer = Map.GetSerializer(ABSaveResult);

            var str = File.ReadAllText($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\model.txt");

            JsonBytes = Encoding.UTF8.GetBytes(str);
            TestObj = JsonSerializer.Deserialize<JsonResponseModel>(str);

            // Serialize everyone
            ABSave();
            UTF8Json();
            TextJson();
            MessagePack();
            BinaryPack();
        }

        //[Benchmark]
        public void ABSave()
        {
            ABSaveResult.Position = 0;
            ABSaveConvert.Serialize(TestObj, Map, ABSaveResult);
        }

        //[Benchmark]
        public void UTF8Json()
        {
            Utf8JsonResult.Position = 0;
            Utf8Json.JsonSerializer.Serialize(Utf8JsonResult, TestObj);
        }

        //[Benchmark]
        public void TextJson()
        {
            TextJsonResult.Position = 0;

            using var writer = new Utf8JsonWriter(TextJsonResult, new JsonWriterOptions());
            JsonSerializer.Serialize(writer, TestObj);
        }

        //[Benchmark]
        public void MessagePack()
        {
            MessagePackResult.Position = 0;
            MessagePackSerializer.Serialize(typeof(JsonResponseModel), MessagePackResult, TestObj);
        }

        //[Benchmark(Baseline = true)]
        public void BinaryPack()
        {
            BinaryPackResult.Position = 0;
            BinaryConverter.Serialize(TestObj, BinaryPackResult);
        }

        [Benchmark]
        public JsonResponseModel ABSave_Deserialize()
        {
            ABSaveResult.Position = 0;
            ABSaveRes = ABSaveConvert.Deserialize<JsonResponseModel>(ABSaveResult, Map);
            return null;
            //return null;
        }

        [Benchmark]
        public JsonResponseModel UTF8Json_Deserialize()
        {
            Utf8JsonResult.Position = 0;
            return Utf8Json.JsonSerializer.Deserialize<JsonResponseModel>(Utf8JsonResult);
        }

        [Benchmark]
        public JsonResponseModel TextJson_Deserialize()
        {
            TextJsonResult.Position = 0;

            var reader = new Utf8JsonReader(JsonBytes);
            return JsonSerializer.Deserialize<JsonResponseModel>(ref reader);
        }

        [Benchmark]
        public JsonResponseModel MessagePack_Deserialize()
        {
            MessagePackResult.Position = 0;
            return MessagePackSerializer.Deserialize<JsonResponseModel>(MessagePackResult);
        }

        [Benchmark(Baseline = true)]
        public JsonResponseModel BinaryPack_Deserialize()
        {
            BinaryPackResult.Position = 0;
            return BinaryConverter.Deserialize<JsonResponseModel>(BinaryPackResult);
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
        //public void XML()
        //{
        //    XMLResult.Position = 0;

        //    var serializer = new XmlSerializer(typeof(JsonResponseModel));
        //    serializer.Serialize(XMLResult, TestObj);
        //}



        [GlobalCleanup]
        public void Finish()
        {
            Console.WriteLine("OUTPUT SIZES:");

            Print(ABSave, ABSaveResult);
            //Print(UTF8Json, Utf8JsonResult);
            //Print(TextJson, TextJsonResult);
            //Print(MessagePack, MessagePackResult);
            //Print(BinaryPack, BinaryPackResult);
            //Print(NewtonsoftJson, NewtonsoftJsonResult);
            //Print(XML, XMLResult);

            void Print(Action a, Stream stream)
            {
                a();
                Console.WriteLine(a.Method.Name + ": " + stream.Length);
            }
        }
    }


    class Program
    {
        static void Main()
        {
            //GenerateAndSaveNewModel();
            TestOutputSize();
            //Console.ReadLine();

            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(null, new DebugInProcessConfig());
            //BenchmarkRunner.Run<TestBenchmark>();
            Console.ReadLine();

            var benchmarks = new TestBenchmark();
            benchmarks.Setup();

            for (int i = 0; i < 16; i++)
            {
                benchmarks.ABSave();
            }

            GC.Collect();

            Debugger.Break();

            for (int i = 0; i < 10000000; i++)
            {
                benchmarks.ABSave();
            }

            Debugger.Break();
        }

        public static void TestOutputSize()
        {
            var benchmarks = new TestBenchmark();
            benchmarks.Setup();
            benchmarks.ABSave_Deserialize();
            benchmarks.Finish();
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