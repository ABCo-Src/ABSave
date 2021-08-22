using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    public abstract class TestBase
    {
        public ABSaveMap CurrentMap;
        public MapItemInfo CurrentMapItem;

        public MemoryStream Stream;
        public ABSaveSerializer Serializer;
        public ABSaveDeserializer Deserializer;

        public void Initialize(Dictionary<Type, uint> targetVersions = null) => Initialize(ABSaveSettings.ForSpeed, targetVersions);
        public void Initialize(ABSaveSettings template, Dictionary<Type, uint> targetVersions = null, bool lazyWriteCompressed = false)
        {
            var settings = template.Customize(b => b
                .SetLazyWriteCompressed(lazyWriteCompressed)
                .SetIncludeVersioningHeader(false)
                .AddConverter<BaseTypeConverter>()
                .AddConverter<SubTypeConverter>()
                .AddConverter<OtherTypeConverter>()
            );

            CurrentMap = ABSaveMap.Get<EmptyClass>(settings);

            Stream = new MemoryStream();
            Serializer = CurrentMap.GetSerializer(Stream, true, targetVersions);
            Deserializer = CurrentMap.GetDeserializer(Stream, true);
        }

        public void GoToStart() => Stream.Position = 0;

        public void ResetStateWithMapFor<T>() => ResetStateWithMapFor(typeof(T));

        public void ResetStateWithMapFor(Type type)
        {
            ResetState();

            var gen = CurrentMap.GetGenerator();
            CurrentMapItem = gen.GetMap(type);
            ABSaveMap.ReleaseGenerator(gen);
        }

        public void ResetState()
        {
            // Reset the serializer and deserializer
            Serializer.Reset();
            Deserializer.Reset();

            ClearStream();
        }

        public void ClearStream()
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
            Serializer.Flush();

            var actual = Stream.ToArray();
            var matches = expected.SequenceEqual(actual);

            if (!matches)
            {
                var expectedStr = BitConverter.ToString(expected);
                var actualStr = BitConverter.ToString(actual);

                throw new Exception($"Non-matching assert!\nExpected: {expectedStr}\nActual:   {actualStr}");
            }
        }

        public byte[] GetByteArr(params short[] data) => GetByteArr(null, data);

        public byte[] GetByteArr(object[] itms, params short[] data)
        {
            List<byte> bytes = new(64);
            ABSaveSerializer serializer = null;
            int currentItmsPos = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] <= 255)
                {
                    bytes.Add((byte)data[i]);
                }
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
                            bytes.AddRange(((MemoryStream)serializer.GetStream()).ToArray());

                            break;
                        case GenType.MapItem:

                            SetupSerializer();

                            var genMap = (GenMap)itms[currentItmsPos++];
                            serializer.WriteExactNonNullItem(genMap.Obj, genMap.Item);
                            bytes.AddRange(((MemoryStream)serializer.GetStream()).ToArray());

                            break;
                        case GenType.Size:

                            SetupSerializer();
                            serializer.WriteCompressedLong((ulong)itms[currentItmsPos++]);

                            bytes.AddRange(((MemoryStream)serializer.GetStream()).ToArray());
                            break;
                    }
                }
            }

            return bytes.ToArray();

            void SetupSerializer()
            {
                if (serializer == null)
                {
                    serializer = CurrentMap.GetSerializer(new MemoryStream(), true);
                }
                else
                {
                    serializer.Reset();
                    serializer.GetStream().Position = 0;
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

        public static void ReflectiveAssert(object expected, object actual)
        {
            if (actual == null) throw new Exception("Objects are not equal! The actual is null.");

            var expectedType = expected.GetType();
            var actualType = actual.GetType();

            if (expectedType != actualType)
                throw new Exception($"Objects not equal! Types do not match! Expected type: {expectedType}, Actual type: {actualType}");

            var props = expectedType.GetProperties();

            for (int i = 0; i < props.Length; i++)
            {
                var expectedPropValue = props[i].GetValue(expected);
                var actualPropValue = props[i].GetValue(actual);

                if (expectedPropValue == null)
                {
                    if (actualPropValue != null)
                        throw new Exception($"The property {props[i].Name} does not match! Expected it to be null but was {actualPropValue}");

                    continue;
                }

                if (!expectedPropValue.Equals(actualPropValue))
                    throw new Exception($"The property {props[i].Name} does not match! Expected: {expectedPropValue} \n Actual: {actualPropValue}");
            }
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
