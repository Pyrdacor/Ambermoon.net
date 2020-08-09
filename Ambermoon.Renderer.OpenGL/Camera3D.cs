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
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        public Camera3D(State state)
        {
            this.state = state;
            currentMatrix = new Matrix4(Matrix4.Identity);
        }

        static Position CoordinatesToPosition(float x, float z) =>
            new Position(Misc.Round((-x - 0.5f * Global.DistancePerTile) / Global.DistancePerTile),
                Misc.Round((z + 0.5f * Global.DistancePerTile) / Global.DistancePerTile));

        public Position Position => CoordinatesToPosition(x, z);

        public Position GetForwardPosition(float distance)
        {
            return CoordinatesToPosition
            (
                x - (float)currentAngleCos * distance,
                z - (float)currentAngleSin * distance
            );
        }

        public Position GetBackwardPosition(float distance)
        {
            return CoordinatesToPosition
            (
                x + (float)currentAngleCos * distance,
                z + (float)currentAngleSin * distance
            );
        }

        public void Activate()
        {
            state.RestoreModelViewMatrix(Matrix4.Identity);
            state.PushModelViewMatrix(currentMatrix);
        }

        public void ActivateBillboards(Billboard3DShader shader)
        {
            shader.SetCameraPosition(x, y, z);
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
            this.x += x;
            this.y += y;
            this.z += z;
            translateMatrix = Matrix4.CreateTranslationMatrix(this.x, this.y, this.z);
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

        public void MoveBackward(float distance)
        {
            Move((float)currentAngleCos * distance, 0.0f, (float)currentAngleSin * distance);
        }

        public void MoveForward(float distance)
        {
            Move(-(float)currentAngleCos * distance, 0.0f, -(float)currentAngleSin * distance);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SetPosition(float x, float z)
        {
            this.x = -x;
            this.y = -1.0f;
            this.z = z;
            translateMatrix = Matrix4.CreateTranslationMatrix(this.x, this.y, this.z);
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
