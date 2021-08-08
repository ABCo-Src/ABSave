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
            var target = new BitTarget(this);
            CompressedSerializer.WriteCompressedInt(data, ref target);
        }

        public void WriteCompressedInt(uint data, ref BitTarget target) => CompressedSerializer.WriteCompressedInt(data, ref target);

        public void WriteCompressedLong(ulong data)
        {
            var target = new BitTarget(this);
            CompressedSerializer.WriteCompressedLong(data, ref target);
        }

        public void WriteCompressedLong(ulong data, ref BitTarget target) => CompressedSerializer.WriteCompressedLong(data, ref target);
    }
}
