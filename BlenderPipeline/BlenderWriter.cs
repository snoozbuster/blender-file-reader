using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace BlenderPipeline
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    /// </summary>
    [ContentTypeWriter]
    public class BlenderWriter : ContentTypeWriter<BlendFile>
    {
        /// <summary>
        /// Version of the output file. Update this when you update anything called by <pre>Write()</pre>.
        /// </summary>
        protected const int FileVersion = 1;

        protected override void Write(ContentWriter output, BlendFile value)
        {
            //output.Write(FileVersion);
            output.WriteObject(value);
            //writeBlendFile(output, value);
        }

        private void writeBlendFile(ContentWriter output, BlendFile value)
        {
            throw new NotImplementedException();
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(BlendFile).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(BlenderWriter).AssemblyQualifiedName;
        }
    }
}
