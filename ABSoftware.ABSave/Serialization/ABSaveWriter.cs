using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace ABSoftware.ABSave.Serialization
{
    internal class ABSaveWriterDataChunk
    {
        internal byte[] Data;
        internal int BytesFilled;
        internal ABSaveWriterDataChunk Next;

        internal ABSaveWriterDataChunk(int size)
        {
            Data = new byte[size];
            BytesFilled = 0;
            Next = null;
        }

        internal ABSaveWriterDataChunk(byte[] data)
        {
            Data = data;
            BytesFilled = data.Length;
            Next = null;
        }
    }

    /// <summary>
    /// The central part of ABSave serialization, this contains information about the state of the ABSave document and the actual serialized data of the document.
    /// </summary>
    public class ABSaveWriter
    {
        // If a new chunk is allocated automatically (NOT writing an uncopied byte array), how big that chunk should be in addition to the data that's initially being added.
        const int AUTO_MIN_CHUNK_SIZE = 128;

        readonly ABSaveWriterDataChunk DataStart;
        ABSaveWriterDataChunk CurrentChunk;
        ABSaveWriterDataChunk DataEnd;
        int FreeSpace;
        int TotalBytesFilled = 0;

        internal readonly ABSaveSettings Settings;
        internal Dictionary<Assembly, byte[]> CachedAssemblies = new Dictionary<Assembly, byte[]>();
        internal Dictionary<Type, byte[]> CachedTypes = new Dictionary<Type, byte[]>();

        // Whether number's or character's endian should be reversed to match the target ABSave.
        public readonly bool ShouldReverseEndian;

        public ABSaveWriter(ABSaveSettings settings)
        {
            DataStart = CurrentChunk = DataEnd = new ABSaveWriterDataChunk(AUTO_MIN_CHUNK_SIZE);
            FreeSpace = AUTO_MIN_CHUNK_SIZE;

            Settings = settings;
            ShouldReverseEndian = settings.UseLittleEndian != BitConverter.IsLittleEndian;
        }

        #region Core Data Chunk

        public void EnsureCanFit(int numberOfBytesRequired)
        {
            if (numberOfBytesRequired > FreeSpace)
                ScaleUp(numberOfBytesRequired - FreeSpace);
        }

        void ScaleUp(int required)
        {
            int newSize = required + AUTO_MIN_CHUNK_SIZE;
            DataEnd = DataEnd.Next = new ABSaveWriterDataChunk(newSize);
            FreeSpace += newSize;
        }

        void MoveToNextChunk() => CurrentChunk = CurrentChunk.Next;

        #endregion

        #region Byte Writing
        // Unchecked means it doesn't check whether there's any free space.
        public void WriteByte(byte byt)
        {
            EnsureCanFit(1);
            UncheckedWriteByte(byt);
        }

        public void UncheckedWriteByte(byte byt)
        {
            if (CurrentChunk.BytesFilled == CurrentChunk.Data.Length) MoveToNextChunk();
            CurrentChunk.Data[CurrentChunk.BytesFilled++] = byt;
            FreeSpace--;
            TotalBytesFilled++;
        }

        public void WriteByteArray(byte[] arr, bool writeSize)
        {
            // If the array is bigger than 20 (if we can fit the length in) or if it's bigger than 44 (if we can't fit the length in), 
            // we'll just add a new chunk with it instead of copying the data in. Otherwise, we'll copy across the data.
            // NOTE: If we aren't writing the size, then being bigger than 20 bytes is good enough reason to add a new chunk instead of copying.
            if (arr.Length > 20 && (!writeSize || FreeSpace > 4 || arr.Length > 44))
            {
                // If we can't fit the length in, allocate a tiny little chunk specifically to fit it in right before the array.
                if (writeSize)
                {
                    if (FreeSpace < 4)
                        DataEnd = DataEnd.Next = new ABSaveWriterDataChunk(4 - FreeSpace);

                    UncheckedWriteInt32((uint)arr.Length);
                }

                CurrentChunk = DataEnd = DataEnd.Next = new ABSaveWriterDataChunk(arr);
                TotalBytesFilled += arr.Length;
                FreeSpace = 0;
            }
            
            else WriteAndCopyByteArray(arr, 0, arr.Length, writeSize);
        }

        public void WriteAndCopyByteArray(byte[] arr, int start, int length, bool writeSize)
        {
            EnsureCanFit(length + (writeSize ? 4 : 0));
            UncheckedWriteAndCopyByteArray(arr, start, length, writeSize);
        }

        public void UncheckedWriteAndCopyByteArray(byte[] arr, int start, int length, bool writeSize)
        {
            if (CurrentChunk.BytesFilled == CurrentChunk.Data.Length) MoveToNextChunk();

            if (writeSize)
                UncheckedWriteInt32((uint)arr.Length + 1);

            int currentPos = start;
            int remainingData = length;

            // Copy as much as we can into the current chunk.
            CopyByteArrayBlock(arr, ref currentPos, ref remainingData, CurrentChunk.Data.Length - CurrentChunk.BytesFilled);

            // If we weren't able to copy everything into this chunk, keep copying into the next chunks until we've copied everything.
            while (currentPos < length)
            {
                MoveToNextChunk();
                CopyByteArrayBlock(arr, ref currentPos, ref remainingData, CurrentChunk.Data.Length);
            }

            FreeSpace -= length;
            TotalBytesFilled += length;
        }

        void CopyByteArrayBlock(byte[] arr, ref int start, ref int remainingData, int currentChunkFreeSpace)
        {
            int canCopy = Math.Min(remainingData, currentChunkFreeSpace);
            Buffer.BlockCopy(arr, start, CurrentChunk.Data, CurrentChunk.BytesFilled, canCopy);

            CurrentChunk.BytesFilled += canCopy;

            start += canCopy;
            remainingData -= canCopy;
        }

        #endregion

        #region Text Writing

        unsafe void WriteRawCharacters(char* str, int strLength)
        {
            // Characters are two bytes big, and there is a 32-bit integer for the length at the beginning.
            int byteSize = strLength * 2;
            EnsureCanFit(byteSize + 4);

            if (CurrentChunk.BytesFilled == CurrentChunk.Data.Length) MoveToNextChunk();

            UncheckedWriteInt32((uint)byteSize);

            byte* strData = (byte*)str;
            bool copyingSecondByteOfChar = false;

            int remainingBytes = byteSize;

            byte charSecondByte = 0;

            // Characters are two bytes big. When we encounter a character we put the second byte into a buffer, that way
            // if the character needs to written across two chunks, we can write the second byte when we move over to the next chunk.
            // Write as much as we can to the current chunk.
            strData = CopyBlockOfChars(strData, &charSecondByte, ref copyingSecondByteOfChar, ref remainingBytes);

            // If we weren't able to copy everything into this chunk, keep copying into the next chunks until we've copied everything.
            while (remainingBytes > 0)
            {
                MoveToNextChunk();
                strData = CopyBlockOfChars(strData, &charSecondByte, ref copyingSecondByteOfChar, ref remainingBytes);
            }

            FreeSpace -= byteSize;
            TotalBytesFilled += byteSize;
        }

        unsafe byte* CopyBlockOfChars(byte* strData, byte* charSecondByte, ref bool copyingSecondByteOfChar, ref int remainingBytes)
        {
            var bytesToCopy = Math.Min(remainingBytes, CurrentChunk.Data.Length);

            for (int i = 0; i < bytesToCopy; i++)
            {
                if (copyingSecondByteOfChar)
                {
                    CurrentChunk.Data[CurrentChunk.BytesFilled++] = *charSecondByte;
                    copyingSecondByteOfChar = false;
                } else {
                    CurrentChunk.Data[CurrentChunk.BytesFilled++] = WriteCorrectEndianCharBytes(strData, charSecondByte);
                    strData += 2;
                    
                    copyingSecondByteOfChar = true;
                }

                remainingBytes--;
            }

            return strData;
        }

        /// <returns>The first byte</returns>
        unsafe byte WriteCorrectEndianCharBytes(byte* charIn, byte* secondByteOut)
        {
            if (ShouldReverseEndian)
            {
                *secondByteOut = charIn[0];
                return charIn[1];
            } else {
                *secondByteOut = charIn[1];
                return charIn[0];
            }
        }

        private unsafe void WriteEscapeChar(byte* currentCharBuffer)
        {
            if (Settings.UseLittleEndian)
            {
                CurrentChunk.Data[CurrentChunk.BytesFilled] = 92;
                currentCharBuffer[0] = 0;
            } else {
                CurrentChunk.Data[CurrentChunk.BytesFilled] = 0;
                currentCharBuffer[0] = 92;
            }
        }

        public unsafe void WriteText(string str)
        {
            fixed (char* s = str)
                WriteRawCharacters(s, str.Length);
        }

        public unsafe void WriteText(char[] chArr)
        {
            fixed (char* s = chArr)
                WriteRawCharacters(s, chArr.Length);
        }

        public void WriteText(StringBuilder str)
        {
            char[] builderContents = new char[str.Length];
            str.CopyTo(0, builderContents, 0, str.Length);
            WriteText(builderContents);
        }

        #endregion

        #region ABSave Controls

        //public void WriteNull() => WriteByte(2);
        //public void WriteItemEnd() => WriteByte(1);
        public void WriteNullAttribute() => WriteByte(1);
        public void WriteMatchingTypeAttribute() => WriteByte(2);
        public void WriteDifferentTypeAttribute() => WriteByte(3);
        //public void WriteDictionaryStart() => WriteByte(6);

        #endregion

        #region Numerical Writing

        public unsafe void WriteNumber(object num, TypeCode tCode)
        {
            switch (tCode)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:

                    // Just return the byte.
                    WriteByte((byte)num);
                    break;

                case TypeCode.UInt16:
                case TypeCode.Int16:

                    WriteInt16((ushort)num);
                    break;

                case TypeCode.UInt32:
                case TypeCode.Int32:

                    WriteInt32((uint)num);
                    break;

                case TypeCode.UInt64:
                case TypeCode.Int64:

                    WriteInt64((ulong)num);
                    break;

                case TypeCode.Single:

                    WriteSingle((float)num);
                    break;

                case TypeCode.Double:

                    WriteDouble((float)num);
                    break;

                case TypeCode.Decimal:

                    WriteDecimal(num);
                    break;
            }
        }

        public unsafe void WriteInt16(ushort num)
        {
            EnsureCanFit(2);
            UncheckedWriteInt16(num);
        }

        public unsafe void UncheckedWriteInt16(ushort num)
        {
            byte* data = (byte*)&num;
            if (ShouldReverseEndian) {
                UncheckedWriteByte(data[1]);
                UncheckedWriteByte(data[0]);
            } else {
                UncheckedWriteByte(data[0]);
                UncheckedWriteByte(data[1]);
            }
        }

        public unsafe void WriteInt32(uint num)
        {
            EnsureCanFit(4);
            UncheckedWriteInt32(num);
        }

        public unsafe void UncheckedWriteInt32(uint num) => NumericalWriteBytes((byte*)&num, 4);

        public unsafe void WriteInt64(ulong num)
        {
            EnsureCanFit(8);
            UncheckedWriteInt64(num);
        }

        public unsafe void UncheckedWriteInt64(ulong num) => NumericalWriteBytes((byte*)&num, 8);

        public unsafe void WriteSingle(float num)
        {
            EnsureCanFit(4);
            UncheckedWriteSingle(num);
        }

        public unsafe void UncheckedWriteSingle(float num) => NumericalWriteBytes((byte*)&num, 4);

        public unsafe void WriteDouble(double num)
        {
            EnsureCanFit(8);
            UncheckedWriteDouble(num);
        }

        public unsafe void UncheckedWriteDouble(double num) => NumericalWriteBytes((byte*)&num, 8);

        public void WriteDecimal(dynamic num)
        {
            EnsureCanFit(16);

            var bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                UncheckedWriteInt32((uint)bits[i]);
        }

        unsafe void NumericalWriteBytes(byte* data, int numberOfBytes)
        {
            int remainingBytes = numberOfBytes;
            byte* currentDataPos = ShouldReverseEndian ? data + numberOfBytes - 1 : data;

            currentDataPos = NumericalWriteBlock(currentDataPos, ref remainingBytes, CurrentChunk.Data.Length - CurrentChunk.BytesFilled);

            while (remainingBytes > 0)
            {
                MoveToNextChunk();
                NumericalWriteBlock(currentDataPos, ref remainingBytes, CurrentChunk.Data.Length);
            }

            FreeSpace -= numberOfBytes;
            TotalBytesFilled += numberOfBytes;
        }

        unsafe byte* NumericalWriteBlock(byte* currentBlock, ref int remainingBytes, int currentChunkFreeBytes)
        {
            int toCopy = Math.Min(remainingBytes, currentChunkFreeBytes);

            if (ShouldReverseEndian)
                for (int i = 0; i < toCopy; i++)
                    CurrentChunk.Data[CurrentChunk.BytesFilled++] = *currentBlock--;
            else
                for (int i = 0; i < toCopy; i++)
                    CurrentChunk.Data[CurrentChunk.BytesFilled++] = *currentBlock++;

            remainingBytes -= toCopy;

            return currentBlock;
        }

        public unsafe static byte[] WriteInt32Truncated(int s, int bytesToWrite, ABSaveWriter writer)
        {
            byte* data = (byte*)&s;
            byte[] res = new byte[bytesToWrite];

            // This is writing from end to start! In order to allow sizes like 1, 2 or 3 to still contain the information required.
            // L ++-- --++ B
            if (BitConverter.IsLittleEndian)
            {
                byte* currentDataPos = data;

                if (writer.ShouldReverseEndian)
                    for (int i = bytesToWrite; i >= 0; i--)
                        res[i] = *currentDataPos++;
                else
                    for (int i = 0; i < bytesToWrite; i++)
                        res[i] = *currentDataPos++;
            }
            else
            {
                byte* currentDataPos = data + 3;

                if (writer.ShouldReverseEndian)
                    for (int i = 0; i < bytesToWrite; i++)
                        res[i] = *currentDataPos--;
                else
                    for (int i = bytesToWrite; i >= 0; i--)
                        res[i] = *currentDataPos--;
            }

            return res;
        }
        #endregion

        #region Exporting Data

        public byte[] ToByteArray()
        {
            var res = new byte[TotalBytesFilled];

            int currentPos = 0;

            ABSaveWriterDataChunk chunk = DataStart;
            do
            {
                Buffer.BlockCopy(chunk.Data, 0, res, currentPos, chunk.BytesFilled);
                currentPos += chunk.BytesFilled;
                chunk = chunk.Next;
            }
            while (chunk != null);

            return res;
        }

        #endregion
    }

    public abstract class ABSaveEndianDependantWriter
    {
        public unsafe abstract void WriteCharBytes(byte* currentCharBuffer);
    }
}
