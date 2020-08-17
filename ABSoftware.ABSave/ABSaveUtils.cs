using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    internal static class ABSaveUtils
    {
        internal const int UNSIGNED_24BIT_MAX = 16777215;

        #region Numerical

        internal static int GetRequiredNoOfBytesToStoreNumber(int num)
        {
            if (num <= byte.MaxValue) return 1;
            else if (num <= ushort.MaxValue) return 2;
            else if (num <= UNSIGNED_24BIT_MAX) return 3;
            else return 4;
        }

        #endregion

        #region Type Convertion

        internal static bool TryFindConverterForType(ABSaveSettings settings, Type type, out ABSaveTypeConverter converter)
        {
            if (settings.ExactConverters.TryGetValue(type, out ABSaveTypeConverter val))
            {
                converter = val;
                return true;
            }
            else
                for (int i = settings.NonExactConverters.Count - 1; i >= 0; i--)
                    if (settings.NonExactConverters[i].CheckCanConvertType(type))
                    {
                        converter = settings.NonExactConverters[i];
                        return true;
                    }

            converter = null;
            return false;
        }

        #endregion

        public static bool HasGenericInterface(Type[] interfaces, Type theInterface)
        {
            for (int i = 0; i < interfaces.Length; i++)
                if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == theInterface)
                    return true;

            return false;
        }

        public static bool HasInterface(Type toCheck, Type theInterface)
        {
            var interfaces = toCheck.GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
                if (interfaces[i] == theInterface)
                    return true;

            return false;
        }
    }
}
