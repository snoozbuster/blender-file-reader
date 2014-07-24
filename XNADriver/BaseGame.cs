using BlenderFileReader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using DialogResult = System.Windows.Forms.DialogResult;
using FileDialog = System.Windows.Forms.OpenFileDialog;

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

        VertexBuffer axisBuff = null;
        VertexPositionColor[] axisVerts = null;

        List<BlenderModel> models = new List<BlenderModel>();
        List<BlenderModel> transparentModels = new List<BlenderModel>();

        Camera camera;

        int currentLayer = 1;

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

            foreach (Keys key in keyboard.GetPressedKeys())
                if(keyboardLastFrame.IsKeyUp(key)) // check to see if just pressed
                {
                    int code = key.GetHashCode();

                    if(code >= 48 && code <= 57)
                    {
                        if(code == 48)
                            code = 10;
                        else
                            code -= 48;
                        if(keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt))
                            code += 10;

                        code = 1 << (code - 1);
                        if(keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
                            currentLayer ^= code; // toggle bit
                        else
                            currentLayer = code;
                    }
                }

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

            foreach(BlenderModel m in models)
                if((m.Layer & currentLayer) != 0)
                    drawModel(m);

            foreach(BlenderModel m in transparentModels)
                if((m.Layer & currentLayer) != 0)
                    drawModel(m, true);

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
            models = new List<BlenderModel>();
            transparentModels = new List<BlenderModel>();
            currentLayer = 1;

            PopulatedStructure curscene = file.GetStructuresByAddress(file.GetStructuresOfType("FileGlobal")[0]["curscene"].GetValueAsPointer())[0];
            ulong next = curscene["base.first"].GetValueAsPointer();
            while(next != 0)
            {
                PopulatedStructure objBase = file.GetStructuresByAddress(next)[0];
                PopulatedStructure obj = file.GetStructuresByAddress(objBase["object"].GetValueAsPointer())[0];
                FieldInfo data = obj["data"];
                int SDNAIndex = file.GetBlockByAddress(data.GetValueAsPointer()).SDNAIndex;
                while(file.StructureDNA.StructureList[SDNAIndex].StructureTypeName != "Mesh")
                {
                    ulong nextPointer = objBase["next"].GetValueAsPointer();
                    if(nextPointer == 0)
                        return; // we've run out of objects in the list, and haven't found any meshes

                    objBase = file.GetStructuresByAddress(nextPointer)[0];
                    obj = file.GetStructuresByAddress(objBase["object"].GetValueAsPointer())[0];
                    data = obj["data"];
                    SDNAIndex = file.GetBlockByAddress(data.GetValueAsPointer()).SDNAIndex;
                }

                PopulatedStructure mesh = file.GetStructuresByAddress(data.GetValueAsPointer())[0];
                BlenderModel model = new BlenderModel(mesh, obj, GraphicsDevice, file);
                if(model.TextureHasTransparency)
                    transparentModels.Add(model);
                else
                    models.Add(model);

                next = objBase["next"].GetValueAsPointer();
            }
        }

        private void drawModel(BlenderModel model, bool transparent = false)
        {
            effect.View = camera.View;
            effect.Projection = camera.Projection;
            effect.World = camera.World * Matrix.CreateScale(model.Scale) * Matrix.CreateFromQuaternion(model.Rotation) * Matrix.CreateTranslation(model.Position);
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            if(transparent)
            {
                GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
            }
            else
            {
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            }
            effect.LightingEnabled = model.LightingEnabled;
            effect.DirectionalLight0.Direction = -Vector3.UnitZ;
            effect.DirectionalLight1.Enabled = true;
            effect.DirectionalLight1.Direction = Vector3.UnitZ;
            effect.TextureEnabled = true;
            effect.PreferPerPixelLighting = true;
            effect.VertexColorEnabled = false;
            effect.Texture = model.Texture;
            GraphicsDevice.SetVertexBuffer(model.VertexBuffer);
            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, model.Vertices, 0, model.Vertices.Length / 3);
            }
            effect.TextureEnabled = false;
            effect.LightingEnabled = false;
            effect.VertexColorEnabled = true;
            GraphicsDevice.SetVertexBuffer(model.NormalBuffer);
            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, model.NormalVerts, 0, model.NormalVerts.Length / 2);
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
