using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    public abstract class TestBase
    {
        public ABSaveMap CurrentMap;
        public MapItemInfo CurrentMapItem;

        public MemoryStream Stream;
        public ABSaveSerializer Serializer;
        public ABSaveDeserializer Deserializer;

        public void Initialize() => Initialize(ABSaveSettings.ForSpeed);
        public void Initialize(ABSaveSettings template, Dictionary<Type, uint> targetVersions = null)
        {
            var settingsBuilder = new SettingsBuilder
            {
                _bypassDangerousTypeChecking = true
            };

            settingsBuilder.AddConverter(typeof(BaseTypeConverter));
            settingsBuilder.AddConverter(typeof(SubTypeConverter));

            CurrentMap = ABSaveMap.Get<EmptyClass>(settingsBuilder.CreateSettings(template));

            Stream = new MemoryStream();
            Serializer = new ABSaveSerializer();
            Serializer.Initialize(Stream, CurrentMap, targetVersions);

            Deserializer = new ABSaveDeserializer();
            Deserializer.Initialize(Stream, CurrentMap);

            // NOTE: To make testing easier, add some test types as saved so they have a consistent single byte for type writing.
            InitializeSerializerSavedTypes();
        }

        private void InitializeSerializerSavedTypes()
        {
            Serializer.SavedTypes.Add(typeof(SubWithHeader), 0);
            Deserializer.SavedTypes.Add(typeof(SubWithHeader));

            Serializer.SavedTypes.Add(typeof(SubWithoutHeader), 1);
            Deserializer.SavedTypes.Add(typeof(SubWithoutHeader));

            Serializer.SavedTypes.Add(typeof(SubNoConverter), 2);
            Deserializer.SavedTypes.Add(typeof(SubNoConverter));

            Serializer.SavedTypes.Add(typeof(NestedClass), 3);
            Deserializer.SavedTypes.Add(typeof(NestedClass));
        }

        public void GoToStart() => Stream.Position = 0;

        public void ResetStateWithMapFor<T>() => ResetStateWithMapFor(typeof(T));

        public void ResetStateWithMapFor(Type type)
        {
            ResetState();

            var gen = CurrentMap.GetGenerator();
            CurrentMapItem = gen.GetMap(type);
            CurrentMap.ReleaseGenerator(gen);
        }

        public void ResetState()
        {
            // Reset the serializer and deserializer
            Serializer.Reset();
            Deserializer.Reset();
            InitializeSerializerSavedTypes();

            ResetPosition();
        }

        public void ResetPosition()
        {
            GoToStart();
            Stream.SetLength(0);
        }

        public void AssertAndGoToStart(params byte[] data)
        {
            AssertOutput(data);
            GoToStart();
        }
        
        public void AssertOutput(params byte[] expected)
        {
            var actual = Stream.ToArray();
            try
            {
                CollectionAssert.AreEqual(expected, actual);
            }
            catch
            {
                Debugger.Break();
                throw;
            }
        }

        public byte[] GetByteArr(params short[] data) => GetByteArr(null, data);

        public byte[] GetByteArr(object[] itms, params short[] data)
        {
            List<byte> bytes = new List<byte>(64);
            ABSaveSerializer serializer = null;
            int currentItmsPos = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] <= 255)
                    bytes.Add((byte)data[i]);
                else
                {
                    switch ((GenType)data[i])
                    {
                        case GenType.Numerical:
                            bytes.AddRange(BitConverter.GetBytes((dynamic)itms[currentItmsPos++]));
                            break;
                        case GenType.String:
                            bytes.AddRange(Encoding.UTF8.GetBytes((string)itms[currentItmsPos++]));
                            break;
                        case GenType.ByteArr:
                            bytes.AddRange((byte[])itms[currentItmsPos++]);
                            break;
                        case GenType.Action:

                            SetupSerializer();
                            ((Action<ABSaveSerializer>)itms[currentItmsPos++])(serializer);
                            bytes.AddRange(((MemoryStream)serializer.Output).ToArray());

                            break;
                        case GenType.MapItem:

                            SetupSerializer();

                            var genMap = (GenMap)itms[currentItmsPos++];
                            serializer.SerializeExactNonNullItem(genMap.Obj, genMap.Item);
                            bytes.AddRange(((MemoryStream)serializer.Output).ToArray());

                            break;
                        case GenType.Size:

                            SetupSerializer();
                            serializer.WriteCompressed((ulong)itms[currentItmsPos++]);

                            bytes.AddRange(((MemoryStream)serializer.Output).ToArray());
                            break;
                    }
                }
            }

            return bytes.ToArray();

            void SetupSerializer()
            {
                if (serializer == null)
                {
                    serializer = new ABSaveSerializer();
                    serializer.Initialize(new MemoryStream(), CurrentMap, null);
                }
                else
                {
                    serializer.Reset();
                    serializer.Output.Position = 0;
                } 
            }
        }

        public static byte[] Concat(byte first, params byte[] second)
        {
            var res = new byte[1 + second.Length];
            res[1] = first;
            second.CopyTo(res, 1);
            return res;
        }
    }

    public enum GenType : short
    {
        Numerical = 256,
        String = 257,
        ByteArr = 258,
        MapItem = 259,
        Action = 260,
        Size = 261
    }

    public struct GenMap
    {
        public object Obj;
        public MapItemInfo Item;

        public GenMap(object obj, MapItemInfo item) => (Obj, Item) = (obj, item);
    }
}
