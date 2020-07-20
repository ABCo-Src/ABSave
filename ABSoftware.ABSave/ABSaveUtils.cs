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

        public static int GetRequiredNoOfBytesToStoreNumber(int num)
        {
            if (num <= byte.MaxValue) return 1;
            else if (num <= ushort.MaxValue) return 2;
            else if (num <= UNSIGNED_24BIT_MAX) return 3;
            else return 4;
        }
        #endregion
    }
}
