using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace BlenderFileReader
{
    /// <summary>
    /// Holds field info for a primitive field.
    /// </summary>
    [DebuggerDisplay("{FullyQualifiedName}: {ToString(),nq}")] // nq means "no quotes", without it the value returned by ToString() will be quoted.
    public class Field<T> : IField
    {
        /// <summary>
        /// Value contained in the field.
        /// </summary>
        public dynamic Value { get; private set; }

        /// <summary>
        /// Name of the field. Should not contain array dimensions or dots.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Fully qualified name of the field using dot notation.
        /// </summary>
        public string FullyQualifiedName
        {
            get
            {
                string name = Name;
                IField parent = Parent;
                while(parent.Parent != null) // stop before adding name of "root" structure, which will just be the name of its type
                {
                    name = parent.Name + "." + name;
                    parent = parent.Parent;
                }
                return name;
            }
        }

        /// <summary>
        /// Size of the field. If the field is a struct, this will be the sum of all contained fields.
        /// </summary>
        public short Size { get; private set; }

        /// <summary>
        /// Indicates if the field is a primitive; or, equivalently, indicates if was instantiated with a
        /// primitive type name.
        /// </summary>
        public bool IsPrimitive { get; private set; }

        /// <summary>
        /// Indicates if the field is a pointer.
        /// </summary>
        public bool IsPointer { get; private set; }

        /// <summary>
        /// Indicates if the field is a pointer to a pointer.
        /// </summary>
        public bool IsPointerToPointer { get; private set; }

        /// <summary>
        /// Indicates if the field is an array.
        /// </summary>
        public bool IsArray { get; private set; }

        /// <summary>
        /// Indicates if the field is a 2D array.
        /// </summary>
        public bool Is2DArray { get; private set; }

        /// <summary>
        /// Field's parent (if any).
        /// </summary>
        public IField Parent { get; private set; }

        /// <summary>
        /// Field's parent's type. If Parent is null, returns "(this)".
        /// </summary>
        public string ParentType { get { return Parent.TypeName; } }

        /// <summary>
        /// Name of the field's type.
        /// </summary>
        public string TypeName { get; private set; }

        /// <summary>
        /// If the field is an array, returns the number of items in the field. Otherwise, returns 1.
        /// </summary>
        public int Length { get; private set; }

        // holds pointer size for this field
        private int pointerSize;

        /// <summary>
        /// Creates a new Field.
        /// </summary>
        /// <param name="value">Value of the field.</param>
        /// <param name="name">Name of the field (like "*next" or "id" or "mat[4][4]").</param>
        /// <param name="type">Type of the field.</param>
        /// <param name="size">Size (in bytes) of the field.</param>
        /// <param name="parent">Parent of the field. Cannot be null.</param>
        /// <param name="pointerSize">Size of the pointers in the file containing this field</param>
        public Field(dynamic value, string name, string type, short size, IField parent, int pointerSize)
        {
            if(parent == null)
                throw new ArgumentNullException("parent");

            this.pointerSize = pointerSize;
            IsPointer = IsPointerToPointer = IsArray = false;
            Length = 1;

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
                    throw new Exception("Don't know what to do with a pointer to a pointer to a pointer");
            }

            IsArray = name.Count(c => c == '[') != 0;
            Is2DArray = name.Count(c => c == '[') == 2;
            if(name.Contains('['))
            {
                Length = int.Parse(name.Split('[')[1].Split(']')[0]);
                if(Is2DArray)
                    Length *= int.Parse(name.Split('[')[2].Split(']')[0]);
            }

            // not found means primitive
            IsPrimitive = (parent as Structure).ContainingFile.StructureDNA.StructureTypeIndices.IndexOf((short)(parent as Structure).ContainingFile.StructureDNA.TypeNameList.IndexOf(type)) == -1;

            Name = name.Split('[')[0]; // get rid of array leftovers
            Parent = parent;
            TypeName = type;
            Size = size;
            Value = value;
        }

        /// <summary>
        /// If the represented field is a pointer, dereferences it and returns the pointed-to structure(s).
        /// If the field is not a pointer, throws <pre>InvalidOperationException</pre>. If the pointer is null,
        /// returns null. If the field is a pointer to a pointer or an array of pointers throws InvalidDataException.
        /// </summary>
        /// <returns>A <pre>Structure</pre> pointed to by the field, or null.</returns>
        public dynamic[] Dereference()
        {
            if(!IsPointer)
                throw new InvalidOperationException("This field is not a pointer.");
            if(IsPointerToPointer)
                throw new InvalidDataException("This field is a pointer to a pointer and cannot be converted to a Structure.");
            if(IsArray)
                throw new InvalidOperationException("This is an array of pointers. Use DereferenceAsArray().");

            ulong address = (ulong)Convert.ChangeType(Value, typeof(ulong));
            return address == 0 ? null : (Parent as Structure).ContainingFile.GetStructuresByAddress(address);
        }

        /// <summary>
        /// If the represented field is an array of pointers, dereferences them and returns an array of references.
        /// For null pointers, null is used. If the represented field is not an array of pointers, throws InvalidOperationException.
        /// If the field is a pointer to a pointer, throws InvalidDataException.
        /// </summary>
        /// <returns>An array of <pre>Structure</pre>s.</returns>
        public dynamic[][] DereferenceAsArray()
        {
            if(!IsPointer)
                throw new InvalidOperationException("This field is not a pointer.");
            if(IsPointerToPointer)
                throw new InvalidDataException("This field is a pointer to a pointer and cannot be converted to a Structure.");
            if(!IsArray)
                throw new InvalidOperationException("This is not an array of pointers. Use Dereference().");

            // have to use dynamic here too
            dynamic addresses = Value;
            Structure[][] output = new Structure[addresses.Length][];
            for(int i = 0; i < output.Length; i++)
                output[i] = addresses[i] == 0 ? null : (Parent as Structure).ContainingFile.GetStructuresByAddress(addresses[i]);
            return output;
        }

        /// <summary>
        /// Converts the value of the field to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // This method heavily uses dynamic to skip compile-time checking so that we don't have to deal with
            // tons of reflection and casting.
            dynamic val = Value;
            Type type = val.GetType();
            if(IsPointer) // T is ulong, ulong[], or ulong[][]
            {
                string output;
                if(type.IsArray) // T is ulong[] or ulong[][]
                {
                    // I have to cast the dynamic variable to give the runtime a hint about where to find
                    // Select; since Select is an extension method. Without the cast, it will look for Select
                    // on System.Array and fail to find it.
                    if(type == typeof(ulong[]))
                        output = "{ " + string.Join(", ", (val as IEnumerable<ulong>).Select(u => "0x" + u.ToString("X" + (pointerSize * 2)))) + " }";
                    else
                        output = "{ " + string.Join(", ", (val as IEnumerable<ulong[]>).Select(a => { return "{ " + string.Join(", ", a.Select(u => "0x" + u.ToString("X" + (pointerSize * 2)))) + " }"; })) + " }";
                }
                else // T is ulong
                    output = "0x" + val.ToString("X" + pointerSize * 2);
                return pointerSize == 4 ? output.Replace("0x00000000", "0x0") : output.Replace("0x0000000000000000", "0x0");
            }
            else if(type.IsArray) // some sort of non-pointer array business
            {
                // special handling for char arrays
                if(type == typeof(char[]))
                {
                    char[] temp = val;
                    return "\'" + new string(temp).Split('\0')[0] + "\'" + (temp.Length > 63 ? "" : " ({ " + string.Join(", ", temp.Select(c => Convert.ToSByte(c))) + " })");
                }
                // although we can use select and join, there would be some weird tricks that would have to happen
                // if it was a 2D array. It's easier to just do it this way.
                string output = "{ ";
                foreach(var obj in val)
                {
                    IEnumerable val2 = obj as IEnumerable;
                    if(val2 != null)
                    {
                        output += "{ ";
                        foreach(var obj2 in val2)
                            output += obj2.ToString() + ", ";
                        output = output.Substring(0, output.Length - 2);
                        output += " }, ";
                    }
                    else
                        output += obj.ToString() + ", ";
                }
                output = output.Substring(0, output.Length - 2);
                output += " }";
                return output;
            }
            else if(type == typeof(char)) // special handling for chars
            {
                sbyte value = Convert.ToSByte(val);
                return "\'" + (value < 32 ? "" : val.ToString()) + "\' (0x" + value.ToString("X2") + ")";
            }
            else
                return Value.ToString();
        }

        public static implicit operator T(Field<T> field)
        {
            return (T)field.Value;
        }
    }
}
