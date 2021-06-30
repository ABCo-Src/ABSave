using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Helpers.NumberContainer
{
    public struct Int32Container : INumberContainer
    {
        public int Number;

        public Int32Container(int number) => Number = number;
        public byte ToByte() => (byte)Number;
        public int ToInt32() => Number;
        public long ToInt64() => Number;
        public bool LessThan(int num) => Number < num;
        public bool LessThanLong(long num) => Number < num;
        public int ShiftRight(byte amount) => Number >> amount;
    }
}
