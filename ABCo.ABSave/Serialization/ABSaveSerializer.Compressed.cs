using ABCo.ABSave.Helpers.NumberContainer;
using System;

namespace ABCo.ABSave.Serialization
{
    // ===============================================
    // NOTE: Full details about the "compressed numerical" structure can be seen in the TXT file: CompressedPlan.txt in the project root
    // ===============================================
    // This is just an implementation of everything shown there.
    public sealed partial class ABSaveSerializer
    {
        public void WriteCompressedInt(uint data)
        {
            using var writer = GetHeader();
            writer.WriteCompressedInt(data);
        }

        public void WriteCompressedLong(ulong data) 
        {
            using var writer = GetHeader();
            writer.WriteCompressedLong(data);
        }
    }
}
