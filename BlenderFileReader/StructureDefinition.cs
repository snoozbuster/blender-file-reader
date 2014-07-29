using System;
using System.Collections.Generic;

namespace BlenderFileReader
{
    /// <summary>
    /// Represents a structure as defined by SDNA. 
    /// </summary>
    public struct StructureDefinition
    {
        private List<FieldDefinition> fieldDictionary;
        private short structureIndexType;
        private StructureDNA sdna;

        /// <summary>
        /// Type (as defined by SDNA) of the structure.
        /// </summary>
        public TypeDefinition StructureType { get { return sdna.TypeList[structureIndexType]; } }
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
        public List<FieldDefinition> FieldList { get { return new List<FieldDefinition>(fieldDictionary); } }

        private bool isInitialized;

        /// <summary>
        /// Creates a new structure as defined by SDNA.
        /// </summary>
        /// <param name="structureType">Index in SDNA.TypeList of the structure type.</param>
        /// <param name="fieldDict">List of fields in the structure.</param>
        public StructureDefinition(short structureType, List<FieldDefinition> fieldDict, StructureDNA sdna)
        {
            fieldDictionary = fieldDict;
            structureIndexType = structureType;
            isInitialized = false;
            this.sdna = sdna;

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

            foreach(FieldDefinition f in fieldDictionary)
                if(!f.IsPointer)
                    f.InitializeStructure();
        }
    }
}
