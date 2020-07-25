using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace ABSoftware.ABSave.Serialization
{
    public class ABSaveStreamWriter : ABSaveWriter
    {
        public Stream Output;

        public ABSaveStreamWriter(Stream writeTo, ABSaveSettings settings) : base(settings) {
            if (!writeTo.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = writeTo;
        }

        #region Byte Writing
        public override void WriteByte(byte byt) => Output.WriteByte(byt);

        public override void WriteByteArray(byte[] arr, bool writeSize)
        {
            if (writeSize) WriteInt32((uint)arr.Length);
            Output.Write(arr, 0, arr.Length);
        }

        #endregion

        #region Short/Character Writing

        public override unsafe void FastWriteShorts(short* str, int strLength)
        {
            int byteCount = strLength * 2;
            WriteInt32((uint)byteCount);

            // Unfortunately, streams ONLY allow us to write arrays to write more than one byte quickly, so we have to allocate a byte array here.
            byte[] charData = new byte[byteCount];

            fixed (byte* dest = charData)
            {
                if (ShouldReverseEndian)
                {
                    byte* currentDestPos = dest;
                    byte* strData = (byte*)str;

                    for (int i = 0; i < strLength; i++)
                    {
                        *currentDestPos++ = strData[1];
                        *currentDestPos++ = strData[0];
                        strData += 2;
                    }
                }

                else Buffer.MemoryCopy(str, dest, byteCount, byteCount);
            }

            Output.Write(charData, 0, byteCount);
        }

        #endregion

        #region Numerical Writing

        public override unsafe void WriteInt16(ushort num)
        {
            byte* data = (byte*)&num;
            if (ShouldReverseEndian)
            {
                Output.WriteByte(data[1]);
                Output.WriteByte(data[0]);
            }
            else
            {
                Output.WriteByte(data[0]);
                Output.WriteByte(data[1]);
            }
        }

        public override unsafe void WriteInt32(uint num) => NumericalWriteBytes((byte*)&num, 4);
        public override unsafe void WriteInt64(ulong num) => NumericalWriteBytes((byte*)&num, 8);
        public override unsafe void WriteSingle(float num) => NumericalWriteBytes((byte*)&num, 4);
        public override unsafe void WriteDouble(double num) => NumericalWriteBytes((byte*)&num, 8);

        public override void WriteDecimal(decimal num)
        {
            var bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                WriteInt32((uint)bits[i]);
        }

        protected unsafe void NumericalWriteBytes(byte* data, int numberOfBytes)
        {
            byte[] byt = new byte[numberOfBytes];

            if (ShouldReverseEndian)
            {
                byte* currentDataPos = data + numberOfBytes;
                for (int i = 0; i < numberOfBytes; i++)
                    byt[i] = *--currentDataPos;
            } else {
                byte* currentDataPos = data;
                for (int i = 0; i < numberOfBytes; i++)
                    byt[i] = *currentDataPos++;
            }

            Output.Write(byt, 0, numberOfBytes);
        }

        public override unsafe void WriteInt32ToSignificantBytes(int s, int significantBytes)
        {
            byte* data = (byte*)&s;

            // L ++-- --++ B
            if (BitConverter.IsLittleEndian)
            {
                if (ShouldReverseEndian)
                {
                    byte* currentDataPos = data + significantBytes;
                    for (int i = 0; i < significantBytes; i++)
                        Output.WriteByte(*--currentDataPos);
                } 
                else 
                {
                    byte* currentDataPos = data;
                    for (int i = 0; i < significantBytes; i++)
                        Output.WriteByte(*currentDataPos++);
                }
            } 
            else 
            {
                if (ShouldReverseEndian)
                {
                    byte* currentDataPos = data + 3;
                    for (int i = 0; i < significantBytes; i++)
                        Output.WriteByte(*currentDataPos--);
                } 
                else 
                {
                    byte* currentDataPos = data + (4 - significantBytes);
                    for (int i = 0; i < significantBytes; i++)
                        Output.WriteByte(*currentDataPos++);
                }
            }
        }
        #endregion
    }
}
