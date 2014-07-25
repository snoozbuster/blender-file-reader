using BlenderFileReader;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderTypes
{
    /// <summary>
    /// Holds information about an object, as Blender thinks of it.
    /// Currently supported object types are Empty and Mesh.
    /// </summary>
    public class BlenderObject
    {
        /// <summary>
        /// Contains the name of the object.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Contains the underlying data stored by the object. Current possible values are 
        /// <pre>Mesh</pre> and <pre>null</pre>.
        /// </summary>
        public object Data { get; private set; }

        /// <summary>
        /// Holds the type of the object contained in the Data field.
        /// </summary>
        public Type DataType { get; private set; }

        /// <summary>
        /// Holds a list of the materials applied to the object. May be empty.
        /// A warning: Materials may be bound to both meshes and objects, based on settings set in Blender.
        /// However, when a mesh and an object both refer to the same <pre>Material</pre>, only one instance
        /// of that material will reside in memory. 
        /// </summary>
        public Material[] Materials { get; private set; }
        
        /// <summary>
        /// Location of the object.
        /// </summary>
        public Vector3 Location { get; private set; }

        // todo: Requires investigation
        /// <summary>
        /// Origin of the object, as defined by Blender. 
        /// </summary>
        //public Vector3 Origin { get; private set; }

        /// <summary>
        /// Scale of the object.
        /// </summary>
        public Vector3 Scale { get; private set; }

        /// <summary>
        /// Rotation of the object in radians (I think). 
        /// </summary>
        public Vector3 AxisRotations { get; private set; }

        /// <summary>
        /// Rotation of the object as a quaternion.
        /// </summary>
        public Quaternion Rotation { get; private set; }

        /// <summary>
        /// Rotation of the object using axis-angle representation. Used with the <pre>RotationAngle</pre> field.
        /// </summary>
        public Vector3 RotationAxis { get; private set; }

        /// <summary>
        /// Rotation of the object using axis-angle representation. Used with the <pre>RotationAxis</pre> field.
        /// </summary>
        public float RotationAngle { get; private set; }

        // There's a good number of additional fields relating to game properties; as well as logic bricks.
        // Might be interesting to someone, but not especially interesting to me.

        internal BlenderObject(BlenderFile file, PopulatedStructure obj)
        {
            
        }
    }
}
