using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
{
    public class AssemblyTypeConverter : ABSaveTypeConverter
    {
        public static AssemblyTypeConverter Instance = new AssemblyTypeConverter();
        private AssemblyTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(Assembly);

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            var assembly = (Assembly)obj;

            var successful = writer.CachedAssemblies.TryGetValue(assembly, out int key);

            if (writer.Settings.CacheTypesAndAssemblies && HandleKeyBeforeAssembly(assembly, writer, successful, key)) return;

            var assemblyName = assembly.GetName();
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var neutralAssemblyCulture = assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture);
            var hasPublicKeyToken = publicKeyToken != null;

            // First Byte: 000000(1 for non-neutral culture)(1 for PublicKeyToken)
            var firstByte = (neutralAssemblyCulture ? 2 : 0) | (hasPublicKeyToken ? 1 : 0);
            writer.WriteByte((byte)firstByte);
            writer.WriteText(assemblyName.Name);
            VersionTypeConverter.Instance.Serialize(assemblyName.Version, new TypeInformation(), writer);

            if (!neutralAssemblyCulture)
                writer.WriteText(assemblyName.CultureName);
            if (hasPublicKeyToken)
                writer.WriteByteArray(publicKeyToken, false);
        }

        bool HandleKeyBeforeAssembly(Assembly assembly, ABSaveWriter writer, bool successful, int key)
        {
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
                writer.WriteInt32ToSignificantBytes(size, ABSaveUtils.GetRequiredNoOfBytesToStoreNumber(writer.CachedAssemblies.Count - 1));
            }

            return false;
        }
    }
}
