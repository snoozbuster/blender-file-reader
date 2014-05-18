using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderFileReader
{
    /// <summary>
    /// Reads a file into StructureDNA, PopulatedStructs, and whatnot, and when done represents a loaded Blender file
    /// </summary>
    public class BlenderFile
    {
        /// <summary>
        /// Describes the pointer size of the file (either 4 or 8).
        /// </summary>
        public int PointerSize { get; private set; }

        /// <summary>
        /// Version number (in string form) of the version of Blender this file was saved in.
        /// </summary>
        public string VersionNumber { get; private set; }

        /// <summary>
        /// List of arrays of PopulatedStructures created from parsing a FileBlock. 
        /// </summary>
        public List<PopulatedStructure[]> Structures { get; private set; }

        /// <summary>
        /// A reference to the parsed structure DNA for this file.
        /// </summary>
        public StructureDNA StructureDNA { get; private set; }

        private List<FileBlock> fileBlocks = new List<FileBlock>();

        public BlenderFile(string path)
            : this(new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
        { }

        public BlenderFile(BinaryReader reader)
        {
            string versionNumber;

            readHeader(reader, out versionNumber);

            VersionNumber = versionNumber;

            // file header read; at first file block. readBlocks returns the StructureDNA.
            StructureDNA = readBlocks(reader);

            // create PopulatedStructures
            Structures = createStructures();
        }

        private void readHeader(BinaryReader fileReader, out string versionNumber)
        {
            versionNumber = null;
            fileReader.ReadBytes(7); // read out 'BLENDER'
            PointerSize = Convert.ToChar(fileReader.ReadByte()) == '_' ? 4 : 8; // '_' = 4, '-' = 8
            char endianness = Convert.ToChar(fileReader.ReadByte()); // 'v' = little, 'V' = big

            if((endianness == 'v' && !BitConverter.IsLittleEndian) || (endianness == 'V' && BitConverter.IsLittleEndian)
                || (endianness != 'v' && endianness != 'V'))
                throw new InvalidDataException("Endianness of computer does not appear to match endianness of file. Open the file in Blender and save it to convert.");

            versionNumber = new string(new[] { Convert.ToChar(fileReader.ReadByte()), '.', Convert.ToChar(fileReader.ReadByte()),
                Convert.ToChar(fileReader.ReadByte()) });
        }

        private StructureDNA readBlocks(BinaryReader fileReader)
        {
            StructureDNA dna = null;
            do
            {
                FileBlock b = FileBlock.ReadBlock(fileReader, PointerSize);
                if(b.Code == "DNA1")
                    dna = (StructureDNA)b;
                fileBlocks.Add(b);
            } while(fileReader.BaseStream.Position < fileReader.BaseStream.Length);

            if(dna == null)
                throw new InvalidDataException("This file contains no structure DNA! What are you trying to pull?!");

            return dna;
        }

        private List<PopulatedStructure[]> createStructures()
        {
            List<PopulatedStructure[]> structures = new List<PopulatedStructure[]>();
            foreach(FileBlock b in fileBlocks)
            {
                PopulatedStructure[] temp = PopulatedStructure.ParseFileBlock(b, StructureDNA, PointerSize);
                if(temp[0] != null)
                    structures.Add(temp);
            }

            if(PopulatedStructure.RawBlockMessages.Count > 0)
                Console.WriteLine(PopulatedStructure.RawBlockMessages.Count + " warnings encountered, more information will be written to file.");

            return structures;
        }

        /// <summary>
        /// Returns a list of all blocks by a given block code.
        /// </summary>
        /// <param name="code">Four-character block code.</param>
        /// <returns></returns>
        public List<FileBlock> GetBlocksByCode(string code)
        {
            return fileBlocks.FindAll(v => { return v.Code == code; });
        }

        /// <summary>
        /// Gets the file block list.
        /// </summary>
        /// <returns></returns>
        public List<FileBlock> GetBlockList()
        {
            return fileBlocks;
        }

        /// <summary>
        /// Gets a single block by its old memory address.
        /// </summary>
        /// <param name="address">Old address</param>
        /// <returns></returns>
        public FileBlock GetBlockByAddress(ulong address)
        {
            return fileBlocks.Find(v => { return v.OldMemoryAddress == address; });
        }
    }
}
