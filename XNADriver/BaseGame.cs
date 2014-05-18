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

namespace XNADriver
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BaseGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        BasicEffect effect;
        VertexBuffer buffer = null;
        VertexPositionColor[] vertices = null;

        VertexBuffer axisBuff;
        VertexPositionColor[] axisVerts = null;

        Camera camera;

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
            // TODO: Add your initialization logic here

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
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            camera.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            drawAxes();

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

            base.Draw(gameTime);
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
