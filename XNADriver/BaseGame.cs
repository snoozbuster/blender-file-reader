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
        VertexPositionColor[] vertices = null;

        VertexBuffer axisBuff;
        VertexPositionColor[] axisVerts = null;

        Camera camera;

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
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mvert"].GetValueAsUInt()))
            {
                float[] vector = s["co[3]"].GetValueAsFloatArray();
                verts.Add(new Vector3(vector[0], vector[1], vector[2]));
            }
            
            List<Vector3> vertices = new List<Vector3>();
            // todo: not yet sure which format versions of Blender between 2.62 and 2.65 use.
            if(float.Parse(file.VersionNumber) >= 2.66f) // uses edges, loops, and polys (Blender 2.66+)
                vertices = loadNewModel(file, mesh, verts);
            else // uses MFace (Blender 2.49-2.61)
                vertices = loadOldModel(file, mesh, verts);

            this.vertices = new VertexPositionColor[vertices.Count];
            for(int i = 0; i < vertices.Count; i++)
                this.vertices[i] = new VertexPositionColor(vertices[i], Color.Gray);
            buffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, this.vertices.Length, BufferUsage.None);
        }

        private static List<Vector3> loadOldModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int[]> faces = new List<int[]>();
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mface"].GetValueAsUInt()))
                faces.Add(new[] { s["v1"].GetValueAsInt(), s["v2"].GetValueAsInt(), s["v3"].GetValueAsInt(), s["v4"].GetValueAsInt() });

            foreach(int[] face in faces)
            {
                Vector3[] faceVerts = new Vector3[face.Length];
                for(int i = 0; i < face.Length; i++)
                    faceVerts[i] = verts[face[i]];

                vertices.Add(faceVerts[2]);
                vertices.Add(faceVerts[1]);
                vertices.Add(faceVerts[0]);

                vertices.Add(faceVerts[3]);
                vertices.Add(faceVerts[2]);
                vertices.Add(faceVerts[0]);
            }

            return vertices;
        }

        private static List<Vector3> loadNewModel(BlenderFile file, PopulatedStructure mesh, List<Vector3> verts)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> edges = new List<Vector2>(); // using x as index1 and y as index2
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["medge"].GetValueAsUInt()))
                edges.Add(new Vector2(s["v1"].GetValueAsInt(), s["v2"].GetValueAsInt()));
            // a "loop" is a vertex index and an edge index. Groups of these are used to define a "poly", which is a face. 
            List<Vector2> loops = new List<Vector2>(); // using x as "v" and y as "e"
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mloop"].GetValueAsUInt()))
                loops.Add(new Vector2(s["v"].GetValueAsInt(), s["e"].GetValueAsInt()));
            List<Vector2> polys = new List<Vector2>(); // using x as "loopstart" and y as "totloop" (loop length)
            foreach(PopulatedStructure s in file.GetStructuresByAddress(mesh["mpoly"].GetValueAsUInt()))
                polys.Add(new Vector2(s["loopstart"].GetValueAsInt(), s["totloop"].GetValueAsInt()));
            // loops of length 3 are triangles and can be directly added to the vertex list. loops of length 4
            // are quads, and have to be split into two triangles.
            foreach(Vector2 poly in polys)
            {
                Vector2[] faceEdges = new Vector2[(int)poly.Y];
                int j = 0;
                int loopOffset = (int)poly.X;
                for(int i = loopOffset; i < (int)poly.Y + loopOffset; i++)
                    faceEdges[j++] = edges[(int)loops[i].Y];
                Vector3[] faceVerts = new Vector3[faceEdges.Length];
                for(int i = 0; i < faceEdges.Length; i++)
                    faceVerts[i] = verts[(int)(loops[loopOffset + i].X == faceEdges[i].X ? faceEdges[i].Y : faceEdges[i].X)];
                if(faceVerts.Length == 3) // already a triangle
                    for(int i = 2; i >= 0; i--)
                        vertices.Add(faceVerts[i]);
                else if(faceVerts.Length == 4) // quad, split into tris
                {
                    vertices.Add(faceVerts[2]);
                    vertices.Add(faceVerts[1]);
                    vertices.Add(faceVerts[0]);

                    vertices.Add(faceVerts[3]);
                    vertices.Add(faceVerts[2]);
                    vertices.Add(faceVerts[0]);
                }
            }

            return vertices;
        }

        private void drawModel()
        {
            effect.View = camera.View;
            effect.Projection = camera.Projection;
            effect.World = camera.World;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            //effect.LightingEnabled = true;
            if(buffer != null)
            {
                GraphicsDevice.SetVertexBuffer(buffer);
                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
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
