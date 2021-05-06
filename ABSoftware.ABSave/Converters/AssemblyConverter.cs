using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Globalization;
using System.Reflection;

namespace ABSoftware.ABSave.Converters
{
    public class AssemblyConverter : Converter
    {
        public readonly static AssemblyConverter Instance = new AssemblyConverter();
        private AssemblyConverter() { }

        public override bool AlsoConvertsNonExact => true;
        public override bool WritesToHeader => true;
        public override bool ConvertsSubTypes => true;

        public override Type[] ExactTypes { get; } = new Type[] { typeof(Assembly) };

        public override void Serialize(object obj, Type actualType, IConverterContext context, ref BitTarget header) =>
            SerializeAssembly((Assembly)obj, ref header);

        public static void SerializeAssembly(Assembly assembly, ref BitTarget header)
        {
            // Try to serialize an already saved version.
            if (HandleSerializeSaved(assembly, ref header)) return;

            var assemblyName = assembly.GetName();
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var cultureNeutral = assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture);
            var hasPublicKeyToken = publicKeyToken != null;

            header.WriteBitWith(cultureNeutral);
            header.WriteBitWith(hasPublicKeyToken);

            // Version
            header.Serializer.SerializeExactNonNullItem(assemblyName.Version, header.Serializer.Map.VersionItem, ref header);

            // Name
            header.Serializer.WriteString(assemblyName.Name);

            // Culture
            if (!cultureNeutral) header.Serializer.WriteString(assemblyName.CultureName);

            // PublicKeyToken
            if (hasPublicKeyToken) header.Serializer.WriteBytes(publicKeyToken);
        }

        static bool HandleSerializeSaved(Assembly assembly, ref BitTarget header)
        {
            var isSaved = header.Serializer.SavedAssemblies.TryGetValue(assembly, out int key);

            if (isSaved)
            {
                header.WriteBitOn();
                header.Serializer.WriteCompressed((uint)key, ref header);
                return true;
            }

            header.WriteBitOff();
            header.Serializer.SavedAssemblies.Add(assembly, header.Serializer.SavedAssemblies.Count);
            return false;
        }

        public override object Deserialize(Type actualType, IConverterContext context, ref BitSource header) =>
            DeserializeAssembly(ref header);

        public static Assembly DeserializeAssembly(ref BitSource header)
        {
            // Try to get a saved assembly.
            var saved = TryDeserializeSaved(ref header);
            if (saved != null) return saved;

            var assemblyName = new AssemblyName();

            var cultureNeutral = header.ReadBit();
            var hasPublicKeyToken = header.ReadBit();

            // Version
            assemblyName.Version = (Version)header.Deserializer.DeserializeExactNonNullItem(header.Deserializer.Map.VersionItem, ref header);

            // Name
            assemblyName.Name = header.Deserializer.ReadString();

            // Culture
            assemblyName.CultureInfo = cultureNeutral ? CultureInfo.InvariantCulture : new CultureInfo(header.Deserializer.ReadString());

            // PublicKeyToken
            if (hasPublicKeyToken)
            {
                var publicKeyToken = ABSaveUtils.CreateUninitializedArray<byte>(8);
                header.Deserializer.ReadBytes(publicKeyToken);
                assemblyName.SetPublicKeyToken(publicKeyToken);
            }

            var assembly = Assembly.Load(assemblyName);
            header.Deserializer.SavedAssemblies.Add(assembly);
            return assembly;
        }

        static Assembly TryDeserializeSaved(ref BitSource header)
        {
            var isSaved = header.ReadBit();

            if (isSaved)
            {
                var key = (int)header.Deserializer.ReadCompressedInt(ref header);
                return header.Deserializer.SavedAssemblies[key];
            }

            return null;
        }

        public override IConverterContext TryGenerateContext(ref ContextGen gen)
        {
            if (gen.Type == typeof(Assembly) || gen.Type.IsSubclassOf(typeof(Assembly))) gen.MarkCanConvert();

            return null;
        }
    }
}
