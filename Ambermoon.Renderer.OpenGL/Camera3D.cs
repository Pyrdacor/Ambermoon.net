using Ambermoon.Render;
using System;

namespace Ambermoon.Renderer.OpenGL
{
    internal class Camera3D : ICamera3D
    {
        const double AngleFactor = Math.PI / 180.0;
        readonly State state;
        readonly Matrix4 currentMatrix;
        Matrix4 rotationMatrix = new Matrix4(Matrix4.Identity);
        Matrix4 translateMatrix = new Matrix4(Matrix4.Identity);
        float currentAngle = 0.0f;
        double currentAngleCos = 0.0;
        double currentAngleSin = -1.0;

        public Camera3D(State state)
        {
            this.state = state;
            currentMatrix = new Matrix4(Matrix4.Identity);
        }

        public float X { get; private set; } = 0.0f;
        public float Y { get; private set; } = 0.0f;
        public float Z { get; private set; } = 0.0f;
        public float GroundY { get; set; } = 0.0f;

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

        public void Activate()
        {
            state.RestoreModelViewMatrix(Matrix4.Identity);
            state.PushModelViewMatrix(currentMatrix);
        }

        public void ActivateBillboards(Billboard3DShader shader)
        {
            shader.SetCameraPosition(X, Y, Z);
            shader.SetCameraDirection((float)currentAngleCos, 0.0f, (float)currentAngleSin);
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
            Move(0.0f, -distance, 0.0f);
        }

        public void LevitateUp(float distance)
        {
            Move(0.0f, distance, 0.0f);
        }

        public void MoveBackward(float distance, bool noX, bool noZ)
        {
            Move(noX ? 0.0f : (float)currentAngleCos * distance, 0.0f, noZ ? 0.0f : (float)currentAngleSin * distance);
        }

        public void MoveForward(float distance, bool noX, bool noZ)
        {
            Move(noX ? 0.0f : -(float)currentAngleCos * distance, 0.0f, noZ ? 0.0f : -(float)currentAngleSin * distance);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SetPosition(float x, float z)
        {
            X = -x;
            Y = GroundY;
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
            currentAngle = angle;
            var radiant = AngleFactor * (currentAngle - 90.0f);
            currentAngleCos = Math.Cos(radiant);
            currentAngleSin = Math.Sin(radiant);
            Rotate(currentAngle);
        }
    }
}
