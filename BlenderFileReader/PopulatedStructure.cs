using System.Collections.Generic;
using System.Linq;

namespace BlenderFileReader
{
    /// <summary>
    /// Represents a SDNA structure filled with data.
    /// </summary>
    public class PopulatedStructure
    {
        /// <summary>
        /// Parses a file block. If the file block is SDNA or another block with block.count == 0, it will return null.
        /// </summary>
        /// <param name="block">The block to parse.</param>
        /// <returns>An array of PopulatedStructures, or { null } if no structures are defined.</returns>
        public static PopulatedStructure[] ParseFileBlock(FileBlock block, StructureDNA sdna, int pointerSize, int blocksParsed, List<string> rawBlockMessages)
        {
            if(block.Count == 0 || block.Code == "DNA1")
                return null;

            if(block.Data.Length != sdna.StructureList[block.SDNAIndex].StructureTypeSize * block.Count)
            {
                // generally, these are things like raw data; packed files, preview images, and arrays of pointers that are themselves pointed to.
                // I have no idea what TEST and REND do.
                rawBlockMessages.Add(blocksParsed + " " + block.OldMemoryAddress.ToString("X" + (pointerSize * 2)) + " " + block.Code + " " + block.SDNAIndex + " " + sdna.StructureList[block.SDNAIndex].StructureTypeSize * block.Count + " " + block.Data.Length);
                return null;
            }

            PopulatedStructure[] output = new PopulatedStructure[block.Count];

            if(block.Count > 1)
                for(int i = 0; i < block.Count; i++)
                {
                    byte[] data = new byte[block.Size / block.Count];
                    for(int j = 0; j < block.Size / block.Count; j++)
                        data[j] = block.Data[i * (block.Size / block.Count) + j];
                    output[i] = new PopulatedStructure(data, sdna.StructureList[block.SDNAIndex], pointerSize);
                    output[i].Size = sdna.StructureList[block.SDNAIndex].StructureTypeSize;
                    output[i].Type = sdna.StructureList[block.SDNAIndex].StructureTypeName;
                    output[i].ContainingBlock = block;
                }
            else
            {
                output[0] = new PopulatedStructure(block.Data, sdna.StructureList[block.SDNAIndex], pointerSize);
                output[0].Size = sdna.StructureList[block.SDNAIndex].StructureTypeSize;
                output[0].Type = sdna.StructureList[block.SDNAIndex].StructureTypeName;
                output[0].ContainingBlock = block;
            }

            return output;
        }

        /// <summary>
        /// Type of the structure.
        /// </summary>
        public string Type { get; private set; }
        /// <summary>
        /// Size of the structure.
        /// </summary>
        public short Size { get; private set; }

        /// <summary>
        /// List of the flattened data.
        /// </summary>
        public List<FieldInfo> FlattenedData { get; private set; }

        /// <summary>
        /// The FileBlock containing this structure.
        /// </summary>
        public FileBlock ContainingBlock { get; private set; }

        // holds pointer size for this structure
        private int pointerSize;

        protected PopulatedStructure(byte[] data, SDNAStructure template, int pointerSize)
        {
            FlattenedData = new List<FieldInfo>();
            int pos = 0;
            this.pointerSize = pointerSize;
            parseStructureFields("", template, data, ref pos); // begin recursive parsing
        }

