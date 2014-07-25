using BlenderFileReader;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderTypes
{
    /// <summary>
    /// Represents all the available information about a processed and interpreted Blender file.
    /// Not to be confused with BlenderFileReader.BlenderFile, which is a representation of the raw data.
    /// </summary>
    public class BlendFile
    {
        /// <summary>
        /// An array of all the scenes contained in the file. These are not guaranteed to be in the same order
        /// as in the file.
        /// </summary>
        public Scene[] Scenes { get; private set; }

        /// <summary>
        /// Returns the scene that was current when the file was saved.
        /// May return null, although I'm not sure that's actually possible given the file structure.
        /// </summary>
        public Scene CurrentScene { get { return currentSceneIndex == -1 ? null : Scenes[currentSceneIndex]; } }
        internal int currentSceneIndex = -1;

        /// <summary>
        /// Returns the full filename as recorded in the file. May not exist on the current computer.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Returns the version of Blender this file was saved in, as a string.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Returns the revision number of the file. 
        /// (I've never seen this as anything but 0, although I assume there's a way to increment it.)
        /// </summary>
        public int Revision { get; private set; }

        internal BlendFile(BlenderFile file)
        {
            Version = file.VersionNumber;

            PopulatedStructure fileGlobal = file.GetStructuresOfType("FileGlobal")[0];
            Filename = new string(fileGlobal["filename"].GetValueAsCharArray()).Split('\0')[0];
            Revision = fileGlobal["revision"].GetValueAsInt();

            ulong curSceneAddr = fileGlobal["curscene"].GetValueAsPointer();
            PopulatedStructure[] scenes = file.GetStructuresOfType("Scene");
            int i = 0;
            Scenes = new Scene[scenes.Length];
            foreach(PopulatedStructure scene in scenes)
            {
                if(scene.ContainingBlock.OldMemoryAddress == curSceneAddr)
                    currentSceneIndex = i;
                Scenes[i++] = new Scene(file, scene);
            }
        }
    }
}
