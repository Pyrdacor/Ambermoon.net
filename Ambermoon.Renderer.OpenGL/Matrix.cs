/*
 * Matrix.cs - Basic 4x4 matrix implementation
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Ambermoon.Renderer
{
    public class Matrix4
    {
        readonly float[] matrix = new float[16];
        float[] inverse = null;

        public static readonly Matrix4 Identity = new Matrix4(new float[16]
        {
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        });

        public static Matrix4 CreateOrtho2D(float left, float right, float top, float bottom, float near = -1.0f, float far = 1.0f)
        {
            // width
            float w = right - left;
            // height
            float h = top - bottom; // swap y so 0,0 for drawing is in the upper-left corner
            // depth
            float d = far - near;

            return new Matrix4(new float[16]
            {
                2.0f / w,   0.0f,       0.0f,       -(right + left) / w,
                0.0f,       2.0f / h,   0.0f,       -(bottom + top) / h,
                0.0f,       0.0f,       2.0f / d,   -(far + near) / d,
                0.0f,       0.0f,       0.0f,       1.0f
            });
        }

        public static Matrix4 CreatePerspective(float fovY, float aspect, float near, float far)
        {
            if (fovY <= 0.0f || fovY >= 180.0f)
                throw new ArgumentException("The field of view y-angle was outside the valid range of 0 < fovAngle < 180.");

            if (Math.Abs(aspect) <= float.Epsilon)
                throw new ArgumentException("Aspect is 0 which is not allowed.");

            if (near <= float.Epsilon)
                throw new ArgumentException("Near z value is 0 or smaller which is not allowed.");

            if (far < near)
                throw new ArgumentException("Far z value is smaller than near z value which is not allowed.");

            if (far - near <= float.Epsilon)
                throw new ArgumentException("Near z value equals far z value or far is smaller than near which is not allowed.");

            float scale = near * (float)Math.Tan(0.5 * fovY);

            float t = scale; // top
            float b = -t; // bottom
            float r = aspect * t; // right
            float l = -r; // left
            float w = r - l; // width
            float h = t - b; // height

            // Ambermoon uses a scaling factor of 256/(256+distance).
            // This can be expressed as 1/(1+distance/256).
            // We scale the 256 down to the near value so that
            // we end up with near/(near+distance*near/256).
            // We do so to avoid clipping through walls.
            // As x, y and z all use the same scaled units which is
            // Global.DistancePerBlock we don't need to care about
            // the scaling of the distance. So we can just use
            // near/(near+distance) in the projection matrix.
            // This projection matrix exactly scales x and y by
            // 256/(256+distance) with the given FOV applied.
            return new Matrix4(new float[16]
            {
                2.0f * near / w,    0.0f,               (r + l) / w,                    0.0f,
                0.0f,               2.0f * near / h,    (t + b) / h,                    0.0f,
                0.0f,               0.0f,               -(2.0f*near+far)/(far-near),    -(2.0f*near*(far+near))/(far-near)-near,
                0.0f,               0.0f,               -1.0f,                          near
            });
        }

        public static Matrix4 CreateTranslationMatrix(float x, float y, float z = 0.0f)
        {
            return new Matrix4(new float[16]
            {
                1.0f, 0.0f, 0.0f, x,
                0.0f, 1.0f, 0.0f, y,
                0.0f, 0.0f, 1.0f, z,
                0.0f, 0.0f, 0.0f, 1.0f
            });
        }

        public static Matrix4 CreateScalingMatrix(float x, float y, float z = 1.0f)
        {
            return new Matrix4(new float[16]
            {
                x,    0.0f, 0.0f, 0.0f,
                0.0f, y,    0.0f, 0.0f,
                0.0f, 0.0f, z,    0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            });
        }

        public static Matrix4 CreateYRotationMatrix(float angle)
        {
            const float deg2rad = (float)(Math.PI / 180.0);

            var sin = (float)Math.Sin(angle * deg2rad);
            var cos = (float)Math.Cos(angle * deg2rad);

            return new Matrix4(new float[16]
            {
                cos,  -sin, 0.0f, 0.0f,
                sin,  cos,  0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            });
        }

        public static Matrix4 CreateZRotationMatrix(float angle)
        {
            const float deg2rad = (float)(Math.PI / 180.0);

            var sin = (float)Math.Sin(angle * deg2rad);
            var cos = (float)Math.Cos(angle * deg2rad);

            return new Matrix4(new float[16]
            {
                cos,  0.0f, sin,  0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                -sin, 0.0f, cos,  0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            });
        }

        public Matrix4 CreateInverseMatrix()
        {
            if (inverse != null)
            {
                var inverseMatrix = new Matrix4(inverse);

                inverseMatrix.inverse = new float[16];
                Array.Copy(matrix, inverseMatrix.inverse, 16);

                return inverseMatrix;
            }

            float[] inv = new float[16];
            float det;
            int i;

            inv[0] = matrix[5] * matrix[10] * matrix[15] -
                     matrix[5] * matrix[11] * matrix[14] -
                     matrix[9] * matrix[6] * matrix[15] +
                     matrix[9] * matrix[7] * matrix[14] +
                     matrix[13] * matrix[6] * matrix[11] -
                     matrix[13] * matrix[7] * matrix[10];

            inv[4] = -matrix[4] * matrix[10] * matrix[15] +
                      matrix[4] * matrix[11] * matrix[14] +
                      matrix[8] * matrix[6] * matrix[15] -
                      matrix[8] * matrix[7] * matrix[14] -
                      matrix[12] * matrix[6] * matrix[11] +
                      matrix[12] * matrix[7] * matrix[10];

            inv[8] = matrix[4] * matrix[9] * matrix[15] -
                     matrix[4] * matrix[11] * matrix[13] -
                     matrix[8] * matrix[5] * matrix[15] +
                     matrix[8] * matrix[7] * matrix[13] +
                     matrix[12] * matrix[5] * matrix[11] -
                     matrix[12] * matrix[7] * matrix[9];

            inv[12] = -matrix[4] * matrix[9] * matrix[14] +
                       matrix[4] * matrix[10] * matrix[13] +
                       matrix[8] * matrix[5] * matrix[14] -
                       matrix[8] * matrix[6] * matrix[13] -
                       matrix[12] * matrix[5] * matrix[10] +
                       matrix[12] * matrix[6] * matrix[9];

            inv[1] = -matrix[1] * matrix[10] * matrix[15] +
                      matrix[1] * matrix[11] * matrix[14] +
                      matrix[9] * matrix[2] * matrix[15] -
                      matrix[9] * matrix[3] * matrix[14] -
                      matrix[13] * matrix[2] * matrix[11] +
                      matrix[13] * matrix[3] * matrix[10];

            inv[5] = matrix[0] * matrix[10] * matrix[15] -
                     matrix[0] * matrix[11] * matrix[14] -
                     matrix[8] * matrix[2] * matrix[15] +
                     matrix[8] * matrix[3] * matrix[14] +
                     matrix[12] * matrix[2] * matrix[11] -
                     matrix[12] * matrix[3] * matrix[10];

            inv[9] = -matrix[0] * matrix[9] * matrix[15] +
                      matrix[0] * matrix[11] * matrix[13] +
                      matrix[8] * matrix[1] * matrix[15] -
                      matrix[8] * matrix[3] * matrix[13] -
                      matrix[12] * matrix[1] * matrix[11] +
                      matrix[12] * matrix[3] * matrix[9];

            inv[13] = matrix[0] * matrix[9] * matrix[14] -
                      matrix[0] * matrix[10] * matrix[13] -
                      matrix[8] * matrix[1] * matrix[14] +
                      matrix[8] * matrix[2] * matrix[13] +
                      matrix[12] * matrix[1] * matrix[10] -
                      matrix[12] * matrix[2] * matrix[9];

            inv[2] = matrix[1] * matrix[6] * matrix[15] -
                     matrix[1] * matrix[7] * matrix[14] -
                     matrix[5] * matrix[2] * matrix[15] +
                     matrix[5] * matrix[3] * matrix[14] +
                     matrix[13] * matrix[2] * matrix[7] -
                     matrix[13] * matrix[3] * matrix[6];

            inv[6] = -matrix[0] * matrix[6] * matrix[15] +
                      matrix[0] * matrix[7] * matrix[14] +
                      matrix[4] * matrix[2] * matrix[15] -
                      matrix[4] * matrix[3] * matrix[14] -
                      matrix[12] * matrix[2] * matrix[7] +
                      matrix[12] * matrix[3] * matrix[6];

            inv[10] = matrix[0] * matrix[5] * matrix[15] -
                      matrix[0] * matrix[7] * matrix[13] -
                      matrix[4] * matrix[1] * matrix[15] +
                      matrix[4] * matrix[3] * matrix[13] +
                      matrix[12] * matrix[1] * matrix[7] -
                      matrix[12] * matrix[3] * matrix[5];

            inv[14] = -matrix[0] * matrix[5] * matrix[14] +
                       matrix[0] * matrix[6] * matrix[13] +
                       matrix[4] * matrix[1] * matrix[14] -
                       matrix[4] * matrix[2] * matrix[13] -
                       matrix[12] * matrix[1] * matrix[6] +
                       matrix[12] * matrix[2] * matrix[5];

            inv[3] = -matrix[1] * matrix[6] * matrix[11] +
                      matrix[1] * matrix[7] * matrix[10] +
                      matrix[5] * matrix[2] * matrix[11] -
                      matrix[5] * matrix[3] * matrix[10] -
                      matrix[9] * matrix[2] * matrix[7] +
                      matrix[9] * matrix[3] * matrix[6];

            inv[7] = matrix[0] * matrix[6] * matrix[11] -
                     matrix[0] * matrix[7] * matrix[10] -
                     matrix[4] * matrix[2] * matrix[11] +
                     matrix[4] * matrix[3] * matrix[10] +
                     matrix[8] * matrix[2] * matrix[7] -
                     matrix[8] * matrix[3] * matrix[6];

            inv[11] = -matrix[0] * matrix[5] * matrix[11] +
                       matrix[0] * matrix[7] * matrix[9] +
                       matrix[4] * matrix[1] * matrix[11] -
                       matrix[4] * matrix[3] * matrix[9] -
                       matrix[8] * matrix[1] * matrix[7] +
                       matrix[8] * matrix[3] * matrix[5];

            inv[15] = matrix[0] * matrix[5] * matrix[10] -
                      matrix[0] * matrix[6] * matrix[9] -
                      matrix[4] * matrix[1] * matrix[10] +
                      matrix[4] * matrix[2] * matrix[9] +
                      matrix[8] * matrix[1] * matrix[6] -
                      matrix[8] * matrix[2] * matrix[5];

            det = matrix[0] * inv[0] + matrix[1] * inv[4] + matrix[2] * inv[8] + matrix[3] * inv[12];

            if (det == 0.0f)
                return null;

            det = 1.0f / det;

            for (i = 0; i < 16; i++)
                inv[i] = inv[i] * det;

            inverse = inv;

            {
                var inverseMatrix = new Matrix4(inverse);

                inverseMatrix.inverse = new float[16];
                Array.Copy(matrix, inverseMatrix.inverse, 16);

                return inverseMatrix;
            }
        }

        public Matrix4(float[] matrix)
        {
            if (matrix.Length != 16)
                throw new Exception("Invalid matrix size.");

            this.matrix = matrix;
        }

        public Matrix4(Matrix4 matrix)
        {
            Buffer.BlockCopy(matrix.matrix, 0, this.matrix, 0, 16 * sizeof(float));
        }

        public bool EqualTo(Matrix4 matrix)
        {
            for (int i = 0; i < 16; ++i)
            {
                if (!Misc.FloatEqual(this.matrix[i], matrix.matrix[i]))
                    return false;
            }

            return true;
        }

        public float[] ToArray()
        {
            return matrix;
        }

        public void Reset()
        {
            for (int i = 0; i < 16; ++i)
                this.matrix[i] = Identity.matrix[i];
        }

        public void Multiply(Matrix4 matrix)
        {
            var multipliedMatrix = this * matrix;

            for (int i = 0; i < 16; ++i)
                this.matrix[i] = multipliedMatrix.matrix[i];
        }

        public static Matrix4 Multiply(Matrix4 matrix1, Matrix4 matrix2)
        {
            float ax1 = matrix1.matrix[0];
            float ax2 = matrix1.matrix[1];
            float ax3 = matrix1.matrix[2];
            float ax4 = matrix1.matrix[3];
            float ay1 = matrix1.matrix[4];
            float ay2 = matrix1.matrix[5];
            float ay3 = matrix1.matrix[6];
            float ay4 = matrix1.matrix[7];
            float az1 = matrix1.matrix[8];
            float az2 = matrix1.matrix[9];
            float az3 = matrix1.matrix[10];
            float az4 = matrix1.matrix[11];
            float aw1 = matrix1.matrix[12];
            float aw2 = matrix1.matrix[13];
            float aw3 = matrix1.matrix[14];
            float aw4 = matrix1.matrix[15];

            float bx1 = matrix2.matrix[0];
            float bx2 = matrix2.matrix[1];
            float bx3 = matrix2.matrix[2];
            float bx4 = matrix2.matrix[3];
            float by1 = matrix2.matrix[4];
            float by2 = matrix2.matrix[5];
            float by3 = matrix2.matrix[6];
            float by4 = matrix2.matrix[7];
            float bz1 = matrix2.matrix[8];
            float bz2 = matrix2.matrix[9];
            float bz3 = matrix2.matrix[10];
            float bz4 = matrix2.matrix[11];
            float bw1 = matrix2.matrix[12];
            float bw2 = matrix2.matrix[13];
            float bw3 = matrix2.matrix[14];
            float bw4 = matrix2.matrix[15];

            return new Matrix4(new float[16]
            {
                ax1*bx1+ax2*by1+ax3*bz1+ax4*bw1,
                ax1*bx2+ax2*by2+ax3*bz2+ax4*bw2,
                ax1*bx3+ax2*by3+ax3*bz3+ax4*bw3,
                ax1*bx4+ax2*by4+ax3*bz4+ax4*bw4,

                ay1*bx1+ay2*by1+ay3*bz1+ay4*bw1,
                ay1*bx2+ay2*by2+ay3*bz2+ay4*bw2,
                ay1*bx3+ay2*by3+ay3*bz3+ay4*bw3,
                ay1*bx4+ay2*by4+ay3*bz4+ay4*bw4,

                az1*bx1+az2*by1+az3*bz1+az4*bw1,
                az1*bx2+az2*by2+az3*bz2+az4*bw2,
                az1*bx3+az2*by3+az3*bz3+az4*bw3,
                az1*bx4+az2*by4+az3*bz4+az4*bw4,

                aw1*bx1+aw2*by1+aw3*bz1+aw4*bw1,
                aw1*bx2+aw2*by2+aw3*bz2+aw4*bw2,
                aw1*bx3+aw2*by3+aw3*bz3+aw4*bw3,
                aw1*bx4+aw2*by4+aw3*bz4+aw4*bw4
            });
        }

        public static Matrix4 operator *(Matrix4 matrix1, Matrix4 matrix2)
        {
            return Multiply(matrix1, matrix2);
        }

        public void MultiplyVector(ref float x, ref float y, ref float z)
        {
            var newX = matrix[0] * x + matrix[1] * y + matrix[2] * z + matrix[3];
            var newY = matrix[4] * x + matrix[5] * y + matrix[6] * z + matrix[7];
            var newZ = matrix[8] * x + matrix[9] * y + matrix[10] * z + matrix[11];

            x = newX;
            y = newY;
            z = newZ;
        }
    }
}
