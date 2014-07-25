using BlenderFileReader;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace BlenderPipeline
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to apply custom processing to content data, converting an object of
    /// type BlenderFile to BlendFile.
    /// </summary>
    [ContentProcessor(DisplayName = "Blend File - BlenderPipeline")]
    public class BlenderProcessor : ContentProcessor<BlenderFile, BlendFile>
    {
        // it's either this or pass it through to every function.
        internal static ContentProcessorContext Context;

        public override BlendFile Process(BlenderFile input, ContentProcessorContext context)
        {
            // I lied, the processor wasn't hard. It's the internal constructors of all the objects
            // that do all the work. 
            Context = context;
            return new BlendFile(input);
        }
    }
}