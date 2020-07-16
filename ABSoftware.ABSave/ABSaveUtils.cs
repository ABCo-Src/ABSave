using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    internal class ABSaveUtils
    {
        internal const int UNSIGNED_24BIT_MAX = 16777215;

        #region Cached Types

        internal static bool SearchForCachedType(ABSaveWriter writer, Type type, out byte[] key) => writer.CachedTypes.TryGetValue(type, out key);
        internal static bool SearchForCachedAssembly(ABSaveWriter writer, Assembly assembly, out byte key) => writer.CachedAssemblies.TryGetValue(assembly, out key);

        internal static byte CreateCachedAssembly(Assembly assembly, ABSaveWriter writer)
        {
            var writtenKey = (byte)writer.CachedAssemblies.Count;
            writer.CachedAssemblies.Add(assembly, writtenKey);
            return writtenKey;
        }

        internal static byte[] CreateCachedType(Type typ, ABSaveWriter writer)
        {
            var writtenKey = GetShortBytes((short)writer.CachedTypes.Count, writer.Settings);
            writer.CachedTypes.Add(typ, writtenKey);

            return writtenKey;
        }

        #endregion

        #region Helpers

        public static byte VersionNumberOfDecimals(Version ver)
        {
            if (ver.MinorRevision > 0) return 4;
            else if (ver.Minor > 0) return 3;
            else if (ver.MajorRevision > 0) return 2;
            else if (ver.Major > 0) return 1;
            return 0;
        }

        #endregion

        #region Numerical

        static int GetRequiredNumberOfBytes(ABSaveWriter writer)
        {
            if (writer.CachedAssemblies.Count <= byte.MaxValue) return 1;
            else if (writer.CachedAssemblies.Count <= ushort.MaxValue) return 2;
            else if (writer.CachedAssemblies.Count <= UNSIGNED_24BIT_MAX) return 3;
            else return 4;
        }
        #endregion
    }
}
