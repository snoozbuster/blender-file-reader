using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderFileReader
{
    /// <summary>
    /// Represents a structure as defined by SDNA. 
    /// </summary>
    public struct SDNAStructure
    {
        private List<BlenderField> fieldDictionary;
        private short structureIndexType;

        /// <summary>
        /// Type (as defined by SDNA) of the structure.
        /// </summary>
        public BlenderType StructureType { get { return StructureDNA.TypeList[structureIndexType]; } }
        /// <summary>
        /// Name of the structure's type (shortcut to StructureType.Name).
        /// </summary>
        public string StructureTypeName { get { return StructureType.Name; } }
        /// <summary>
        /// Size of the structure's type (shortcut to StructureType.Size).
        /// </summary>
        public short StructureTypeSize { get { return StructureType.Size; } }
        /// <summary>
        /// List of fields in the structure.
        /// </summary>
        public List<BlenderField> FieldList { get { return new List<BlenderField>(fieldDictionary); } }

        private bool isInitialized;

        /// <summary>
        /// Creates a new structure as defined by SDNA.
        /// </summary>
        /// <param name="structureType">Index in SDNA.TypeList of the structure type.</param>
        /// <param name="fieldDict">List of fields in the structure.</param>
        public SDNAStructure(short structureType, List<BlenderField> fieldDict)
        {
            fieldDictionary = fieldDict;
            structureIndexType = structureType;
            isInitialized = false;

            if(StructureType.IsPrimitive)
                throw new ArgumentException("Type of structure is a primitive.");
        }

        /// <summary>
        /// Initializes all the non-primitive fields' Structure member.
        /// </summary>
        public void InitializeFields()
        {
            if(isInitialized)
                throw new InvalidOperationException("Already initialized.");
            isInitialized = true;

            foreach(BlenderField f in fieldDictionary)
                if(!f.IsPointer)
                    f.InitializeStructure();
        }
    }

    /// <summary>
    /// A type as defined by SDNA.
    /// </summary>
    public struct BlenderType
    {
        /// <summary>
        /// Name of the type.
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Size in bytes of the type.
        /// </summary>
        public readonly short Size;
        /// <summary>
        /// Indicates if this type is a primitive (non-primitive types are defined in the SDNA).
        /// </summary>
        public readonly bool IsPrimitive;

        /// <summary>
        /// Creates a new type as defined by SDNA.
        /// </summary>
        /// <param name="name">Name of the type.</param>
        /// <param name="size">Size of the type in bytes.</param>
        public BlenderType(string name, short size)
        {
            Name = name;
            Size = size;

            int index = StructureDNA.TypeNameList.IndexOf(name);
            IsPrimitive = StructureDNA.StructureTypeIndices.IndexOf((short)index) == -1; // not found means primitive
        }
    }

    /// <summary>
    /// A field of a structure as defined by SDNA.
    /// </summary>
    public struct BlenderField
    {
        /// <summary>
        /// Name of the field.
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Type (as defined by SDNA) of the field.
        /// </summary>
        public readonly BlenderType Type;

        /// <summary>
        /// Shortcut to Type.IsPrimitive; indicates if the field is a structure.
        /// </summary>
        public bool IsPrimitive { get { return Type.IsPrimitive; } }

        /// <summary>
        /// Indicates if the field is a pointer.
        /// </summary>
        public bool IsPointer { get { return Name[0] == '*'; } }

        /// <summary>
        /// Indicates if the field is an array.
        /// </summary>
        public bool IsArray { get { return Name.Contains('['); } }

        public SDNAStructure Structure { get { if(IsPrimitive) throw new InvalidOperationException(Name + " is a primitive and does not have an SDNAStructure."); if(structure == null) InitializeStructure(); return structure.Value; } }
        private SDNAStructure? structure;
        private bool isInitialized;

        /// <summary>
        /// Creates a new field.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="type">Type (as defined by SDNA) of the field.</param>
        public BlenderField(string name, BlenderType type)
        {
            Name = name;
            Type = type;
            structure = null;
            isInitialized = false;
        }

        /// <summary>
        /// Creates a new field.
        /// </summary>
        /// <param name="nameIndex">Index of SDNA.NameList containing the name of the field.</param>
        /// <param name="typeIndex">Index of SDNA.TypeList containing the type of the field.</param>
        public BlenderField(short nameIndex, short typeIndex)
        {
            Name = StructureDNA.NameList[nameIndex];
            Type = StructureDNA.TypeList[typeIndex];

            if(Type.Name.Count(v => { return v == '['; }) > 2)
                throw new Exception("A 3D array is present and this program is not set up to handle that.");

            isInitialized = false;
            structure = null;
        }

        /// <summary>
        /// If the field is non-primitive, this will populate Structure. Safe to call on primitives.
        /// </summary>
        public void InitializeStructure()
        {
            if(isInitialized)
                throw new InvalidOperationException("Can't initialize a field's structure twice.");
            if(IsPrimitive)
                return;
            isInitialized = true;

            string name = Type.Name; // can't use 'this'
            structure = StructureDNA.StructureList.Find(v => { return v.StructureTypeName == name; });
            structure.Value.InitializeFields();
        }
    }
}
