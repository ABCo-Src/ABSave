namespace ABCo.ABSave.Deserialization
{
    // ===============================================
    // NOTE: Full details about the "compressed numerical" structure can be seen in the TXT file: CompressedPlan.txt in the project root
    // ===============================================
    // This is just an implementation of everything shown there.
    public sealed partial class ABSaveDeserializer
    {
        public uint ReadCompressedInt() => GetHeader().ReadCompressedInt();
        public ulong ReadCompressedLong() => GetHeader().ReadCompressedLong();
    }
}
