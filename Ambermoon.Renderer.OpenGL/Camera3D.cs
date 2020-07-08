using Ambermoon.Render;

namespace Ambermoon.Renderer.OpenGL
{
    internal class Camera3D : ICamera3D
    {
        readonly State state;
        readonly Matrix4 currentMatrix;
        float currentAngle = 0.0f;

        public Camera3D(State state)
        {
            this.state = state;
            currentMatrix = new Matrix4(Matrix4.Identity);
        }

        public void Activate()
        {
            state.RestoreModelViewMatrix(Matrix4.Identity);
            state.PushModelViewMatrix(currentMatrix);
        }

        private void Move(float x, float y, float z)
        {
            currentMatrix.Multiply(Matrix4.CreateTranslationMatrix(x, y, z));
        }

        private void Rotate(float angle)
        {
            currentMatrix.Multiply(Matrix4.CreateRotationMatrix(angle));
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
            Move(0.0f, 0.0f, distance);
        }

        public void MoveForward(float distance)
        {
            Move(0.0f, 0.0f, -distance);
        }

        public void SetPosition(float x, float y)
        {
            // Note: x and y are from top-down so x is real x and y is real z
            currentMatrix.Reset();
            Move(x, 0.0f, y);
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
            Rotate(currentAngle);
        }
    }
}
