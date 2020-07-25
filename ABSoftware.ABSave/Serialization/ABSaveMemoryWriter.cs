using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("ABSoftware.ABSave.Testing.UnitTests,PublicKey="+
    "0024000004800000940000000602000000240000525341310004000001000100b5f7ee33f51a2b"+
    "1770ddc0d7c04adde37382b11e83cecc46d60f9f7bac5492fad71d066f760e2b163f100aa2a51f"+
    "036843f13de106ee3dd3c38a8c6ea28dfcf6d08e0a633d2c69bdace83858240697c03de97def5d"+
    "a06d8bec0830a75c092aae290b01420d11e7d5f9caaeb53cb624d8127de4f2765f9b466eb3a738"+
    "fba225fe")]
namespace ABSoftware.ABSave.Serialization
{
    /// <summary>
    /// A stream that writes in "chunks" as opposed to one big block of memory. Used by default with ABSave when writing to a variable.
    /// </summary>
    public class ABSaveMemoryWriter : ABSaveWriter
    {
        // If a new chunk is allocated automatically (NOT writing an uncopied byte array), how big that chunk should be in addition to the data that's initially being added.
        const int AUTO_MIN_CHUNK_SIZE = 128;

        internal LinkedMemoryDataChunk DataStart;
        internal LinkedMemoryDataChunk CurrentChunk;
        internal LinkedMemoryDataChunk DataEnd;
        internal int FreeSpace;
        internal int TotalBytesFilled = 0;

        public ABSaveMemoryWriter(ABSaveSettings settings) : base(settings)
        {
            DataStart = CurrentChunk = DataEnd = new LinkedMemoryDataChunk(AUTO_MIN_CHUNK_SIZE);
            FreeSpace = AUTO_MIN_CHUNK_SIZE;
        }

        #region Core Chunk Management

        public void EnsureCanFit(int numberOfBytesRequired)
        {
            if (numberOfBytesRequired > FreeSpace)
                ScaleUp(numberOfBytesRequired - FreeSpace);
        }

        void ScaleUp(int required)
        {
            int newSize = required + AUTO_MIN_CHUNK_SIZE;
            DataEnd = DataEnd.Next = new LinkedMemoryDataChunk(newSize);
            FreeSpace += newSize;
        }

        void MoveToNextChunk() => CurrentChunk = CurrentChunk.Next;

        #endregion

        #region Byte Writing
        // Unchecked means it doesn't check whether there's any free space.
        public override void WriteByte(byte value)
        {
            EnsureCanFit(1);
            UncheckedWriteByte(value);
        }

        public void UncheckedWriteByte(byte byt)
        {
            if (CurrentChunk.BytesFilled == CurrentChunk.Data.Length) MoveToNextChunk();
            CurrentChunk.Data[CurrentChunk.BytesFilled++] = byt;
            FreeSpace--;
            TotalBytesFilled++;
        }

