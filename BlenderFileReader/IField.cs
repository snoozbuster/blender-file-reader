using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlenderFileReader
{
    /// <summary>
    /// Represents a single weakly-typed field in a structure. 
    /// </summary>
    public interface IField
    {
        /// <summary>
        /// Value contained in the field. Use this sparingly.
        /// </summary>
        dynamic Value { get; }

        /// <summary>
        /// Name of the field. Should not contain array dimensions or dots.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Fully qualified name of the field using dot notation.
        /// </summary>
        string FullyQualifiedName { get; }

        /// <summary>
        /// Size of the field. If the field is a struct, this will be the sum of all contained fields.
        /// </summary>
        short Size { get; }

        /// <summary>
        /// Indicates if the field is a primitive; or, equivalently, indicates if was instantiated with a
        /// primitive type name.
        /// </summary>
        bool IsPrimitive { get; }

        /// <summary>
        /// Indicates if the field is a pointer.
        /// </summary>
        bool IsPointer { get; }

        /// <summary>
        /// Indicates if the field is a pointer to a pointer.
        /// </summary>
        bool IsPointerToPointer { get; }

        /// <summary>
        /// Indicates if the field is an array.
        /// </summary>
        bool IsArray { get; }

        /// <summary>
        /// Indicates if the field is a 2D array.
        /// </summary>
        bool Is2DArray { get; }

        /// <summary>
        /// If the field is an array, returns the number of items in the field. Otherwise, returns 1.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Field's parent (if any).
        /// </summary>
        IField Parent { get; }

        /// <summary>
        /// Field's parent's type. If Parent is null, returns "(this)".
        /// </summary>
        string ParentType { get; }

        /// <summary>
        /// Name of the field's type.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// If the represented field is a pointer, dereferences it and returns the pointed-to structure(s).
        /// If the field is not a pointer, throws <pre>InvalidOperationException</pre>. If the pointer is null,
        /// returns null. If the field is a pointer to a pointer or an array of pointers throws InvalidDataException.
        /// </summary>
        /// <returns>A <pre>PopulatedStructure</pre> pointed to by the field, or null.</returns>
        PopulatedStructure[] Dereference();

        /// <summary>
        /// If the represented field is an array of pointers, dereferences them and returns an array of references.
        /// For null pointers, null is used. If the represented field is not an array of pointers, throws InvalidOperationException.
        /// If the field is a pointer to a pointer, throws InvalidDataException.
        /// </summary>
        /// <returns>An array of <pre>PopulatedStructure</pre>s.</returns>
        PopulatedStructure[][] DereferenceAsArray();
    }

    /// <summary>
    /// Represents a single strongly-typed field in Blender. Use IStructField if the field contains a struct.
    /// </summary>
    /// <typeparam name="T">T should be specified as a primitive; where the type of <pre>string</pre> is a special case
    /// denoting a Blender-defined struct; in which case the value should be the SDNA type name.</typeparam>
    public interface IField<T> : IField
    {
        /// <summary>
        /// Value contained in the field.
        /// </summary>
        new T Value { get; }
    }

    /// <summary>
    /// Represents a field that contains a struct. The struct is represented as a list of <pre>IField</pre>s.
    /// </summary>
    public interface IStructField : IField<string>
    {
        /// <summary>
        /// A list of fields contained in this structure.
        /// </summary>
        IField[] Fields { get; }

        /// <summary>
        /// Array-style access to fields.
        /// </summary>
        /// <param name="identifier">Name of the field you want to look up.</param>
        /// <returns>IField object representing the field with that identifier</returns>
        IField this[string identifier] { get; }

        /// <summary>
        /// A count of the number of fields in this structure and all contained structures.
        /// </summary>
        int NumFields { get; }
    }
}
