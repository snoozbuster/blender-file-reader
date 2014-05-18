using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XNADriver
{
    public class Camera
    {
        private Matrix rotation = Matrix.Identity;
        public Matrix Rotation { get { return rotation; } }
        public Vector3 Position { get; private set; }

        // Simply feed this camera the position of whatever you want its target to be
        protected Vector3 targetPosition = Vector3.Zero;
        public Vector3 TargetPosition { get { return targetPosition; } set { targetPosition = value; } }
        protected Vector3 posLastFrame;

        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }
        private float zoom = 100.0f;
        public float Zoom { get { return zoom; } set { zoom = MathHelper.Clamp(value, zoomMin, zoomMax); } }

        private float horizontalAngle = MathHelper.PiOver2;
        public float HorizontalAngle
        {
            get { return horizontalAngle; }
            set
            {
                if(value >= MathHelper.Pi || value <= -MathHelper.Pi)
                {
                    horizontalAngle = -horizontalAngle + (value % MathHelper.Pi);
                    return;
                }
                horizontalAngle = value % MathHelper.Pi;
            }
        }

        private float verticalAngle = (float)Math.Sqrt(3) / 2;
        public float VerticalAngle { get { return verticalAngle; } set { verticalAngle = MathHelper.Clamp(value, verticalAngleMin, verticalAngleMax); } }

        private float verticalAngleMin = 0;
        private float verticalAngleMax = MathHelper.TwoPi;
        private float zoomMin = 0;
        private float zoomMax = 10000;

        public Matrix WorldViewProj { get { return World * ViewProj; } }
        public Matrix InverseView { get { return Matrix.Invert(View); } }
        public Matrix ViewProj { get { return View * Projection; } }
        public Matrix World { get { return Matrix.Identity; } }

        private MouseState mouseLastFrame;

        /// <summary>
        /// Creates a camera.
        /// </summary>
        /// <param name="fieldOfView">The field of view in radians.</param>
        /// <param name="aspectRatio">The aspect ratio of the game.</param>
        /// <param name="nearPlane">The near plane.</param>
        /// <param name="farPlane">The far plane.</param>
        public Camera(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
        {
            if(nearPlane < 0.1f)
                throw new ArgumentException("nearPlane must be greater than 0.1.");

            Position = new Vector3(20, 20, 20);

            HorizontalAngle = MathHelper.PiOver4;
            VerticalAngle = (float)Math.Sqrt(3) / 2;

            this.Projection = Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio,
                                                                        nearPlane, farPlane);
            this.LookAt(TargetPosition);
            this.View = Matrix.CreateLookAt(this.Position,
                                            this.Position + this.rotation.Forward,
                                            this.rotation.Up);
            
            mouseLastFrame = Mouse.GetState();
        }

        public void Update(GameTime gameTime)
        {
            posLastFrame = Position;
            Vector3 cameraPosition = new Vector3(0.0f, 0.0f, zoom);

            HandleInput(gameTime);

            // Rotate vertically
            cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateRotationX(verticalAngle));

            // Rotate horizontally
            cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateRotationZ(horizontalAngle));

            Position = cameraPosition + TargetPosition;

            this.LookAt(TargetPosition);

            // Compute view matrix
            this.View = Matrix.CreateLookAt(this.Position,
                                            this.Position + this.rotation.Forward,
                                            this.rotation.Up);
        }

        /// <summary>
        /// Points camera in direction of any position.
        /// </summary>
        /// <param name="targetPos">Target position for camera to face.</param>
        public void LookAt(Vector3 targetPos)
        {
            Vector3 newForward = targetPos - this.Position;
            newForward.Normalize();
            this.rotation.Forward = newForward;

            Vector3 referenceVector = Vector3.UnitZ;

            this.rotation.Right = Vector3.Cross(this.rotation.Forward, referenceVector);
            this.rotation.Up = Vector3.Cross(this.rotation.Right, this.rotation.Forward);
        }

        private void HandleInput(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();

            Zoom -= MathHelper.ToRadians(mouseState.ScrollWheelValue - mouseLastFrame.ScrollWheelValue);
            if(mouseState.RightButton == ButtonState.Pressed)
            {
                HorizontalAngle += MathHelper.ToRadians(mouseState.X - mouseLastFrame.X);
                VerticalAngle += MathHelper.ToRadians((mouseState.Y - mouseLastFrame.Y) * 0.5f);
            }

            mouseLastFrame = mouseState;
        }
    }
}
