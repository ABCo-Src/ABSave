using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Globalization;
using System.Reflection;

namespace ABSoftware.ABSave.Converters
{
    public class AssemblyConverter : ABSaveConverter
    {
        public readonly static AssemblyConverter Instance = new AssemblyConverter();
        private AssemblyConverter() { }

        public override bool AlsoConvertsNonExact => true;
        public override bool WritesToHeader => true;
        public override bool ConvertsSubTypes => true;

        public override Type[] ExactTypes { get; } = new Type[] { typeof(Assembly) };

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header) =>
            SerializeAssembly((Assembly)obj, ((Context)context).VersionMapItem, ref header);

        public void SerializeAssembly(Assembly assembly, MapItem versionMap, ref BitTarget header)
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
            header.Serializer.SerializeExactNonNullItem(assemblyName.Version, versionMap, ref header);

            // Name
            header.Serializer.WriteString(assemblyName.Name);

            // Culture
            if (!cultureNeutral) header.Serializer.WriteString(assemblyName.CultureName);

            // PublicKeyToken
            if (hasPublicKeyToken) header.Serializer.WriteBytes(publicKeyToken);
        }

        bool HandleSerializeSaved(Assembly assembly, ref BitTarget header)
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

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header) =>
            DeserializeAssembly(ref header, ((Context)context).VersionMapItem);

        public Assembly DeserializeAssembly(ref BitSource header, MapItem versionMap)
        {
            // Try to get a saved assembly.
            var saved = TryDeserializeSaved(ref header);
            if (saved != null) return saved;

            var assemblyName = new AssemblyName();

            var cultureNeutral = header.ReadBit();
            var hasPublicKeyToken = header.ReadBit();

            // Version
            assemblyName.Version = (Version)header.Deserializer.DeserializeExactNonNullItem(versionMap, ref header);

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

        Assembly TryDeserializeSaved(ref BitSource header)
        {
            var isSaved = header.ReadBit();

            if (isSaved)
            {
                var key = (int)header.Deserializer.ReadCompressedInt(ref header);
                return header.Deserializer.SavedAssemblies[key];
            }

            return null;
        }

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (type == typeof(Assembly) || type.IsSubclassOf(typeof(Assembly))) return new Context(map.GetMaptimeSubItem(typeof(Version)));
            return null;
        }

        class Context : IABSaveConverterContext 
        {
            public MapItem VersionMapItem;

            public Context(MapItem versionMapItem) => VersionMapItem = versionMapItem;
        }
    }
}
