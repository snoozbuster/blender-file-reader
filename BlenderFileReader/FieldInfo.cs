using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlenderFileReader
{
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
        /// Indicates if the field is a pointer, in which case the size will always be <pre>pointerSize</pre>.
        /// </summary>
        public readonly bool IsPointer;

        /// <summary>
        /// Indicates if the field is a pointer to a pointer.
        /// </summary>
        public readonly bool IsPointerToPointer;

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

        // holds pointer size for this field
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
            IsPointer = IsArray = IsPointerToPointer = false;
            Length = 1;
            Dimensions = new[] { 1 };
            this.pointerSize = pointerSize;

            if(name[0] == '*')
            {
                IsPointer = true;
                name = name.Substring(1);
                if(name[0] == '*')
                {
                    IsPointerToPointer = true;
                    name = name.Substring(1);
                }
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
                    return field.GetValueAsPointerString();
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
            if(field.Size == 0)
                return "nothing";

            throw new InvalidOperationException("FieldInfo is confusing.");
        }

        // helper function to print an array
        private static string printArray(Array a, int x)
        {
            string s = "{ ";

            for(int i = 0; i < x; i++)
                s += a.GetValue(i).ToString() + ", ";

            s = s.Substring(0, s.Length - 2);
            s += " }";
            return s;
        }

        // helper function to print a 2D array
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

        /// <summary>
        /// Gets the field as a hexadecimal pointer string. Fails if field isn't a pointer.
        /// </summary>
        /// <returns></returns>
        public string GetValueAsPointerString()
        {
            if(IsArray) // others will be checked in the next function
                throw new InvalidOperationException("This field isn't a pointer.");

            return getValueAsPointer(0);
        }

        // helper function for pointer-getting
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
        /// Gets the field as either a 4-byte pointer or an 8-byte pointer, depending on the file.
        /// </summary>
        /// <returns></returns>
        public ulong GetValueAsPointer()
        {
            if(Size != pointerSize || !IsPointer || IsArray)
                throw new InvalidOperationException("This field isn't a pointer.");

            return pointerSize == 4 ? BitConverter.ToUInt32(Value, index) : BitConverter.ToUInt64(Value, index);
        }

        /// <summary>
        /// Gets the field as an array of hexadecimal pointers. Fails if field isn't an array of pointers.
        /// </summary>
        /// <returns></returns>
        public string GetValueAsPointerArray()
        {
            if(Size != pointerSize || !IsPointer || !IsArray)
                throw new InvalidOperationException("This field isn't an array of pointers.");

            // todo: this could be redone to use string.Join()
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
        /// Gets the value of the field as a 2D System.Array. Fails if the field isn't a 2D array or is a 2D pointer array.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets field as a one byte char (signed). Fails if the field isn't a char.
        /// </summary>
        /// <returns></returns>
        public char GetValueAsChar()
        {
            if(IsArray || Type != typeof(sbyte) || IsPointer)
                throw new InvalidOperationException("This field isn't a char.");

            return Encoding.ASCII.GetChars(Value, 0, 1)[0];
        }

        /// <summary>
        /// Gets field as an array of one-byte chars. Fails if the fiels isn't an array of one-byte chars.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as a one-byte uchar (unsigned byte). Fails if the field isn't a one-byte uchar.
        /// </summary>
        /// <returns></returns>
        public byte GetValueAsUChar()
        {
            if(IsArray || Type != typeof(byte) || IsPointer)
                throw new InvalidOperationException("This field isn't a uchar.");

            return Value[0];
        }

        /// <summary>
        /// Gets the value of the field as an array of one-byte uchars. Fails if the field isn't an array of one-byte uchars.
        /// </summary>
        /// <returns></returns>
        public byte[] GetValueAsUCharArray()
        {
            if(!IsArray || Type != typeof(sbyte) || IsPointer)
                throw new InvalidOperationException("This field isn't a uchar[].");

            return Value;
        }
        #endregion

        #region short
        /// <summary>
        /// Gets the value of the field as a signed short (two bytes). Fails if the field isn't a short.
        /// </summary>
        /// <returns></returns>
        public short GetValueAsShort()
        {
            if(IsArray || Type != typeof(short) || IsPointer)
                throw new InvalidOperationException("This field isn't a short.");

            return BitConverter.ToInt16(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of shorts (signed, two bytes). Fails if the field isn't an array of shorts.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as an unsigned short (two bytes). Fails if the field isn't a ushort.
        /// </summary>
        /// <returns></returns>
        public ushort GetValueAsUShort()
        {
            if(IsArray || Type != typeof(ushort) || IsPointer)
                throw new InvalidOperationException("This field isn't a ushort.");

            return BitConverter.ToUInt16(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of unsigned shorts (two bytes each). Fails if the field
        /// isn't an array of ushorts.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as a signed int (four bytes). Fails if the field isn't an int.
        /// Often used for 32-bit pointers.
        /// </summary>
        /// <returns></returns>
        public int GetValueAsInt()
        {
            if(IsArray || Type != typeof(int) || IsPointer)
                throw new InvalidOperationException("This field isn't an int.");

            return BitConverter.ToInt32(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of signed ints (four bytes). Fails if the field isn't 
        /// an array of ints.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as an unsigned int (four bytes). Fails if the field isn't a uint.
        /// </summary>
        /// <returns></returns>
        public uint GetValueAsUInt()
        {
            if(IsArray || (Type != typeof(uint) && !IsPointer))// || IsPointer) // 32-bit pointers are secretly uints so this is also okay
                throw new InvalidOperationException("This field isn't a uint or a 32-bit pointer.");

            return BitConverter.ToUInt32(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of unsigned ints (four bytes). Fails if the field isn't
        /// an array of uints.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as a signed long (8 bytes). Fails if the field isn't a long.
        /// </summary>
        /// <returns></returns>
        public long GetValueAsLong()
        {
            if(IsArray || Type != typeof(long) || IsPointer)
                throw new InvalidOperationException("This field isn't a long.");

            return BitConverter.ToInt64(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of signed longs (8 bytes). Fails if the field isn't an
        /// array of signed longs.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as an unsigned long (8 bytes). Fails if the field isn't an unsigned long.
        /// Often used for 64-bit pointers.
        /// </summary>
        /// <returns></returns>
        public ulong GetValueAsULong()
        {
            if(IsArray || (Type != typeof(ulong) && !IsPointer))// || IsPointer) // 64-bit pointers are secretly ulongs so this is okay
                throw new InvalidOperationException("This field isn't a ulong or a 64-bit pointer.");

            return BitConverter.ToUInt64(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of unsigned longs (8 bytes). Fails if the field isn't an
        /// array of ulongs.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as a single-precision float (four bytes). Fails if the field isn't a float.
        /// </summary>
        /// <returns></returns>
        public float GetValueAsFloat()
        {
            if(IsArray || Type != typeof(float) || IsPointer)
                throw new InvalidOperationException("This field isn't a float.");

            return BitConverter.ToSingle(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of single-precision floats (four bytes). Fails if the field
        /// isn't an array of floats.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Gets the value of the field as a double-precision float (eight bytes). Fails if the field isn't a double.
        /// </summary>
        /// <returns></returns>
        public double GetValueAsDouble()
        {
            if(IsArray || Type != typeof(double) || IsPointer)
                throw new InvalidOperationException("This field isn't a double.");

            return BitConverter.ToDouble(Value, 0);
        }

        /// <summary>
        /// Gets the value of the field as an array of double-precision floats (8 bytes). Fails if the field
        /// isn't an array of doubles.
        /// </summary>
        /// <returns></returns>
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
