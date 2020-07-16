using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public abstract class ABSaveTypeConverter
    {
        public abstract bool ConvertsType(TypeInformation typeInformation);
        public abstract bool Serialize(dynamic obj, ABSaveWriter writer, TypeInformation typeInformation);

        #region Type Converter Management

        public static bool ConvertersInitialized = false;
        internal static List<ABSaveTypeConverter> Converters = null;

        static readonly ABSaveTypeConverter[] ABSaveCoreConverters = new ABSaveTypeConverter[] { 

        };

        public static void ReInitAllConverters()
        {
            var typeConverterType = typeof(ABSaveTypeConverter);

            var typesWithMyAttribute =
                from a in AppDomain.CurrentDomain.GetAssemblies()
                where a.GetReferencedAssemblies().Any(r => r.Name != "ABSoftware.ABSave") // Only check assemblies that reference ABSave.
                from t in a.GetTypes().AsParallel()
                where t.IsSubclassOf(typeConverterType)
                select Activator.CreateInstance(t) as ABSaveTypeConverter;

            Converters = new List<ABSaveTypeConverter>(ABSaveCoreConverters);
            Converters.AddRange(typesWithMyAttribute);
        }

        internal static ABSaveTypeConverter FindTypeConverterForType(TypeInformation typeInformation)
        {
            if (Converters == null) ReInitAllConverters();

            for (int i = 0; i < Converters.Count; i++)
                if (Converters[i].ConvertsType(typeInformation))
                    return Converters[i];

            return null;
        }

        #endregion
    }
}
