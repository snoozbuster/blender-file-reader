using BlenderFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderTypes
{
    public class Material
    {
        private Material(BlenderFile file, PopulatedStructure material)
        {

        }

        /// <summary>
        /// Creates a new <pre>Material</pre> with the given arguments, unless the <pre>Material</pre> was created already.
        /// If the <pre>Material</pre> was already created, returns a reference to the previously created <pre>Material</pre>.
        /// </summary>
        internal static Material GetOrCreateMaterial(BlenderFile file, PopulatedStructure material)
        {
            if(materialDict.Keys.Contains(material.ContainingBlock.OldMemoryAddress))
                return materialDict[material.ContainingBlock.OldMemoryAddress];

            Material m = new Material(file, material);
            materialDict.Add(material.ContainingBlock.OldMemoryAddress, m);
            return m;
        }

        /// <summary>
        /// Dictionary to avoid creating the same <pre>Material</pre> multiple times.
        /// </summary>
        private static Dictionary<ulong, Material> materialDict = new Dictionary<ulong, Material>();

    }
}
