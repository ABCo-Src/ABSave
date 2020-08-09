using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Globalization;
using System.Reflection;

namespace ABSoftware.ABSave.Converters
{
    public class AssemblyTypeConverter : ABSaveTypeConverter
    {
        public static AssemblyTypeConverter Instance = new AssemblyTypeConverter();
        private AssemblyTypeConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(TypeInformation typeInformation) => typeInformation.ActualType == typeof(Assembly) || typeInformation.ActualType.IsSubclassOf(typeof(Assembly));

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            var assembly = (Assembly)obj;

            if (writer.Settings.CacheTypesAndAssemblies && SerializeKeyBeforeAssembly(assembly, writer)) return;

            var assemblyName = assembly.GetName();
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var cultureNeutral = assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture);
            var hasPublicKeyToken = publicKeyToken != null;

            var firstByte = (cultureNeutral ? 2 : 0) | (hasPublicKeyToken ? 1 : 0);
            writer.WriteByte((byte)firstByte);
            writer.WriteText(assemblyName.Name);
            VersionTypeConverter.Instance.Serialize(assemblyName.Version, new TypeInformation(), writer);

            if (!cultureNeutral)
                writer.WriteText(assemblyName.CultureName);
            if (hasPublicKeyToken)
                writer.WriteByteArray(publicKeyToken, false);
        }

        bool SerializeKeyBeforeAssembly(Assembly assembly, ABSaveWriter writer)
        {
            var successful = writer.CachedAssemblies.TryGetValue(assembly, out int key);
            if (successful)
            {
                writer.WriteInt32((uint)key);
                return true;
            }
            else if (writer.CachedAssemblies.Count == int.MaxValue)
                writer.WriteInt32(uint.MaxValue);
            else
            {
                int size = writer.CachedAssemblies.Count;
                writer.CachedAssemblies.Add(assembly, size);
                writer.WriteLittleEndianInt32(size, ABSaveUtils.GetRequiredNoOfBytesToStoreNumber(size));
            }

            return false;
        }

        public override object Deserialize(TypeInformation typeInfo, ABSaveReader reader)
        {
            uint key = 0;

            // Handle the key.
            if (reader.Settings.CacheTypesAndAssemblies)
            {
                key = reader.ReadLittleEndianInt32(ABSaveUtils.GetRequiredNoOfBytesToStoreNumber(reader.CachedAssemblies.Count - 1));
                if (key < reader.CachedAssemblies.Count) return reader.CachedAssemblies[(int)key];
            }

            var assemblyName = new AssemblyName();

            var firstByte = reader.ReadByte();
            var cultureNeutral = (firstByte & 2) > 0;
            var hasPublicKeyToken = (firstByte & 1) > 0;

            assemblyName.Name = reader.ReadString();
            assemblyName.Version = (Version)VersionTypeConverter.Instance.Deserialize(new TypeInformation(), reader);
            assemblyName.CultureInfo = cultureNeutral ? CultureInfo.InvariantCulture : new CultureInfo(reader.ReadString());
           
            if (hasPublicKeyToken)
            {
                byte[] publicKeyToken = new byte[8];
                reader.ReadBytes(publicKeyToken);
                assemblyName.SetPublicKeyToken(publicKeyToken);
            }

            var assembly = Assembly.Load(assemblyName);
            if (key != 0xFFFFFFFF) reader.CachedAssemblies.Add(assembly);

            return assembly;
        }
    }
}
