using BlenderFileReader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace XNADriver
{
    class BlenderModel
    {
        static Texture2D defaultTex;

        public readonly VertexPositionColor[] NormalVerts;
        public readonly VertexBuffer NormalBuffer;
        public readonly VertexPositionNormalTexture[] Vertices;
        public readonly VertexBuffer VertexBuffer;
        public readonly Texture2D Texture;

        public readonly string Name;
        public readonly int Layer;

        public readonly bool TextureHasTransparency;
        public readonly bool LightingEnabled;

        public Vector3 Position = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Scale = Vector3.One;

        protected GraphicsDevice GraphicsDevice;

        public BlenderModel(PopulatedStructure mesh, PopulatedStructure obj, GraphicsDevice GraphicsDevice, BlenderFile file)
        {
            if(defaultTex == null)
            {
                defaultTex = new Texture2D(GraphicsDevice, 1, 1);
                defaultTex.SetData(new Color[] { Color.Gray });
            }

            this.GraphicsDevice = GraphicsDevice;

            // both structures use the same vertex structure
            List<Vector3> verts = new List<Vector3>();
            List<short[]> unconvertedNormals = new List<short[]>();
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mvert"].GetValueAsUInt()))
            {
                float[] vector = s["co[3]"].GetValueAsFloatArray();
                unconvertedNormals.Add(s["no[3]"].GetValueAsShortArray());
                verts.Add(new Vector3(vector[0], vector[1], vector[2]));
            }

            List<Vector3> normals = convertNormals(unconvertedNormals);
            VertexPositionNormalTexture[] vertices;
            Texture2D texture;
            // todo: not yet sure which format versions of Blender between 2.62 and 2.65 use.
            if(float.Parse(file.VersionNumber) >= 2.66f) // uses edges, loops, and polys (Blender 2.66+)
                vertices = loadNewModel(file, mesh, verts, normals, out texture);
            else // uses MFace (Blender 2.49-2.61)
                vertices = loadOldModel(file, mesh, verts, normals, out texture);

            VertexPositionColor[] normalVerts = new VertexPositionColor[normals.Count * 2];
            for(int i = 0; i < verts.Count * 2; i += 2)
            {
                normalVerts[i] = new VertexPositionColor(verts[i / 2], Color.MidnightBlue);
                normalVerts[i + 1] = new VertexPositionColor(verts[i / 2] + normals[i / 2] * 0.25f, Color.MidnightBlue);
            }

            float[] posVector = obj["loc[3]"].GetValueAsFloatArray();
            this.Position = new Vector3(posVector[0], posVector[1], posVector[2]);
            float[] scaleVector = obj["size[3]"].GetValueAsFloatArray();
            this.Scale = new Vector3(scaleVector[0], scaleVector[1], scaleVector[2]);
            float[] rotVector = obj["rot[3]"].GetValueAsFloatArray();
            this.Rotation = Quaternion.CreateFromYawPitchRoll(rotVector[1], rotVector[0], rotVector[2]);
            this.Vertices = vertices;
            this.NormalVerts = normalVerts;
            this.VertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration, this.Vertices.Length, BufferUsage.None);
            this.NormalBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, this.NormalVerts.Length, BufferUsage.None);
            this.Texture = texture;
            this.Name = new string(obj["id.name[66]"].GetValueAsCharArray()).Split('\0')[0].Substring(2); // remove null term, remove first two characters

            // LSB on represents layer 1, next bit is layer 2, etc
            this.Layer = obj["lay"].GetValueAsInt();

            // the "mat" field is a pointer to a pointer (technically, a pointer to an array of pointers)
            // I'm not sure what to do with multiple materials, so just use the first one
            uint blockaddr = mesh["mat"].GetValueAsUInt();
            if(blockaddr != 0)
            {
                PopulatedStructure mat = file.GetStructuresByAddress(BitConverter.ToUInt32(file.GetBlockByAddress(blockaddr).Data, 0))[0];
                this.TextureHasTransparency = mat["game.alpha_blend"].GetValueAsInt() != 0;

                int mode = mat["mode"].GetValueAsInt();
                this.LightingEnabled = (mode & 4) == 0; // as far as I can tell, this is where "shadeless" is stored.
            }
            else
                this.LightingEnabled = true;
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

        private VertexPositionNormalTexture[] loadOldModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts, List<Vector3> normals, out Texture2D texture)
        {
            // I believe this function has a bug when used on a mesh that has unconnected chunks of vertices;
            // however the only file I currently have that exhibits this problem decompresses to 240MB when I use the HTML
            // renderer tool, so I can't feasibly poke through the data to see what's going wrong.

            List<VertexPositionNormalTexture> output = new List<VertexPositionNormalTexture>();

            List<int[]> faces = new List<int[]>();
            List<float[,]> tFaces = new List<float[,]>();
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mface"].GetValueAsUInt()))
                faces.Add(new[] { s["v1"].GetValueAsInt(), s["v2"].GetValueAsInt(), s["v3"].GetValueAsInt(), s["v4"].GetValueAsInt() });
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mtface"].GetValueAsUInt()))
                tFaces.Add((float[,])s["uv[4][2]"].GetValueAsMultidimensionalArray());

            // assume all faces use same texture
            PopulatedStructure image = file.GetStructuresByAddress(file.GetStructuresByAddress(mesh["mtface"].GetValueAsUInt())[0]["tpage"].GetValueAsUInt())[0];
            if(image["packedfile"].GetValueAsUInt() != 0)
            {
                byte[] rawImage = file.GetBlockByAddress(file.GetStructuresByAddress(image["packedfile"].GetValueAsUInt())[0]["data"].GetValueAsUInt()).Data;
                using(Stream s = new MemoryStream(rawImage))
                    texture = Texture2D.FromStream(GraphicsDevice, s);
            }
            else
            {
                try
                {
                    string texturePath = image["name[1024]"].ToString().Split('\0')[0].Replace("/", "\\").Replace("\\\\", "\\");
                    string filePath = file.GetStructuresOfType("FileGlobal")[0]["filename[1024]"].ToString();
                    filePath = filePath.Substring(0, filePath.LastIndexOf('\\'));
                    using(Stream s = File.Open((filePath + texturePath).Replace("\'", ""), FileMode.Open, FileAccess.Read))
                        texture = Texture2D.FromStream(GraphicsDevice, s);
                }
                catch
                {
                    texture = defaultTex;
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

        private VertexPositionNormalTexture[] loadNewModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts, List<Vector3> normals, out Texture2D texture)
        {
            List<VertexPositionNormalTexture> output = new List<VertexPositionNormalTexture>();

            List<Vector2> edges = new List<Vector2>(); // using x as index1 and y as index2
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["medge"].GetValueAsUInt()))
                edges.Add(new Vector2(s["v1"].GetValueAsInt(), s["v2"].GetValueAsInt()));
            // a "loop" is a vertex index and an edge index. Groups of these are used to define a "poly", which is a face. 
            List<Vector2> loops = new List<Vector2>(); // using x as "v" and y as "e"
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mloop"].GetValueAsUInt()))
                loops.Add(new Vector2(s["v"].GetValueAsInt(), s["e"].GetValueAsInt()));
            List<Vector2> uvLoops = null; // using x as u and y as v
            Vector2[] backupUVs = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }; // in case uvLoops is null
            if(mesh["mloopuv"].GetValueAsUInt() != 0)
            {
                uvLoops = new List<Vector2>();
                foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mloopuv"].GetValueAsUInt()))
                {
                    float[] uv = s["uv[2]"].GetValueAsFloatArray();
                    uvLoops.Add(new Vector2(uv[0], uv[1]));
                }
            }
            List<Vector2> polys = new List<Vector2>(); // using x as "loopstart" and y as "totloop" (loop length)
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mpoly"].GetValueAsUInt()))
                polys.Add(new Vector2(s["loopstart"].GetValueAsInt(), s["totloop"].GetValueAsInt()));
            // assume all faces use same texture for now
            if(mesh["mtpoly"].GetValueAsUInt() != 0)
            {
                try
                {
                    // todo: sometimes this line fails, probably due to "assume all faces use same texture"
                    PopulatedStructure image = file.GetStructuresByAddress(file.GetStructuresByAddress(mesh["mtpoly"].GetValueAsUInt())[0]["tpage"].GetValueAsUInt())[0];
                    if(image["packedfile"].GetValueAsUInt() != 0)
                    {
                        byte[] rawImage = file.GetBlockByAddress(file.GetStructuresByAddress(image["packedfile"].GetValueAsUInt())[0]["data"].GetValueAsUInt()).Data;
                        using(Stream s = new MemoryStream(rawImage))
                            texture = Texture2D.FromStream(GraphicsDevice, s);
                    }
                    else
                    {
                        string texturePath = image["name[1024]"].ToString().Split('\0')[0].Replace("/", "\\").Replace("\\\\", "\\");
                        string filePath = file.GetStructuresOfType("FileGlobal")[0]["filename[1024]"].ToString();
                        filePath = filePath.Substring(0, filePath.LastIndexOf('\\'));
                        using(Stream s = File.Open((filePath + texturePath).Replace("\'", ""), FileMode.Open, FileAccess.Read))
                            texture = Texture2D.FromStream(GraphicsDevice, s);
                    }
                }
                catch
                {
                    texture = defaultTex;
                }
            }
            else
            {
                texture = new Texture2D(GraphicsDevice, 1, 1);
                texture.SetData(new Color[] { Color.Gray });
            }
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

            return output.ToArray();
        }

    }
}
