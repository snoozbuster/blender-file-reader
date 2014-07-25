using BlenderFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderTypes
{
    // To whom it may concern: there is a lot of data stored in a Scene. For the most part,
    // I didn't find much of it interesting, but a lot of Blender Game Engine settings and audio 
    // settings are stored here. If you are interested in letting your designers tweak those kinds of
    // settings, feel free to add the things you're interested in.
    // Additionally, there's an entire object called World that has world-related settings that I didn't
    // include. The reason I haven't included these things is because I don't want to spend time figuring
    // out exactly what they all do. 

    /// <summary>
    /// Contains information about a single scene contained in a .blend file.
    /// </summary>
    public class Scene
    {
        /// <summary>
        /// Returns the name of the scene.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns a list of objects in this scene. 
        /// </summary>
        public BlenderObject[] Objects { get; private set; }

        /// <summary>
        /// Returns the object referenced by the "<pre>basact</pre>" field in the scene. This may be the
        /// currently selected object; I'm not really sure. It may be useful to someone.
        /// May return null.
        /// </summary>
        public BlenderObject ActiveObject { get { return activeObjectIndex == -1 ? null : Objects[activeObjectIndex]; } }
        internal int activeObjectIndex = -1;

        internal Scene(BlenderFile file, PopulatedStructure scene)
        {
            Name = new string(scene["id.name"].GetValueAsCharArray()).Split('\0')[0].Substring(2);

            // todo: add lamp importing here
            string[] validTypeNames = new[] { "Mesh" };
            List<BlenderObject> objects = new List<BlenderObject>();
            int i = 0;

            ulong next = scene["base.first"].GetValueAsPointer();
            ulong basact = scene["basact"].GetValueAsPointer(); // this is the address of the Base object
            while(next != 0)
            {
                if(next == basact)
                    activeObjectIndex = i;

                PopulatedStructure objBase = file.GetStructuresByAddress(next)[0];
                PopulatedStructure obj = file.GetStructuresByAddress(objBase["object"].GetValueAsPointer())[0];
                Field data = obj["data"];
                ulong ptr = data.GetValueAsPointer(); // this will be 0 for objects of the Empty type
                if(ptr == 0)
                    objects.Add(new BlenderObject(file, obj));
                else
                {
                    int SDNAIndex = file.GetBlockByAddress(ptr).SDNAIndex;
                    if(validTypeNames.Contains(file.StructureDNA.StructureList[SDNAIndex].StructureTypeName))
                        objects.Add(new BlenderObject(file, obj));
                }
                
                next = objBase["next"].GetValueAsPointer();
                i++;
            }
        }
    }
}
