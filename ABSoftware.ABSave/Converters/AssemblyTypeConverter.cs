using System;
using System.Globalization;
using System.Reflection;

namespace ABSoftware.ABSave.Converters
{
    public class AssemblyTypeConverter : ABSaveTypeConverter
    {
        internal static readonly Type RuntimeAssembly = typeof(string).Assembly.GetType("System.RuntimeAssembly") ?? typeof(Assembly); // Fallback to harmless "Assembly"

        public readonly static AssemblyTypeConverter Instance = new AssemblyTypeConverter();
        private AssemblyTypeConverter() { }

        public override bool HasNonExactTypes => true;

        public override Type[] ExactTypes { get; } = new Type[] { RuntimeAssembly };

        public override bool CheckCanConvertNonExact(Type type) => type.IsSubclassOf(typeof(Assembly));

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var assembly = (Assembly)obj;

            if (writer.Settings.CacheTypesAndAssemblies && SerializeKeyBeforeAssembly(assembly, writer)) return;

            var assemblyName = assembly.GetName();
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var cultureNeutral = assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture);
            var hasPublicKeyToken = publicKeyToken != null;

            var firstByte = (cultureNeutral ? 2 : 0) | (hasPublicKeyToken ? 1 : 0);
            writer.WriteByte((byte)firstByte);
            writer.WriteString(assemblyName.Name);
            VersionTypeConverter.Instance.Serialize(assemblyName.Version, typeof(Version), writer);

            if (!cultureNeutral)
                writer.WriteString(assemblyName.CultureName);
            if (hasPublicKeyToken)
                writer.WriteByteArray(publicKeyToken, false);
        }

        bool SerializeKeyBeforeAssembly(Assembly assembly, ABSaveWriter writer)
        {
            var successful = writer.CachedAssemblies.TryGetValue(assembly, out int key);

            // TODO: Optimize this in some way?
            if (successful)
            {
                writer.WriteLittleEndianInt32((uint)key, ABSaveUtils.GetRequiredNoOfBytesToStoreNumber(writer.CachedAssemblies.Count - 1));
                return true;
            }
            else if (writer.CachedAssemblies.Count == int.MaxValue)
                writer.WriteInt32(uint.MaxValue);
            else
            {
                int size = writer.CachedAssemblies.Count;
                writer.CachedAssemblies.Add(assembly, size);
                writer.WriteLittleEndianInt32((uint)size, ABSaveUtils.GetRequiredNoOfBytesToStoreNumber(size));
            }

            return false;
        }

        public override object Deserialize(Type type, ABSaveReader reader)
        {
            uint key = 0;

            // Handle the key.
            if (reader.Settings.CacheTypesAndAssemblies)
            {
                key = reader.ReadLittleEndianInt32(ABSaveUtils.GetRequiredNoOfBytesToStoreNumber(reader.CachedAssemblies.Count));
                if (key < reader.CachedAssemblies.Count) return reader.CachedAssemblies[(int)key];
            }

            var assemblyName = new AssemblyName();

            var firstByte = reader.ReadByte();
            var cultureNeutral = (firstByte & 2) > 0;
            var hasPublicKeyToken = (firstByte & 1) > 0;

            assemblyName.Name = reader.ReadString();
            assemblyName.Version = (Version)VersionTypeConverter.Instance.Deserialize(typeof(Version), reader);
            assemblyName.CultureInfo = cultureNeutral ? CultureInfo.InvariantCulture : new CultureInfo(reader.ReadString());
           
            if (hasPublicKeyToken)
            {
                var publicKeyToken = new byte[8];
                reader.ReadBytes(publicKeyToken);
                assemblyName.SetPublicKeyToken(publicKeyToken);
            }

            var assembly = Assembly.Load(assemblyName);
            if (key != uint.MaxValue) reader.CachedAssemblies.Add(assembly);

            return assembly;
        }
    }
}
