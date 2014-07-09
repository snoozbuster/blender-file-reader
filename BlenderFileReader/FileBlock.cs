    using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderFileReader
{
    /// <summary>
    /// Defines a FileBlock that contains a header and data.
    /// </summary>
    public class FileBlock
    {
        /// <summary>
        /// Four-character code indicating the type of data in the FileBlock.
        /// </summary>
        public string Code { get; private set; }
        /// <summary>
        /// Size of the FileBlock's data, in bytes.
        /// </summary>
        public int Size { get; private set; }
        /// <summary>
        /// Index in the structures defined by SDNA used to decode the data in this FileBlock.
        /// </summary>
        public int SDNAIndex { get; private set; }
        /// <summary>
        /// Number of objects to decode.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Raw data contained in the FileBlock.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Old memory address of the FileBlock; used for lists and pointer references.
        /// </summary>
        public ulong OldMemoryAddress { get; private set; }

        /// <summary>
        /// Reads a file block and returns it. 
        /// If the block is SDNA, initializes and returns an instance of <pre>StructureDNA</pre>. 
        /// </summary>
        /// <param name="file">A blend file with the reader at the start of a file block.</param>
        /// <param name="pointerSize">The pointer size of the current file.</param>
        /// <returns>A parsed FileBlock.</returns>
        public static FileBlock ReadBlock(BinaryReader file, int pointerSize)
        {
            if(pointerSize != 4 && pointerSize != 8)
                throw new ArgumentException("Impossible for pointerSize to be " + pointerSize);

            string code;
            int size, sdna, count;
            byte[] data;
            ulong address;

            // read block header
            code = new string(file.ReadChars(4));
            size = file.ReadInt32();
            address = pointerSize == 4 ? file.ReadUInt32() : file.ReadUInt64();
            sdna = file.ReadInt32();
            count = file.ReadInt32();
            data = file.ReadBytes(size);

            FileBlock block = code == "DNA1" ? new StructureDNA(code, size, sdna, count, data) : 
                                               new FileBlock(code, size, sdna, count, data);

            // blocks are aligned at four bytes
            while(file.BaseStream.Position % 4 != 0 && file.BaseStream.Position < file.BaseStream.Length) // don't want to read off the file by accident
                file.ReadByte();

            // I'm not 100% sure why this is done here.
            block.OldMemoryAddress = address;
            return block;
        }

        protected FileBlock(string code, int size, int sdna, int count, byte[] data)
        {
            Code = code;
            Size = size;
            SDNAIndex = sdna;
            Count = count;
            Data = data;
        }

        // todo: perhaps I could add some convenience methods here
    }
}
