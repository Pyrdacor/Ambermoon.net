using Ambermoon.Render;
using System;
using System.Linq;

namespace Ambermoon.Renderer.OpenGL
{
    internal class Camera3D : ICamera3D
    {
        const double AngleFactor = Math.PI / 180.0;
        const double QuarterTurnAngle = 0.5 * Math.PI;
        readonly State state;
        readonly Matrix4 currentMatrix;
        Matrix4 rotationMatrix = new Matrix4(Matrix4.Identity);
        Matrix4 translateMatrix = new Matrix4(Matrix4.Identity);
        float currentAngle = 0.0f;
        double currentAngleCos = 0.0;
        double currentAngleSin = -1.0;
        // The perpendicular value is -90° rotation (= turn left by a quarter)
        double currentPerpendicularAngleCos = -1.0;
        double currentPerpendicularAngleSin = 0.0;

        public Camera3D(State state)
        {
            this.state = state;
            currentMatrix = new Matrix4(Matrix4.Identity);
        }

        public float X { get; private set; } = 0.0f;
        public float Y { get; private set; } = 0.0f;
        public float Z { get; private set; } = 0.0f;
        public float GroundY { get; set; } = 0.0f;

        Action<float> turnedHandler;

        public event Action<float> Turned
        {
            add
            {
                if (turnedHandler == null || !turnedHandler.GetInvocationList().Contains(value))
                    turnedHandler += value;
            }
            remove
            {
                turnedHandler -= value;
            }
        }

        public void GetForwardPosition(float distance, out float x, out float z, bool noX, bool noZ)
        {
            x = noX ? X : X - (float)currentAngleCos * distance;
            z = noZ ? Z : Z - (float)currentAngleSin * distance;
        }

        public void GetBackwardPosition(float distance, out float x, out float z, bool noX, bool noZ)
        {
            x = noX ? X : X + (float)currentAngleCos * distance;
            z = noZ ? Z : Z + (float)currentAngleSin * distance;
        }

        public void GetLeftPosition(float distance, out float x, out float z, bool noX, bool noZ)
        {
            x = noX ? X : X - (float)currentPerpendicularAngleCos * distance;
            z = noZ ? Z : Z - (float)currentPerpendicularAngleSin * distance;
        }

        public void GetRightPosition(float distance, out float x, out float z, bool noX, bool noZ)
        {
            x = noX ? X : X + (float)currentPerpendicularAngleCos * distance;
            z = noZ ? Z : Z + (float)currentPerpendicularAngleSin * distance;
        }

        public void Activate()
        {
            state.RestoreModelViewMatrix(Matrix4.Identity);
            state.PushModelViewMatrix(currentMatrix);
        }

        private void UpdateMatrix()
        {
            currentMatrix.Reset();
            currentMatrix.Multiply(rotationMatrix);
            currentMatrix.Multiply(translateMatrix);
        }

        private void Move(float x, float y, float z)
        {
            X += x;
            Y += y;
            Z += z;
            translateMatrix = Matrix4.CreateTranslationMatrix(X, Y, Z);
            UpdateMatrix();
        }

        private void Rotate(float angle)
        {
            rotationMatrix = Matrix4.CreateZRotationMatrix(angle);
            UpdateMatrix();
        }

        public void LevitateDown(float distance)
        {
            Move(0.0f, distance, 0.0f);
        }

        public void LevitateUp(float distance)
        {
            Move(0.0f, -distance, 0.0f);
        }

        public void MoveBackward(float distance, bool noX, bool noZ)
        {
            Move(noX ? 0.0f : (float)currentAngleCos * distance, 0.0f, noZ ? 0.0f : (float)currentAngleSin * distance);
        }

        public void MoveForward(float distance, bool noX, bool noZ)
        {
            Move(noX ? 0.0f : -(float)currentAngleCos * distance, 0.0f, noZ ? 0.0f : -(float)currentAngleSin * distance);
        }

        public void MoveLeft(float distance, bool noX, bool noZ)
        {
            Move(noX ? 0.0f : -(float)currentPerpendicularAngleCos * distance, 0.0f, noZ ? 0.0f : -(float)currentPerpendicularAngleSin * distance);
        }

        public void MoveRight(float distance, bool noX, bool noZ)
        {
            Move(noX ? 0.0f : (float)currentPerpendicularAngleCos * distance, 0.0f, noZ ? 0.0f : (float)currentPerpendicularAngleSin * distance);
        }

        public void UpdatePosition()
        {
            Y = GroundY;
            translateMatrix = Matrix4.CreateTranslationMatrix(X, Y, Z);
            TurnTowards(currentAngle);
            UpdateMatrix();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SetPosition(float x, float z, float? y = null)
        {
            X = -x;
            Y = y ?? GroundY;
            Z = z;
            translateMatrix = Matrix4.CreateTranslationMatrix(X, Y, Z);
            UpdateMatrix();
        }

        public void TurnLeft(float angle)
        {
            TurnTowards(currentAngle - angle);
        }

        public void TurnRight(float angle)
        {
            TurnTowards(currentAngle + angle);
        }

        public void TurnTowards(float angle)
        {
            while (angle < 0.0f)
                angle += 360.0f;
            while (angle >= 360.0f)
                angle -= 360.0f;

            currentAngle = angle;
            var radiant = AngleFactor * (currentAngle - 90.0f);
            currentAngleCos = Math.Cos(radiant);
            currentAngleSin = Math.Sin(radiant);
            currentPerpendicularAngleCos = Math.Cos(radiant - QuarterTurnAngle);
            currentPerpendicularAngleSin = Math.Sin(radiant - QuarterTurnAngle);
            Rotate(currentAngle);

            turnedHandler?.Invoke(currentAngle);
        }
    }
}
