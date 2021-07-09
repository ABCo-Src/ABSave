using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Helpers.NumberContainer
{
    public struct Int64Container : INumberContainer
    {
        public long Number;

        public Int64Container(long number) => Number = number;
        public byte ToByte() => (byte)Number;
        public int ToInt32() => (int)Number;
        public long ToInt64() => Number;
        public bool LessThan(int num) => Number < num;
        public bool LessThanLong(long num) => Number < num;
        public int ShiftRight(byte amount) => (int)(Number >> amount);
    }
}
