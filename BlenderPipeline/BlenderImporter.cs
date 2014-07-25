using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content.Pipeline;
using BlenderFileReader;

namespace BlenderPipeline
{
    [ContentImporter(".blend", DefaultProcessor = "Blend File - BlenderPipeline",
      DisplayName = "Blend File - BlenderPipeline")]
    class BlenderImporter : ContentImporter<BlenderFile>
    {
        public override BlenderFile Import(string filename, ContentImporterContext context)
        {
            // importing from the file is done by the library; that's the easy part.
            // the content processor is the hard part.
            return new BlenderFile(filename);
        }
    }
}