        public override unsafe void WriteByteArray(byte[] arr, bool writeSize)
        {
            // If the array is bigger than 36 (if we can fit the length in) or if it's bigger than 72 (if we can't fit the length in), 
            // we'll just add a new chunk with it instead of copying the data in. Otherwise, we'll copy across the data.
            // NOTE: If we aren't writing the size, then being bigger than 36 bytes is good enough reason to add a new chunk instead of copying.
            if (arr.Length > 36 && (!writeSize || FreeSpace > 4 || arr.Length > 72))
            {
                // If we can't fit the length in, allocate a tiny little chunk specifically to fit it in right before the array.
                if (writeSize)
                {
                    if (FreeSpace < 4)
                        DataEnd = DataEnd.Next = new LinkedMemoryDataChunk(4 - FreeSpace);

                    UncheckedWriteInt32((uint)arr.Length);
                }

                CurrentChunk = DataEnd = DataEnd.Next = new LinkedMemoryDataChunk(arr);
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

        public unsafe void UncheckedWriteAndCopyByteArray(byte[] arr, int start, int length, bool writeSize)
        {
            fixed (byte* data = arr)
                UncheckedWriteAndCopyByteArray(data + start, length, writeSize);
        }

        public unsafe void UncheckedWriteAndCopyByteArray(byte* arr, int length, bool writeSize)
        {
            if (CurrentChunk.BytesFilled == CurrentChunk.Data.Length) MoveToNextChunk();

            if (writeSize)
                UncheckedWriteInt32((uint)length);

            int remainingData = length;

            // Copy as much as we can into the current chunk.
            arr = CopyByteArrayBlock(arr, ref remainingData, CurrentChunk.Data.Length - CurrentChunk.BytesFilled);

            // If we weren't able to copy everything into this chunk, keep copying into the next chunks until we've copied everything.
            while (remainingData > 0)
            {
                MoveToNextChunk();
                arr = CopyByteArrayBlock(arr, ref remainingData, CurrentChunk.Data.Length);
            }

            FreeSpace -= length;
            TotalBytesFilled += length;
        }

        unsafe byte* CopyByteArrayBlock(byte* arr, ref int remainingData, int currentChunkFreeSpace)
        {
            int canCopy = Math.Min(currentChunkFreeSpace, remainingData);
            fixed (byte* chunkData = CurrentChunk.Data)
                Buffer.MemoryCopy(arr, chunkData + CurrentChunk.BytesFilled, canCopy, canCopy);

            arr += canCopy;
            CurrentChunk.BytesFilled += canCopy;
            remainingData -= canCopy;

            return arr; 
        }

        #endregion

        #region Text Writing

        public override unsafe void FastWriteShorts(short* str, int strLength)
        {
            int byteSize = strLength * 2;

            EnsureCanFit(byteSize + 4);

            if (ShouldReverseEndian) UncheckedFastWriteShortsReverse((byte*)str, byteSize);
            else UncheckedWriteAndCopyByteArray((byte*)str, byteSize, true);
        }

        unsafe void UncheckedFastWriteShortsReverse(byte* strData, int remainingBytes)
        {
            UncheckedWriteInt32((uint)remainingBytes);
            bool copyingSecondByteOfChar = false;

            FreeSpace -= remainingBytes;
            TotalBytesFilled += remainingBytes;

            byte charSecondByte = 0;
            byte* charSecondBytePointer = &charSecondByte;

            // Shorts/characters are two bytes big. When we encounter a character we put the second byte into a buffer, that way
            // if the character needs to written across two chunks, we can write the second byte when we move over to the next chunk.
            // Write as much as we can to the current chunk.
            copyingSecondByteOfChar = CopyBlockOfShortsReverse(ref strData, charSecondBytePointer, copyingSecondByteOfChar, ref remainingBytes);

            // If we weren't able to copy everything into this chunk, keep copying into the next chunks until we've copied everything.
            while (remainingBytes > 0)
            {
                MoveToNextChunk();
                copyingSecondByteOfChar = CopyBlockOfShortsReverse(ref strData, charSecondBytePointer, copyingSecondByteOfChar, ref remainingBytes);
            }
        }

        unsafe bool CopyBlockOfShortsReverse(ref byte* strData, byte* charSecondByte, bool copyingSecondByteOfChar, ref int remainingBytes)
        {
            int bytesToCopy = Math.Min(CurrentChunk.Data.Length - CurrentChunk.BytesFilled, remainingBytes);
            remainingBytes -= bytesToCopy;

            if (copyingSecondByteOfChar)
            {
                CurrentChunk.Data[CurrentChunk.BytesFilled++] = *charSecondByte;
                bytesToCopy--;
            }

            byte* actualStrData = strData;

            // Write all the perfect 2-byte blocks we can.
            for (; bytesToCopy > 1; bytesToCopy -= 2)
            {
                CurrentChunk.Data[CurrentChunk.BytesFilled++] = actualStrData[1];
                CurrentChunk.Data[CurrentChunk.BytesFilled++] = actualStrData[0];
                actualStrData += 2;
            }

            // If there's still one more byte we can fit into this chunk, write that, and queue up the next byte to write .
            if (bytesToCopy == 1)
            {
                CurrentChunk.Data[CurrentChunk.BytesFilled++] = actualStrData[1];

                *charSecondByte = actualStrData[0];

                actualStrData += 2;
                strData = actualStrData;
                return true;
            }

            strData = actualStrData;
            return false;
        }

        #endregion

        #region Numerical Writing

        public override unsafe void WriteInt16(ushort num)
        {
            EnsureCanFit(2);
            UncheckedWriteInt16(num);
        }

        public unsafe void UncheckedWriteInt16(ushort num)
        {
            byte* data = (byte*)&num;
            if (ShouldReverseEndian)
            {
                UncheckedWriteByte(data[1]);
                UncheckedWriteByte(data[0]);
            }
            else
            {
                UncheckedWriteByte(data[0]);
                UncheckedWriteByte(data[1]);
            }
        }

        public override unsafe void WriteInt32(uint num)
        {
            EnsureCanFit(4);
            UncheckedWriteInt32(num);
        }

        public unsafe void UncheckedWriteInt32(uint num) => NumericalWriteBytes((byte*)&num, 4);

        public override unsafe void WriteInt64(ulong num)
        {
            EnsureCanFit(8);
            UncheckedWriteInt64(num);
        }

        public unsafe void UncheckedWriteInt64(ulong num) => NumericalWriteBytes((byte*)&num, 8);

        public override unsafe void WriteSingle(float num)
        {
            EnsureCanFit(4);
            UncheckedWriteSingle(num);
        }

        public unsafe void UncheckedWriteSingle(float num) => NumericalWriteBytes((byte*)&num, 4);

        public override unsafe void WriteDouble(double num)
        {
            EnsureCanFit(8);
            UncheckedWriteDouble(num);
        }

        public unsafe void UncheckedWriteDouble(double num) => NumericalWriteBytes((byte*)&num, 8);

        protected unsafe void NumericalWriteBytes(byte* data, int numberOfBytes)
        {
            int remainingBytes = numberOfBytes;
            byte* currentDataPos = ShouldReverseEndian ? data + numberOfBytes : data;

            currentDataPos = NumericalWriteBlock(currentDataPos, ref remainingBytes, CurrentChunk.Data.Length - CurrentChunk.BytesFilled);

            while (remainingBytes > 0)
            {
                MoveToNextChunk();
                currentDataPos = NumericalWriteBlock(currentDataPos, ref remainingBytes, CurrentChunk.Data.Length);
            }

            FreeSpace -= numberOfBytes;
            TotalBytesFilled += numberOfBytes;
        }

        unsafe byte* NumericalWriteBlock(byte* currentBlock, ref int remainingBytes, int currentChunkFreeBytes)
        {
            int toCopy = Math.Min(remainingBytes, currentChunkFreeBytes);

            if (ShouldReverseEndian)
                for (int i = 0; i < toCopy; i++)
                    CurrentChunk.Data[CurrentChunk.BytesFilled++] = *--currentBlock;
            else
                for (int i = 0; i < toCopy; i++)
                    CurrentChunk.Data[CurrentChunk.BytesFilled++] = *currentBlock++;

            remainingBytes -= toCopy;

            return currentBlock;
        }

        public override unsafe void WriteInt32ToSignificantBytes(int s, int significantBytes)
        {
            byte* data = (byte*)&s;

            EnsureCanFit(significantBytes);

            // L ++-- --++ B
            if (BitConverter.IsLittleEndian)
            {
                if (ShouldReverseEndian)
                {
                    byte* currentDataPos = data + significantBytes;
                    for (int i = 0; i < significantBytes; i++)
                        UncheckedWriteByte(*--currentDataPos);
                }
                else
                {
                    
                    byte* currentDataPos = data;
                    for (int i = 0; i < significantBytes; i++)
                        UncheckedWriteByte(*currentDataPos++);
                }
            }
            else
            {
                if (ShouldReverseEndian)
                {
                    byte* currentDataPos = data + 3;
                    for (int i = 0; i < significantBytes; i++)
                        UncheckedWriteByte(*currentDataPos--);
                }
                else
                {
                    byte* currentDataPos = data + (4 - significantBytes);
                    for (int i = 0; i < significantBytes; i++)
                        UncheckedWriteByte(*currentDataPos++);
                }
            }
        }

        public override unsafe void WriteDecimal(decimal num)
        {
            EnsureCanFit(16);

            var bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                UncheckedWriteInt32((uint)bits[i]);
        }

        #endregion

        #region Extracting

        public void CopyToArray(byte[] arr, int index)
        {
            LinkedMemoryDataChunk chunk = DataStart;
            do
            {
                Buffer.BlockCopy(chunk.Data, 0, arr, index, chunk.BytesFilled);
                index += chunk.BytesFilled;
                chunk = chunk.Next;
            }
            while (chunk != null);
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[TotalBytesFilled];
            CopyToArray(bytes, 0);
            return bytes;
        }

        #endregion
    }

    internal class LinkedMemoryDataChunk
    {
        internal byte[] Data;
        internal int BytesFilled;
        internal LinkedMemoryDataChunk Next;

        internal LinkedMemoryDataChunk(int size)
        {
            Data = new byte[size];
            BytesFilled = 0;
            Next = null;
        }

        internal LinkedMemoryDataChunk(byte[] data)
        {
            Data = data;
            BytesFilled = data.Length;
            Next = null;
        }
    }
}
