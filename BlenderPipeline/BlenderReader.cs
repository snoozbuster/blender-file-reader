using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlenderPipeline
{
    /// <summary>
    /// This class will read the .xnb file into a BlendFile. Technically, it should go in the XNA project itself,
    /// but I like to have everything self-contained, and making it external makes it harder to use. Since you
    /// have to add a reference to BlenderPipeline to you main project anyway, it shouldn't matter.
    /// </summary>
    class BlenderReader : ContentTypeReader<BlendFile>
    {
        // The documentation is vague, but existingInstance isn't used by any current implementations of XNA.
        // It was intended for future-proofing, but was never implemented.
        // More info (retrieved 7/24/14): http://social.msdn.microsoft.com/Forums/en-US/87032bc0-c4ae-4cbd-a4df-b93eeeaf6063/some-missing-documentation?forum=xnaframework
        protected override BlendFile Read(ContentReader input, BlendFile existingInstance)
        {
            //int version = input.ReadInt32();

            return input.ReadObject<BlendFile>();

            //try
            //{
            //    return (BlendFile)typeof(BlendFile).GetMethod("readVersion" + version).Invoke(this, new object[] { input, existingInstance });
            //}
            //catch(Exception ex)
            //{
            //    throw new TargetException("Could not read the .xnb file with version " + version + 
            //        ". It is likely that no readVersion" + version + "() exists (did you forget to write one?).", ex);
            //}
        }

        protected BlendFile readVersion1(ContentReader input, BlendFile existingInstance)
        {
            throw new NotImplementedException();
        }
    }
}
