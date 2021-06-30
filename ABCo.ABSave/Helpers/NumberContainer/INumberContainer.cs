using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Helpers.NumberContainer
{
    /// <summary>
    /// An interface used alongside generics to allow passing both "int" and "long" to a method.
    /// All the overhead you'd think would be incurred from this is optimized away due to the 
    /// method being compiled by the JIT specifically for each generic parameter (int and long).
    /// </summary>
    public interface INumberContainer
    {
        public byte ToByte();
        public int ToInt32();
        public long ToInt64();
        public bool LessThan(int num);
        public bool LessThanLong(long num);
        public int ShiftRight(byte amount);
    }
}
