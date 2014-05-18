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

            camera = new Camera(MathHelper.ToRadians(70), GraphicsDevice.Viewport.AspectRatio, 1, 1000);
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
            {
                BlenderFile file = new BlenderFile(f.FileName);
                loadModelData(file);
            }
        }

        private void loadModelData(BlenderFile file)
        {

        }

        private void drawModel()
        {
            effect.View = camera.View;
            effect.Projection = camera.Projection;
            effect.World = camera.World;
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
