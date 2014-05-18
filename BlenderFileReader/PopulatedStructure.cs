using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderFileReader
{
    /// <summary>
    /// Represents a SDNA structure filled with data.
    /// </summary>
    public class PopulatedStructure
    {
        private static int blocksParsed = 0;

        /// <summary>
        /// A list of pieces of information about raw data blocks (blocks that are just data). In the format
        /// "block_number block_address block_code block_index bytes_given bytes_expected"
        /// </summary>
        public static List<string> RawBlockMessages = new List<string>();
        
        /// <summary>
        /// Parses a file block. If the file block is SDNA or another block with block.count == 0, it will return null.
        /// </summary>
        /// <param name="block">The block to parse.</param>
        /// <returns>An array of PopulatedStructures, or { null } if no structures are defined.</returns>
        public static PopulatedStructure[] ParseFileBlock(FileBlock block, int pointerSize)
        {
            blocksParsed++;

            if(block.Count == 0 || block.Code == "DNA1")
                return new PopulatedStructure[] { null };

            if(block.Data.Length != StructureDNA.StructureList[block.SDNAIndex].StructureTypeSize * block.Count)
            {
                // generally, these are things like raw data; packed files, preview images, and the target of pointers to primitives.
                // I have no idea what TEST and REND do.
                RawBlockMessages.Add(blocksParsed + " " + block.OldMemoryAddress.ToString("X" + (pointerSize * 2)) + " " + block.Code + " " + block.SDNAIndex + " " + StructureDNA.StructureList[block.SDNAIndex].StructureTypeSize * block.Count + " " + block.Data.Length);
                return new PopulatedStructure[] { null };
            }

            PopulatedStructure[] output = new PopulatedStructure[block.Count];

            if(block.Count > 1)
                for(int i = 0; i < block.Count; i++)
                {
                    byte[] data = new byte[block.Size / block.Count];
                    for(int j = 0; j < block.Size / block.Count; j++)
                        data[j] = block.Data[i * (block.Size / block.Count) + j];
                    output[i] = new PopulatedStructure(data, StructureDNA.StructureList[block.SDNAIndex], pointerSize);
                    output[i].Size = StructureDNA.StructureList[block.SDNAIndex].StructureTypeSize;
                    output[i].Type = StructureDNA.StructureList[block.SDNAIndex].StructureTypeName;
                    output[i].ContainingBlock = block;
                }
            else
            {
                output[0] = new PopulatedStructure(block.Data, StructureDNA.StructureList[block.SDNAIndex], pointerSize);
                output[0].Size = StructureDNA.StructureList[block.SDNAIndex].StructureTypeSize;
                output[0].Type = StructureDNA.StructureList[block.SDNAIndex].StructureTypeName;
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

        private byte[] subArray(byte[] original, int start, int length)
        {
            byte[] output = new byte[length];
            for(int i = 0; i < length; i++)
                output[i] = original[start + i];
            return output;
        }

        private int getIntFromArrayName(string name)
        {
            int start = name.IndexOf('[');
            int end = name.IndexOf(']');
            string numberString = name.Substring(start + 1, end - 1 - start);
            int number = int.Parse(numberString);
            return number;
        }
    }

    /// <summary>
    /// Holds field info for a flattened structure.
    /// </summary>
    public struct FieldInfo
    {
        /// <summary>
        /// Value of the field in bytes of length Size.
        /// </summary>
        public byte[] Value;

        /// <summary>
        /// Flattened name of the field (ie "parent.child.value")
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Name of the field's type.
        /// </summary>
        public readonly string TypeName;

        /// <summary>
        /// Size of the field.
        /// </summary>
        public readonly short Size;

        /// <summary>
        /// Indicates if the field is a pointer, in which case the size will always be four.
        /// </summary>
        public readonly bool IsPointer;

        /// <summary>
        /// Indicates if the field is an array.
        /// </summary>
        public readonly bool IsArray;

        /// <summary>
        /// Length of all the members of the array (if it's not an array, this will be 1). If the array is multidimensional this will be 
        /// the value of all the dimensions multiplied.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Array of dimensions of the array (or { 1 } if it's not an array).
        /// </summary>
        public readonly int[] Dimensions;

        /// <summary>
        /// Indicates if the field is a multidimensional array.
        /// </summary>
        public bool IsMultidimensional { get { return Dimensions.Length > 1; } }

        /// <summary>
        /// Field's C# type (if applicable). If it's not a primitive, it will be FieldInfo.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Name of the type of structure containing the field.
        /// </summary>
        public readonly string ParentType;

        private int pointerSize;

        /// <summary>
        /// Creates a new FieldInfo struct.
        /// </summary>
        /// <param name="value">Value of the field.</param>
        /// <param name="name">Name of the field (like "*next" or "id").</param>
        /// <param name="type">Type of the field.</param>
        /// <param name="size">Size (in bytes) of the field.</param>
        /// <param name="parent">Name of the parents up to here (like "parent.child" or "parent").</param>
        /// <param name="parentType">Name of the type of the containing object.</param>
        /// <param name="pointerSize">Size of the pointers in the file containing this field</param>
        public FieldInfo(byte[] value, string name, string type, short size, string parent, string parentType, int pointerSize)
        {
            IsPointer = IsArray = false;
            Length  = 1;
            Dimensions = new[] { 1 };
            this.pointerSize = pointerSize;

            if(name[0] == '*')
            {
                IsPointer = true;
                name = name.Substring(1);
                if(name[0] == '*')
                    name = name.Substring(1);
                if(name[0] == '*')
                    throw new Exception("help");
            }

            if(name.Contains('['))
            {
                IsArray = true;
                List<int> dim = new List<int>();
                do
                {
                    int start = name.IndexOf('[');
                    int end = name.IndexOf(']');
                    string numberString = name.Substring(start + 1, end - 1 - start);
                    int number = int.Parse(numberString);
                    Length *= number;
                    dim.Add(number);
                    name = name.Substring(0, start) + name.Substring(end + 1);
                } while(name.Contains('['));
                Dimensions = dim.ToArray();
            }

            Name = (parent == string.Empty ? "" : parent + ".") + name;

            if(Dimensions[0] != 1)
                for(int i = 0; i < Dimensions.Length; i++)
                    Name += "[" + Dimensions[i] + "]";

            TypeName = type;
            Size = size;
            Value = value;
            ParentType = parentType;
            if(!Name.Contains('.'))
                ParentType = "(this)";

            if(type == "char")
                Type = typeof(sbyte);
            else if(type == "uchar")
                Type = typeof(byte);
            else if(type == "short")
                Type = typeof(short);
            else if(type == "ushort")
                Type = typeof(ushort);
            else if(type == "int")
                Type = typeof(int);
            else if(type == "long") // blender longs are only 32 bits
                Type = typeof(int);
            else if(type == "ulong")
                Type = typeof(uint);
            else if(type == "float")
                Type = typeof(float);
            else if(type == "double")
                Type = typeof(double);
            else if(type == "int64_t")
                Type = typeof(long);
            else if(type == "uint64_t")
                Type = typeof(ulong);
            else if(type == "void") // probably a pointer
                Type = typeof(void);
            else
                Type = typeof(FieldInfo);
        }

        #region string ops
        /// <summary>
        /// Converts the value of the field to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            FieldInfo field = this; // lazy lol

            if(field.IsPointer)
            {
                if(field.IsArray)
                    return field.GetValueAsPointerArray();
                else
                    return field.GetValueAsPointer();
            }
            if(field.IsMultidimensional)
                return print2DArray(field.GetValueAsMultidimensionalArray(), field.Dimensions[0], field.Dimensions[1]);

            switch(field.TypeName)
            {
                case "char":
                    if(field.IsArray)
                        return "\'" + new string(field.GetValueAsCharArray()).Split('\0')[0] + "\'" + getAlternateCharArray(field);
                    else
                    {
                        char output = field.GetValueAsChar();
                        return "\'" + (Convert.ToSByte(output) < 32 ? "" : output.ToString()) + "\' (0x" + Convert.ToSByte(output).ToString("X2") + ")";
                    }
                case "uchar":
                    if(field.IsArray)
                        return printArray(field.GetValueAsUCharArray(), field.Length);
                    else
                        return field.GetValueAsUChar().ToString();
                case "short":
                    if(field.IsArray)
                        return printArray(field.GetValueAsShortArray(), field.Length);
                    else
                        return field.GetValueAsShort().ToString();
                case "ushort":
                    if(field.IsArray)
                        return printArray(field.GetValueAsUShortArray(), field.Length);
                    else
                        return field.GetValueAsUShort().ToString();
                case "int":
                case "long":
                    if(field.IsArray)
                        return printArray(field.GetValueAsIntArray(), field.Length);
                    else
                        return field.GetValueAsInt().ToString();
                case "ulong":
                    if(field.IsArray)
                        return printArray(field.GetValueAsUIntArray(), field.Length);
                    else
                        return field.GetValueAsUInt().ToString();
                case "int64_t":
                    if(field.IsArray)
                        return printArray(field.GetValueAsLongArray(), field.Length);
                    else
                        return field.GetValueAsLong().ToString();
                case "uint64_t":
                    if(field.IsArray)
                        return printArray(field.GetValueAsULongArray(), field.Length);
                    else
                        return field.GetValueAsULong().ToString();
                case "float":
                    if(field.IsArray)
                        return printArray(field.GetValueAsFloatArray(), field.Length);
                    else
                        return field.GetValueAsFloat().ToString();
                case "double":
                    if(field.IsArray)
                        return printArray(field.GetValueAsDoubleArray(), field.Length);
                    else
                        return field.GetValueAsDouble().ToString();
            }
            throw new InvalidOperationException("FieldInfo is confusing.");
        }

        private static string printArray(Array a, int x)
        {
            string s = "{ ";

            for(int i = 0; i < x; i++)
                s += a.GetValue(i).ToString() + ", ";

            s = s.Substring(0, s.Length - 2);
            s += " }";
            return s;
        }

        private static string print2DArray(Array a, int x, int y)
        {
            string s = "{ ";

            for(int i = 0; i < x; i++)
            {
                s += "{ ";
                for(int j = 0; j < y; j++)
                    s += a.GetValue(i, j).ToString() + ", ";
                s = s.Substring(0, s.Length - 2);
                s += " }, ";
            }

            s = s.Substring(0, s.Length - 3);
            s += " }";
            return s;
        }

        private static string getAlternateCharArray(FieldInfo f)
        {
            if(f.Dimensions[0] > 63)
                return "";

            char[] array = f.GetValueAsCharArray();
            string s = " ({ ";
            for(int i = 0; i < array.Length; i++)
                s += Convert.ToSByte(array[i]) + ", ";

            s = s.Substring(0, s.Length - 2);
            s += " })";
            return s;
        }
        #endregion

        public string GetValueAsPointer()
        {
            if(IsArray) // others will be checked in the next function
                throw new InvalidOperationException("This field isn't a pointer.");

            return getValueAsPointer(0);
        }

        private string getValueAsPointer(int index)
        {
            if(Size != pointerSize || !IsPointer)
                throw new InvalidOperationException("This field isn't a pointer.");

            string hex;
            if(pointerSize == 4)
                hex = "0x" + BitConverter.ToUInt32(Value, index).ToString("X" + (pointerSize * 2));
            else
                hex = "0x" + BitConverter.ToUInt64(Value, index).ToString("X" + (pointerSize * 2));

            if(hex == "0x00000000" || hex == "0x0000000000000000")
                return "0x0";
            return hex;
        }

        /// <summary>
        /// This method doesn't support 3D arrays or above. Sorry.
        /// </summary>
        /// <returns></returns>
        public string GetValueAsPointerArray()
        {
            if(Size != pointerSize || !IsPointer || !IsArray)
                throw new InvalidOperationException("This field isn't an array of pointers.");

            string s = "{ ";
            if(Dimensions.Length == 1)
            {
                for(int i = 0; i < Dimensions[0]; i++)
                    s += getValueAsPointer(i * pointerSize) + ", ";
                s = s.Substring(0, s.Length - 2);
            }
            else
            {
                for(int i = 0; i < Dimensions[0]; i++)
                {
                    s += "{ ";
                    for(int j = 0; j < Dimensions[1]; j++)
                        s += BitConverter.ToUInt32(Value, (i * Dimensions[1] + j) * pointerSize).ToString("X") + ", ";
                    s = s.Substring(0, s.Length - 3);
                    s += " }, ";
                }
                s = s.Substring(0, s.Length - 3);
            }
            s += " }";
            return s;
        }

        /// <summary>
        /// This method doesn't support 3D arrays and above. Sorry.
        /// </summary>
        /// <returns>A 2D System.Array filled with things.</returns>
        public Array GetValueAsMultidimensionalArray()
        {
            if(!IsArray || Type == typeof(FieldInfo) || IsPointer ||
                Dimensions.Length == 1)
                throw new InvalidOperationException("This field isn't a multidimensional array (or if it is, it's of pointers).");

            Array output = Array.CreateInstance(Type, Dimensions);
            switch(TypeName)
            {
                case "char":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(Encoding.ASCII.GetChars(Value, (i * Dimensions[1] + j), 1)[0], i, j);
                    break;
                case "uchar":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(Value[i * Dimensions[1] + j], i, j);
                    break;
                case "short":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToInt16(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                case "ushort":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToUInt16(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                case "int":
                case "long":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToInt32(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                case "ulong":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToUInt32(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                case "int64_t":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToInt64(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                case "uint64_t":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToUInt64(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                case "float":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToSingle(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                case "double":
                    for(int i = 0; i < Dimensions[0]; i++)
                        for(int j = 0; j < Dimensions[1]; j++)
                            output.SetValue(BitConverter.ToDouble(Value, (i * Dimensions[1] + j)), i, j);
                    break;
                default: throw new InvalidOperationException("Help.");
            }

            return output;
        }

        #region char (sbyte)
        public char GetValueAsChar()
        {
            if(IsArray || Type != typeof(sbyte) || IsPointer)
                throw new InvalidOperationException("This field isn't a char.");
            
            return Encoding.ASCII.GetChars(Value, 0, 1)[0];
        }

        public char[] GetValueAsCharArray()
        {
            if(!IsArray || Type != typeof(sbyte) || IsPointer)
                throw new InvalidOperationException("This field isn't a char[].");

            char[] data = new char[Length];
            for(int i = 0; i < (Length * Size); i++)
                data[i] = Encoding.ASCII.GetChars(Value, i, 1)[0];

            return data;
        }
        #endregion

        #region uchar (byte)
        public byte GetValueAsUChar()
        {
            if(IsArray || Type != typeof(byte) || IsPointer)
                throw new InvalidOperationException("This field isn't a uchar.");

            return Value[0];
        }

        public byte[] GetValueAsUCharArray()
        {
            if(!IsArray || Type != typeof(sbyte) || IsPointer)
                throw new InvalidOperationException("This field isn't a uchar[].");

            return Value;
        }
        #endregion

        #region short
        public short GetValueAsShort()
        {
            if(IsArray || Type != typeof(short) || IsPointer)
                throw new InvalidOperationException("This field isn't a short.");

            return BitConverter.ToInt16(Value, 0);
        }

        public short[] GetValueAsShortArray()
        {
            if(!IsArray || Type != typeof(short) || IsPointer)
                throw new InvalidOperationException("This field isn't a short[].");

            short[] data = new short[Length];
            for(int i = 0; i < (Length * Size); i += Size)
                data[i / Size] = BitConverter.ToInt16(Value, i);

            return data;
        }
        #endregion

        #region ushort
        public ushort GetValueAsUShort()
        {
            if(IsArray || Type != typeof(ushort) || IsPointer)
                throw new InvalidOperationException("This field isn't a ushort.");

            return BitConverter.ToUInt16(Value, 0);
        }

        public ushort[] GetValueAsUShortArray()
        {
            if(!IsArray || Type != typeof(ushort) || IsPointer)
                throw new InvalidOperationException("This field isn't a ushort[].");

            ushort[] data = new ushort[Length];
            for(int i = 0; i < (Length * Size); i += 2)
                data[i] = BitConverter.ToUInt16(Value, i);

            return data;
        }
    #endregion

        #region int
        public int GetValueAsInt()
        {
            if(IsArray || Type != typeof(int) || IsPointer)
                throw new InvalidOperationException("This field isn't an int.");

            return BitConverter.ToInt32(Value, 0);
        }

        public int[] GetValueAsIntArray()
        {
            if(!IsArray || Type != typeof(int) || IsPointer)
                throw new InvalidOperationException("This field isn't a int[].");

            int[] data = new int[Length];
            for(int i = 0; i < (Length * Size); i += Size)
                data[i / Size] = BitConverter.ToInt32(Value, i);

            return data;
        }
        #endregion

        #region uint
        public uint GetValueAsUInt()
        {
            if(IsArray || (Type != typeof(uint) && !IsPointer))// || IsPointer) // 32-bit pointers are secretly uints so this is also okay
                throw new InvalidOperationException("This field isn't a uint or a 32-bit pointer.");

            return BitConverter.ToUInt32(Value, 0);
        }

        public uint[] GetValueAsUIntArray()
        {
            if(!IsArray || Type != typeof(uint) || IsPointer)
                throw new InvalidOperationException("This field isn't a uint[].");

            uint[] data = new uint[Length];
            for(int i = 0; i < (Length * Size); i += 4)
                data[i] = BitConverter.ToUInt32(Value, i);

            return data;
        }
        #endregion

        #region long
        public long GetValueAsLong()
        {
            if(IsArray || Type != typeof(long) || IsPointer)
                throw new InvalidOperationException("This field isn't a long.");

            return BitConverter.ToInt64(Value, 0);
        }

        public long[] GetValueAsLongArray()
        {
            if(!IsArray || Type != typeof(long) || IsPointer)
                throw new InvalidOperationException("This field isn't a int[].");

            long[] data = new long[Length];
            for(int i = 0; i < (Length * Size); i += 8)
                data[i] = BitConverter.ToInt64(Value, i);

            return data;
        }
        #endregion

        #region ulong
        public ulong GetValueAsULong()
        {
            if(IsArray || (Type != typeof(ulong) && !IsPointer))// || IsPointer) // 64-bit pointers are secretly ulongs so this is okay
                throw new InvalidOperationException("This field isn't a ulong or a 64-bit pointer.");

            return BitConverter.ToUInt64(Value, 0);
        }

        public ulong[] GetValueAsULongArray()
        {
            if(!IsArray || Type != typeof(ulong) || IsPointer)
                throw new InvalidOperationException("This field isn't a uint[].");

            ulong[] data = new ulong[Length];
            for(int i = 0; i < (Length * Size); i += 8)
                data[i] = BitConverter.ToUInt64(Value, i);

            return data;
        }
        #endregion

        #region float
        public float GetValueAsFloat()
        {
            if(IsArray || Type != typeof(float) || IsPointer)
                throw new InvalidOperationException("This field isn't a float.");

            return BitConverter.ToSingle(Value, 0);
        }

        public float[] GetValueAsFloatArray()
        {
            if(!IsArray || Type != typeof(float) || IsPointer)
                throw new InvalidOperationException("This field isn't a float[].");

            float[] data = new float[Length];
            for(int i = 0; i < (Length * Size); i += Size)
                data[i / Size] = BitConverter.ToSingle(Value, i);

            return data;
        }
        #endregion

        #region double
        public double GetValueAsDouble()
        {
            if(IsArray || Type != typeof(double) || IsPointer)
                throw new InvalidOperationException("This field isn't a double.");

            return BitConverter.ToDouble(Value, 0);
        }

        public double[] GetValueAsDoubleArray()
        {
            if(!IsArray || Type != typeof(double) || IsPointer)
                throw new InvalidOperationException("This field isn't a double[].");

            double[] data = new double[Length];
            for(int i = 0; i < (Length * Size); i += Size)
                data[i / Size] = BitConverter.ToDouble(Value, i);

            return data;
        }
        #endregion
    }
}
