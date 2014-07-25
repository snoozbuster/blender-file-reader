using BlenderFileReader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderTypes
{
    public class Mesh
    {
        /// <summary>
        /// Underlying model of the class. Includes normals; whether or not UVs are included is indicated by
        /// Mesh.ModelHasUVs.
        /// </summary>
        public Model Model { get; private set; }
        /// <summary>
        /// List of materials bound to the mesh. May be empty.
        /// </summary>
        public Material[] Materials { get; private set; }

        /// <summary>
        /// Location of the mesh.
        /// This is NOT usually where the location is stored. In fact, this field is almost completely unused.
        /// Use BlenderObject.Location instead.
        /// </summary>
        public Vector3 Location { get; private set; }
        /// <summary>
        /// Rotation of the mesh; not sure if it's supposed to be degrees or radians.
        /// This is NOT usually where the rotation is stored. In fact, this field is almost completely unused.
        /// Use BlenderObject.Rotation instead.
        /// </summary>
        public Vector3 Rotation { get; private set; }
        /// <summary>
        /// Computed size of the mesh, as stored in the file. Includes rotations and scaling from object
        /// data, possibly from parent objects too.
        /// </summary>
        public Vector3 Size { get; private set; }

        /// <summary>
        /// Name of the mesh. In most cases, this will be the name of the object.
        /// </summary>
        public string Name { get; private set; }

        internal Mesh(PopulatedStructure mesh, BlenderFile file)
        {
            FileBlock materialArray;

            int pointerSize = mesh["mat"].Size;
            Name = new string(mesh["id.name"].GetValueAsCharArray()).Split('\0')[0].Substring(2);
            ulong mat = mesh["mat"].GetValueAsPointer();
            if(mat == 0 || (materialArray = file.GetBlockByAddress(mat)).Size % pointerSize != 0)
                Materials = new Material[0];
            else
            {                
                int count = materialArray.Size % pointerSize;
                Materials = new Material[count];
                for(int i = 0; i < count; i++)
                    Materials[i] = Material.GetOrCreateMaterial(
                        file, 
                        file.GetStructuresByAddress(
                            pointerSize == 4 ? BitConverter.ToUInt32(materialArray.Data, count * pointerSize) : 
                                               BitConverter.ToUInt64(materialArray.Data, count * pointerSize)
                        )[0]
                    );
            }
            float[] vectorTemp = mesh["loc"].GetValueAsFloatArray();
            Location = new Vector3(vectorTemp[0], vectorTemp[1], vectorTemp[2]);
            vectorTemp = mesh["rot"].GetValueAsFloatArray();
            Rotation = new Vector3(vectorTemp[0], vectorTemp[1], vectorTemp[2]);
            vectorTemp = mesh["size"].GetValueAsFloatArray();
            Size = new Vector3(vectorTemp[0], vectorTemp[1], vectorTemp[2]);

            MeshBuilder primordialMesh = MeshBuilder.StartMesh(Name);

            // both structures use the same vertex structure
            List<Vector3> verts = new List<Vector3>();
            List<short[]> unconvertedNormals = new List<short[]>();
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mvert"].GetValueAsPointer()))
            {
                float[] vector = s["co"].GetValueAsFloatArray();
                unconvertedNormals.Add(s["no"].GetValueAsShortArray());
                verts.Add(new Vector3(vector[0], vector[1], vector[2]));
            }
            List<Vector3> normals = convertNormals(unconvertedNormals);

            VertexPositionNormalTexture[] vertices;
            BasicMaterialContent bmc;
            // todo: not yet sure which format versions of Blender between 2.62 and 2.65 use.
            if(float.Parse(file.VersionNumber) >= 2.66f) // uses edges, loops, and polys (Blender 2.66+)
                vertices = loadNewModel(file, mesh, verts, normals, out bmc);
            else // uses MFace (Blender 2.49-2.61)
                vertices = loadOldModel(file, mesh, verts, normals, out bmc);

            MeshBuilder mb = MeshBuilder.StartMesh(Name);
            foreach(VertexPositionNormalTexture v in vertices)
                mb.CreatePosition(v.Position);
            int uvChannel = mb.CreateVertexChannel<Vector2>(VertexChannelNames.TextureCoordinate(0));
            int normalChannel = mb.CreateVertexChannel<Vector3>(VertexChannelNames.Normal());
            int j = 0;
            foreach(VertexPositionNormalTexture v in vertices)
            {
                mb.SetVertexChannelData(uvChannel, v.TextureCoordinate);
                mb.SetVertexChannelData(normalChannel, v.Normal);

                mb.AddTriangleVertex(j++);
            }
        }

        private VertexPositionNormalTexture[] loadOldModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts, List<Vector3> normals, out BasicMaterialContent bmc)
        {
            // I believe this function has a bug when used on a mesh that has unconnected chunks of vertices;
            // however the only file I currently have that exhibits this problem decompresses to 240MB when I use the HTML
            // renderer tool, so I can't feasibly poke through the data to see what's going wrong.

            List<VertexPositionNormalTexture> output = new List<VertexPositionNormalTexture>();
            bmc = new BasicMaterialContent();

            List<int[]> faces = new List<int[]>();
            List<float[,]> tFaces = new List<float[,]>();
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mface"].GetValueAsPointer()))
                faces.Add(new[] { s["v1"].GetValueAsInt(), s["v2"].GetValueAsInt(), s["v3"].GetValueAsInt(), s["v4"].GetValueAsInt() });
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mtface"].GetValueAsPointer()))
                tFaces.Add((float[,])s["uv"].GetValueAsMultidimensionalArray());

            // assume all faces use same texture
            PopulatedStructure image = file.GetStructuresByAddress(file.GetStructuresByAddress(mesh["mtface"].GetValueAsPointer())[0]["tpage"].GetValueAsPointer())[0];
            if(image["packedfile"].GetValueAsPointer() != 0)
            {
                byte[] rawImage = file.GetBlockByAddress(file.GetStructuresByAddress(image["packedfile"].GetValueAsPointer())[0]["data"].GetValueAsPointer()).Data;
                string filename = Name + "_" + new string(image["id.name"].GetValueAsCharArray()).Split('\0')[0].Substring(2);
                using(BinaryWriter s = new BinaryWriter(File.Open(filename, FileMode.Create)))
                    s.Write(rawImage);
                bmc.Texture = new ExternalReference<TextureContent>(filename);
            }
            else
            {
                try
                {
                    string texturePath = image["name"].ToString().Split('\0')[0].Replace("/", "\\").Replace("\\\\", "\\");
                    string filePath = file.GetStructuresOfType("FileGlobal")[0]["filename"].ToString();
                    filePath = filePath.Substring(0, filePath.LastIndexOf('\\'));
                    File.Copy((filePath + texturePath).Replace("\'", ""), Name + "_" + new string(image["id.name"].GetValueAsCharArray()).Split('\0')[0].Substring(2));
                }
                catch
                {
                    //texture = defaultTex;
                }
            }

            int j = 0;
            foreach(int[] face in faces)
            {
                Vector3[] faceVerts = new Vector3[face.Length];
                Vector3[] faceVertNormals = new Vector3[face.Length];
                Vector2[] faceUVs = new Vector2[face.Length];
                for(int i = 0; i < face.Length; i++)
                {
                    faceVerts[i] = verts[face[i]];
                    faceVertNormals[i] = normals[face[i]];
                    faceUVs[i] = new Vector2(tFaces[j][i, 0], tFaces[j][i, 1]);
                }
                j++;

                // 2, 1, 0
                for(int i = 2; i >= 0; i--)
                    output.Add(new VertexPositionNormalTexture(faceVerts[i], faceVertNormals[i], faceUVs[i]));

                // 3, 2, 0
                for(int i = 3; i >= 1; i--)
                    output.Add(new VertexPositionNormalTexture(faceVerts[i == 1 ? 0 : i], faceVertNormals[i == 1 ? 0 : i], faceUVs[i == 1 ? 0 : i]));
            }

            return output.ToArray();
        }

        private List<Vector3> convertNormals(List<short[]> unconvertedNormals)
        {
            // Blender stores normals by storing a denormalized mantissa in a short, then
            // negating that short if the normal is negative. To convert a float mantissa to
            // a decimal, multiply the integer representation of the mantissa by 2^-23.
            // For whatever reason, this algorithm only keeps about three places of accuracy before it starts to diverge.

            List<Vector3> normals = new List<Vector3>();
            foreach(short[] shortNormal in unconvertedNormals)
            {
                bool[] negatives = new bool[3];
                int[] intNormals = new int[3];
                for(int i = 0; i < shortNormal.Length; i++)
                {
                    negatives[i] = shortNormal[i] < 0;
                    intNormals[i] = negatives[i] ? -shortNormal[i] : shortNormal[i];
                    intNormals[i] <<= 8; // move to correct mantissa location
                }
                double power = Math.Pow(2, -23);
                float[] floatNormals = new float[3];
                for(int i = 0; i < intNormals.Length; i++)
                    floatNormals[i] = (float)(intNormals[i] * power) * (negatives[i] ? -1 : 1);
                normals.Add(Vector3.Normalize(new Vector3(floatNormals[0], floatNormals[1], floatNormals[2])));
            }
            return normals;
        }

        private VertexPositionNormalTexture[] loadNewModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts, List<Vector3> normals, out BasicMaterialContent bmc)
        {
            List<VertexPositionNormalTexture> output = new List<VertexPositionNormalTexture>();

            List<Vector2> edges = new List<Vector2>(); // using x as index1 and y as index2
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["medge"].GetValueAsPointer()))
                edges.Add(new Vector2(s["v1"].GetValueAsInt(), s["v2"].GetValueAsInt()));
            // a "loop" is a vertex index and an edge index. Groups of these are used to define a "poly", which is a face. 
            List<Vector2> loops = new List<Vector2>(); // using x as "v" and y as "e"
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mloop"].GetValueAsPointer()))
                loops.Add(new Vector2(s["v"].GetValueAsInt(), s["e"].GetValueAsInt()));
            List<Vector2> uvLoops = null; // using x as u and y as v
            Vector2[] backupUVs = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }; // in case uvLoops is null
            if(mesh["mloopuv"].GetValueAsPointer() != 0)
            {
                uvLoops = new List<Vector2>();
                foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mloopuv"].GetValueAsPointer()))
                {
                    float[] uv = s["uv"].GetValueAsFloatArray();
                    uvLoops.Add(new Vector2(uv[0], uv[1]));
                }
            }
            List<Vector2> polys = new List<Vector2>(); // using x as "loopstart" and y as "totloop" (loop length)
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mpoly"].GetValueAsPointer()))
                polys.Add(new Vector2(s["loopstart"].GetValueAsInt(), s["totloop"].GetValueAsInt()));
            // assume all faces use same texture for now
            //if(mesh["mtpoly"].GetValueAsPointer() != 0)
            //{
            //    try
            //    {
            //        // todo: sometimes this line fails, probably due to "assume all faces use same texture"
            //        PopulatedStructure image = file.GetStructuresByAddress(file.GetStructuresByAddress(mesh["mtpoly"].GetValueAsPointer())[0]["tpage"].GetValueAsPointer())[0];
            //        if(image["packedfile"].GetValueAsPointer() != 0)
            //        {
            //            byte[] rawImage = file.GetBlockByAddress(file.GetStructuresByAddress(image["packedfile"].GetValueAsPointer())[0]["data"].GetValueAsPointer()).Data;
            //            using(Stream s = new MemoryStream(rawImage))
            //                texture = Texture2D.FromStream(GraphicsDevice, s);
            //        }
            //        else
            //        {
            //            string texturePath = image["name"].ToString().Split('\0')[0].Replace("/", "\\").Replace("\\\\", "\\");
            //            string filePath = file.GetStructuresOfType("FileGlobal")[0]["filename"].ToString();
            //            filePath = filePath.Substring(0, filePath.LastIndexOf('\\'));
            //            using(Stream s = File.Open((filePath + texturePath).Replace("\'", ""), FileMode.Open, FileAccess.Read))
            //                texture = Texture2D.FromStream(GraphicsDevice, s);
            //        }
            //    }
            //    catch
            //    {
            //        texture = defaultTex;
            //    }
            //}
            //else
            //{
            //    texture = new Texture2D(GraphicsDevice, 1, 1);
            //    texture.SetData(new Color[] { Color.Gray });
            //}
            // loops of length 3 are triangles and can be directly added to the vertex list. loops of length 4
            // are quads, and have to be split into two triangles.
            foreach(Vector2 poly in polys)
            {
                Vector2[] faceEdges = new Vector2[(int)poly.Y];
                Vector2[] faceUVs = new Vector2[faceEdges.Length];
                int j = 0;
                int loopOffset = (int)poly.X;
                for(int i = loopOffset; i < (int)poly.Y + loopOffset; i++)
                {
                    faceEdges[j] = edges[(int)loops[i].Y];
                    faceUVs[j] = uvLoops == null ? backupUVs[i - loopOffset] : uvLoops[i];
                    j++;
                }
                Vector3[] faceVerts = new Vector3[faceEdges.Length];
                Vector3[] faceVertNormals = new Vector3[faceEdges.Length];
                for(int i = 0; i < faceEdges.Length; i++)
                {
                    int index = (int)(loops[loopOffset + i].X == faceEdges[i].X ? faceEdges[i].Y : faceEdges[i].X);
                    faceVerts[i] = verts[index];
                    faceVertNormals[i] = normals[index];
                }
                if(faceVerts.Length == 3) // already a triangle
                {
                    // push 0 to the end
                    Vector2 temp = faceUVs[0];
                    faceUVs[0] = faceUVs[1];
                    faceUVs[1] = temp;
                    temp = faceUVs[1];
                    faceUVs[1] = faceUVs[2];
                    faceUVs[2] = temp;

                    for(int i = 2; i >= 0; i--) // 2, 1, 0
                        output.Add(new VertexPositionNormalTexture(faceVerts[i], faceVertNormals[i], faceUVs[i]));
                }
                else if(faceVerts.Length == 4) // quad, split into tris
                {
                    // swap 3 with 1 and 2 with 3
                    Vector2 temp = faceUVs[1];
                    faceUVs[1] = faceUVs[3];
                    faceUVs[3] = temp;
                    temp = faceUVs[2];
                    faceUVs[2] = faceUVs[3];
                    faceUVs[3] = temp;

                    // 2, 1, 0
                    for(int i = 2; i >= 0; i--)
                        output.Add(new VertexPositionNormalTexture(faceVerts[i], faceVertNormals[i], faceUVs[i]));

                    // 3, 2, 0
                    for(int i = 3; i >= 1; i--)
                        output.Add(new VertexPositionNormalTexture(faceVerts[i == 1 ? 0 : i], faceVertNormals[i == 1 ? 0 : i], faceUVs[i == 1 ? 0 : i]));
                }
            }
            bmc = new BasicMaterialContent();
            return output.ToArray();
        }
    }
}
