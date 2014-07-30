using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public List<Structure[]> Structures { get; private set; }

        /// <summary>
        /// A reference to the parsed structure DNA for this file.
        /// </summary>
        public StructureDNA StructureDNA { get; private set; }

        /// <summary>
        /// A list of pieces of information about raw data blocks (blocks that are just data). In the format
        /// "block_number block_address block_code block_index bytes_given bytes_expected"
        /// </summary>
        public List<string> RawBlockMessages = new List<string>();

        /// <summary>
        /// The path on disk to the file. May be null if the <pre>BlenderFile</pre> was instantiated with 
        /// a <pre>BinaryReader</pre> instead of a string.
        /// </summary>
        public string SourceFilename { get; private set; }

        private List<FileBlock> fileBlocks = new List<FileBlock>();
        private Dictionary<ulong, Structure[]> memoryMap = new Dictionary<ulong, Structure[]>();

        /// <summary>
        /// Creates a new parsed <pre>BlenderFile</pre> from a filepath.
        /// </summary>
        /// <param name="path">Path to <pre>.blend</pre> file to be parsed.</param>
        public BlenderFile(string path)
            : this(new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
        {
            SourceFilename = path;
        }

        /// <summary>
        /// Creates a new parsed <pre>BlenderFile</pre> from a <pre>BinaryReader</pre> containing a <pre>.blend</pre> file.
        /// Warning: this constructor will not and cannot set SourceFilename, as the reader has no way of
        /// knowing where its source stream came from.
        /// </summary>
        /// <param name="reader"><pre>BinaryReader</pre> containing a Blender file.</param>
        public BlenderFile(BinaryReader reader)
        {
            string versionNumber;

            readHeader(reader, out versionNumber);

            VersionNumber = versionNumber;

            // file header read; at first file block. readBlocks returns the StructureDNA.
            StructureDNA = readBlocks(reader);

            // create PopulatedStructures
            memoryMap = createStructures();
            Structures = memoryMap.Values.ToList();

            reader.Close();
        }

        /// <summary>
        /// Reads the header of the blend file
        /// </summary>
        /// <param name="fileReader"></param>
        /// <param name="versionNumber"></param>
        private void readHeader(BinaryReader fileReader, out string versionNumber)
        {
            versionNumber = null;
            fileReader.ReadBytes(7); // read out 'BLENDER', this can be used to determine if the file is gzipped
            PointerSize = Convert.ToChar(fileReader.ReadByte()) == '_' ? 4 : 8; // '_' = 4, '-' = 8
            char endianness = Convert.ToChar(fileReader.ReadByte()); // 'v' = little, 'V' = big

            if((endianness == 'v' && !BitConverter.IsLittleEndian) || (endianness == 'V' && BitConverter.IsLittleEndian)
                || (endianness != 'v' && endianness != 'V'))
                throw new InvalidDataException("Endianness of computer does not appear to match endianness of file. Open the file in Blender and save it to convert.");

            // read out version number
            versionNumber = new string(new[] { Convert.ToChar(fileReader.ReadByte()), '.', Convert.ToChar(fileReader.ReadByte()),
                Convert.ToChar(fileReader.ReadByte()) });
        }

        /// <summary>
        /// Reads the file blocks from the file. Returns the block with the code <pre>DNA1</pre>, which is the file's
        /// structure DNA.
        /// </summary>
        /// <param name="fileReader">Reference to file reader for current file.</param>
        /// <returns>File's Structure DNA.</returns>
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

        /// <summary>
        /// Parses the file blocks to create a map of memory addresses to populated structures.
        /// </summary>
        /// <returns>A dictionary mapping memory addresses to the structures held in the corresponding file block.</returns>
        private Dictionary<ulong, Structure[]> createStructures()
        {
            Dictionary<ulong, Structure[]> structures = new Dictionary<ulong, Structure[]>();
            int blocksParsed = 0;
            foreach(FileBlock b in fileBlocks)
            {
                Structure[] temp = Structure.ParseFileBlock(b, blocksParsed++, this);
                if(temp != null)
                    structures.Add(b.OldMemoryAddress, temp);
            }

            return structures;
        }

        /// <summary>
        /// Gets all structures of the named type. This isn't great to use on things like MPoly or other list types, as you won't be able to tell
        /// when the list ends and the next begins.
        /// </summary>
        /// <param name="typeName">Name of the type you want to find.</param>
        /// <returns>Array of all PopulatedStructures of that type.</returns>
        public Structure[] GetStructuresOfType(string typeName)
        {
            List<Structure> output = new List<Structure>();
            Structures.ForEach(array => {
                output.AddRange(array.Where(structure => { return structure.TypeName == typeName; }));
            });
            return output.ToArray();
        }

        /// <summary>
        /// Gets an array of PopulatedStructures by their containing FileBlock's old memory address.
        /// Returns null if address isn't found.
        /// </summary>
        /// <param name="address">Address to look up.</param>
        /// <returns></returns>
        public Structure[] GetStructuresByAddress(ulong address)
        {
            return memoryMap.ContainsKey(address) ? memoryMap[address] : null;
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
