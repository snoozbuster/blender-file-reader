using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlenderFileReader
{
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
        public BlenderType(string name, short size, StructureDNA s)
        {
            Name = name;
            Size = size;

            int index = s.TypeNameList.IndexOf(name);
            IsPrimitive = s.StructureTypeIndices.IndexOf((short)index) == -1; // not found means primitive
        }
    }
}
