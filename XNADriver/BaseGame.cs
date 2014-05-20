using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using BlenderFileReader;
using FileDialog = System.Windows.Forms.OpenFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using System.IO;

namespace XNADriver
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BaseGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        BasicEffect effect;
        VertexBuffer buffer = null;
        VertexPositionNormalTexture[] vertices = null;

        VertexBuffer axisBuff;
        VertexPositionColor[] axisVerts = null;

        VertexBuffer normalBuff;
        VertexPositionColor[] normalVerts = null;

        Camera camera;

        Texture2D texture;

        KeyboardState keyboardLastFrame;

        public BaseGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            keyboardLastFrame = Keyboard.GetState();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            camera = new Camera(MathHelper.ToRadians(70), GraphicsDevice.Viewport.AspectRatio, 1, 10000);
            effect = new BasicEffect(GraphicsDevice);
            effect.TextureEnabled = false;
            effect.VertexColorEnabled = true;

            axisVerts = new VertexPositionColor[6];
            axisVerts[0] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Red);
            axisVerts[1] = new VertexPositionColor(new Vector3(10000, 0, 0), Color.Red);
            axisVerts[2] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Green);
            axisVerts[3] = new VertexPositionColor(new Vector3(0, 10000, 0), Color.Green);
            axisVerts[4] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue);
            axisVerts[5] = new VertexPositionColor(new Vector3(0, 0, 10000), Color.Blue);
            axisBuff = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, axisVerts.Length, BufferUsage.None);

            texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new Color[] { Color.Gray });
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() { }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            if(keyboard.IsKeyDown(Keys.Escape))
                Exit();

            camera.Update(gameTime);

            if(keyboard.IsKeyUp(Keys.O) && keyboardLastFrame.IsKeyDown(Keys.O))
                loadFile();

            base.Update(gameTime);

            keyboardLastFrame = keyboard;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            drawAxes();

            drawModel();

            base.Draw(gameTime);
        }

        private void loadFile()
        {
            FileDialog f = new FileDialog();
            f.Multiselect = false;
            f.Filter = "Blender files (*.blend)|*.blend";
            f.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if(f.ShowDialog() == DialogResult.OK)
                loadModelData(new BlenderFile(f.FileName));
        }

        private void loadModelData(BlenderFile file)
        {
            // this will crash if there are no meshes, because I don't want to deal with checking to see when things are the null pointer
            PopulatedStructure curscene = file.GetStructuresByAddress(file.GetStructuresOfType("FileGlobal")[0]["curscene"].GetValueAsUInt())[0];
            PopulatedStructure obj = file.GetStructuresByAddress(curscene["base.first"].GetValueAsUInt())[0];
            FieldInfo data = file.GetStructuresByAddress(obj["object"].GetValueAsUInt())[0]["data"];
            int SDNAIndex = file.GetBlockByAddress(data.GetValueAsUInt()).SDNAIndex;
            while(file.StructureDNA.StructureList[SDNAIndex].StructureTypeName != "Mesh")
            {
                obj = file.GetStructuresByAddress(obj["next"].GetValueAsUInt())[0];
                data = file.GetStructuresByAddress(obj["object"].GetValueAsUInt())[0]["data"];
                SDNAIndex = file.GetBlockByAddress(data.GetValueAsUInt()).SDNAIndex;
            }
            PopulatedStructure mesh = file.GetStructuresByAddress(data.GetValueAsUInt())[0];
            // now that we have a mesh, the interesting bits happen
            
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
            // todo: not yet sure which format versions of Blender between 2.62 and 2.65 use.
            if(float.Parse(file.VersionNumber) >= 2.66f) // uses edges, loops, and polys (Blender 2.66+)
                vertices = loadNewModel(file, mesh, verts, normals);
            else // uses MFace (Blender 2.49-2.61)
                vertices = loadOldModel(file, mesh, verts, normals);

            buffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration, this.vertices.Length, BufferUsage.None);

            normalVerts = new VertexPositionColor[normals.Count * 2];
            for(int i = 0; i < verts.Count * 2; i += 2)
            {
                normalVerts[i] = new VertexPositionColor(verts[i / 2], Color.MidnightBlue);
                normalVerts[i + 1] = new VertexPositionColor(verts[i / 2] + normals[i / 2] * 0.25f, Color.MidnightBlue);
            }
            normalBuff = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, normalVerts.Length, BufferUsage.None);
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

        private VertexPositionNormalTexture[] loadOldModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts, List<Vector3> normals)
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
                    faceUVs[i] = new Vector2(tFaces[j][i,0], tFaces[j][i,1]);
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

        private VertexPositionNormalTexture[] loadNewModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts, List<Vector3> normals)
        {
            List<VertexPositionNormalTexture> output = new List<VertexPositionNormalTexture>();

            List<Vector2> edges = new List<Vector2>(); // using x as index1 and y as index2
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["medge"].GetValueAsUInt()))
                edges.Add(new Vector2(s["v1"].GetValueAsInt(), s["v2"].GetValueAsInt()));
            // a "loop" is a vertex index and an edge index. Groups of these are used to define a "poly", which is a face. 
            List<Vector2> loops = new List<Vector2>(); // using x as "v" and y as "e"
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mloop"].GetValueAsUInt()))
                loops.Add(new Vector2(s["v"].GetValueAsInt(), s["e"].GetValueAsInt()));
            List<Vector2> uvLoops = new List<Vector2>(); // using x as u and y as v
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mloopuv"].GetValueAsUInt()))
            {
                float[] uv = s["uv[2]"].GetValueAsFloatArray();
                uvLoops.Add(new Vector2(uv[0], uv[1]));
            }
            List<Vector2> polys = new List<Vector2>(); // using x as "loopstart" and y as "totloop" (loop length)
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mpoly"].GetValueAsUInt()))
                polys.Add(new Vector2(s["loopstart"].GetValueAsInt(), s["totloop"].GetValueAsInt()));
            // assume all faces use same texture
            PopulatedStructure image = file.GetStructuresByAddress(file.GetStructuresByAddress(mesh["mtpoly"].GetValueAsUInt())[0]["tpage"].GetValueAsUInt())[0];
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
                    using(Stream s = File.Open(filePath + texturePath, FileMode.Open, FileAccess.Read))
                        texture = Texture2D.FromStream(GraphicsDevice, s);
                }
                catch
                {
                    texture = new Texture2D(GraphicsDevice, 1, 1);
                    texture.SetData(new Color[] { Color.Gray });
                }
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
                    faceUVs[j++] = uvLoops[i];
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
                    for(int i = 2; i >= 0; i--) // 2, 1, 0
                        output.Add(new VertexPositionNormalTexture(faceVerts[i], faceVertNormals[i], faceUVs[i]));
                else if(faceVerts.Length == 4) // quad, split into tris
                {
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

        private void drawModel()
        {
            effect.View = camera.View;
            effect.Projection = camera.Projection;
            effect.World = camera.World;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            effect.LightingEnabled = true;
            effect.TextureEnabled = true;
            effect.VertexColorEnabled = false;
            effect.Texture = texture;
            if(buffer != null)
            {
                GraphicsDevice.SetVertexBuffer(buffer);
                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
                }
                effect.TextureEnabled = false;
                effect.LightingEnabled = false;
                effect.VertexColorEnabled = true;
                GraphicsDevice.SetVertexBuffer(normalBuff);
                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, normalVerts, 0, normalVerts.Length / 2);
                }
            }
        }

        private void drawAxes()
        {
            GraphicsDevice.SetVertexBuffer(axisBuff);
            effect.World = camera.World;
            effect.View = camera.View;
            effect.Projection = camera.Projection;
            effect.LightingEnabled = false;
            effect.TextureEnabled = false;
            effect.VertexColorEnabled = true;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(
                    PrimitiveType.LineList, axisVerts, 0, 3);
            }
        }
    }
}
