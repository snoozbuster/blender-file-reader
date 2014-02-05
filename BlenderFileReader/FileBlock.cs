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
    class FileBlock
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

        private static List<FileBlock> fileBlocks = new List<FileBlock>();

        /// <summary>
        /// Reads a file block and adds it to the master list of file blocks. 
        /// If the block is SDNA, initializes StructureDNA. 
        /// </summary>
        /// <param name="file">A blend file with the reader at the start of a file block.</param>
        /// <returns>The FileBlock that was added to the master list.</returns>
        public static FileBlock ReadBlock(BinaryReader file)
        {
            int pointerSize = Program.PointerSize;
            if(pointerSize != 4 && pointerSize != 8)
                throw new ArgumentException("Impossible for pointerSize to be " + pointerSize);

            string code;
            int size, sdna, count;
            byte[] data;
            ulong address;

            code = new string(file.ReadChars(4));
            size = file.ReadInt32();
            address = pointerSize == 4 ? file.ReadUInt32() : file.ReadUInt64();
            sdna = file.ReadInt32();
            count = file.ReadInt32();
            data = file.ReadBytes(size);

            FileBlock block;

            if(code == "DNA1") // then this block is StructureDNA
            {
                StructureDNA.Create(data);
                block = new FileBlock(code, size, sdna, count, data);
            }
            else
                block = new FileBlock(code, size, sdna, count, data);

            // blocks are aligned at four bytes
            while(file.BaseStream.Position % 4 != 0 && file.BaseStream.Position < file.BaseStream.Length) // don't want to read off the file by accident
                file.ReadByte();

            block.OldMemoryAddress = address;
            fileBlocks.Add(block);
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

        /// <summary>
        /// Returns a list of all blocks by a given block code.
        /// </summary>
        /// <param name="code">Four-character block code.</param>
        /// <returns></returns>
        public static List<FileBlock> GetBlocksByCode(string code)
        {
            return fileBlocks.FindAll(v => { return v.Code == code; });
        }

        /// <summary>
        /// Gets the file block list.
        /// </summary>
        /// <returns></returns>
        public static List<FileBlock> GetBlockList()
        {
            return fileBlocks;
        }

        /// <summary>
        /// Gets a single block by its old memory address.
        /// </summary>
        /// <param name="address">Old address</param>
        /// <returns></returns>
        public static FileBlock GetBlockByAddress(ulong address)
        {
            return fileBlocks.Find(v => { return v.OldMemoryAddress == address; });
        }
    }
}