        /// <summary>
        /// Recursively parses fields into flattened form.
        /// </summary>
        /// <param name="breadcrumbs">Trail so far (ie "parent.child" or "parent" or "").</param>
        /// <param name="toParse">Structure to parse the fields of.</param>
        /// <param name="data">Data to input into fields.</param>
        /// <param name="position">Position of index in data.</param>
        private void parseStructureFields(string breadcrumbs, SDNAStructure toParse, byte[] data, ref int position)
        {
            foreach(BlenderField f in toParse.FieldList)
                if(f.IsPointer) // it's almost a primitive
                {
                    if(f.IsArray)
                    {
                        int width = 1;
                        if(f.Name.Count(v => { return v == '['; }) == 1)
                            FlattenedData.Add(new FieldInfo(subArray(data, position, pointerSize * getIntFromArrayName(f.Name)),
                                f.Name, f.Type.Name, (short)pointerSize, breadcrumbs, toParse.StructureTypeName, pointerSize));
                        else
                        {
                            int start = f.Name.LastIndexOf('[');
                            int end = f.Name.LastIndexOf(']');
                            string numberString = f.Name.Substring(start + 1, end - 1 - start);
                            width = int.Parse(numberString);
                            FlattenedData.Add(new FieldInfo(subArray(data, position, pointerSize * getIntFromArrayName(f.Name) * width),
                                f.Name, f.Type.Name, (short)pointerSize, breadcrumbs, toParse.StructureTypeName, pointerSize));
                        }
                        position += pointerSize * width * getIntFromArrayName(f.Name);
                    }
                    else
                    {
                        FlattenedData.Add(new FieldInfo(subArray(data, position, pointerSize),
                            f.Name, f.Type.Name, (short)pointerSize, breadcrumbs, toParse.StructureTypeName, pointerSize));
                        position += pointerSize;
                    }
                }
                else if(f.IsPrimitive)
                {
                    if(f.IsArray)
                    {
                        int width = 1;
                        if(f.Name.Count(v => { return v == '['; }) == 1)
                            FlattenedData.Add(new FieldInfo(subArray(data, position, f.Type.Size * getIntFromArrayName(f.Name)),
                                f.Name, f.Type.Name, f.Type.Size, breadcrumbs, toParse.StructureTypeName, pointerSize));
                        else
                        {
                            int start = f.Name.LastIndexOf('[');
                            int end = f.Name.LastIndexOf(']');
                            string numberString = f.Name.Substring(start + 1, end - 1 - start);
                            width = int.Parse(numberString);
                            FlattenedData.Add(new FieldInfo(subArray(data, position, f.Type.Size * getIntFromArrayName(f.Name) * width),
                                f.Name, f.Type.Name, f.Type.Size, breadcrumbs, toParse.StructureTypeName, pointerSize));
                        }
                        position += f.Type.Size * width * getIntFromArrayName(f.Name);
                    }
                    else
                    {
                        FlattenedData.Add(new FieldInfo(subArray(data, position, f.Type.Size),
                            f.Name, f.Type.Name, f.Type.Size, breadcrumbs, toParse.StructureTypeName, pointerSize));
                        position += f.Type.Size;
                    }
                }
                else // non-primitive
                {
                    if(f.IsArray)
                    {
                        if(f.Name.Count(v => { return v == '['; }) == 1)
                            for(int i = 0; i < getIntFromArrayName(f.Name); i++)
                                parseStructureFields((breadcrumbs == string.Empty ? "" : breadcrumbs + ".") + f.Name.Substring(0, f.Name.IndexOf('[')) + "[" + i + "]", f.Structure, data, ref position);
                        else
                        {
                            // ugh
                            int start = f.Name.LastIndexOf('[');
                            int end = f.Name.LastIndexOf(']');
                            string numberString = f.Name.Substring(start + 1, end - 1 - start);
                            int width = int.Parse(numberString);
                            for(int i = 0; i < getIntFromArrayName(f.Name); i++)
                                for(int j = 0; j < width; j++)
                                    parseStructureFields((breadcrumbs == string.Empty ? "" : breadcrumbs + ".") + f.Name.Substring(0, f.Name.IndexOf('[')) + "[" + i + "]" + "[" + j + "]", f.Structure, data, ref position);
                        }
                    }
                    else
                        parseStructureFields((breadcrumbs == string.Empty ? "" : breadcrumbs + ".") + f.Name, f.Structure, data, ref position);
                }

        }

        // helper function to "slice" arrays
        private byte[] subArray(byte[] original, int start, int length)
        {
            byte[] output = new byte[length];
            for(int i = 0; i < length; i++)
                output[i] = original[start + i];
            return output;
        }

        // helper function to get array size from field name
        private int getIntFromArrayName(string name)
        {
            int start = name.IndexOf('[');
            int end = name.IndexOf(']');
            string numberString = name.Substring(start + 1, end - 1 - start);
            int number = int.Parse(numberString);
            return number;
        }

        /// <summary>
        /// Array-style access to fields.
        /// </summary>
        /// <param name="key">Name of the flattened field you want to look up.</param>
        /// <returns>FieldInfo object representing the field with that identifier</returns>
        public FieldInfo this[string identifier]
        {
            get
            {
                return FlattenedData.Find(field => { return field.Name == identifier; });
            }
        }
    }
}
