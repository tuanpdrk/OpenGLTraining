using OpenCvSharp;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using DepthMapToMesh;
using System.Diagnostics;
using System;

namespace LearnOpenTK
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            string depthImagePath = "Resources/depth_map.jpg";

            var (vertices, indices) = GenerateVerticeAndMeshFromDepthMap(depthImagePath);

            using (var window = new MeshWindow(vertices, indices))
            {
                window.Run();
            }
        }

        private static (float[] points, int[] indices) GenerateVerticeAndMeshFromDepthMap(string filePath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Mat depthImage = Cv2.ImRead(filePath, ImreadModes.Grayscale);
            int width = depthImage.Width;
            int height = depthImage.Height;

            // Camera intrinsic parameters (giả định đơn giản)
            //float fx = width / 2.0f;
            //float fy = height / 2.0f;
            //float cx = width / 2.0f;
            //float cy = height / 2.0f;

            //var points = new List<Vector3>();

            //// Tạo point cloud
            //for (int y = 0; y < height; y++)
            //{
            //    for (int x = 0; x < width; x++)
            //    {
            //        byte depthValue = depthImage.At<byte>(y, x);
            //        float z = depthValue / 255.0f; // Normalize depth (0-1)

            //        if (z > 0) // Bỏ qua các điểm không hợp lệ
            //        {
            //            float x3D = (x - cx) * z / fx;
            //            float y3D = (y - cy) * z / fy;
            //            points.Add(new Vector3(x3D, y3D, -z));
            //        }
            //        else
            //        {
            //            points.Add(new Vector3(float.NaN, float.NaN, float.NaN)); // Placeholder
            //        }
            //    }
            //}

            float[] vertices = new float[width * height * 3]; // x, y, z per vertex

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float z = depthImage.At<float>(y, x); // Depth value
                    int index = (y * width + x) * 3;
                    vertices[index + 0] = x;    // X coordinate
                    vertices[index + 1] = y;    // Y coordinate
                    vertices[index + 2] = z;    // Z coordinate (depth)
                }
            }


            // Tạo mesh (tam giác)
            //for (int y = 0; y < height - 1; y++)
            //{
            //    for (int x = 0; x < width - 1; x++)
            //    {
            //        int topLeft = y * width + x;
            //        int topRight = y * width + (x + 1);
            //        int bottomLeft = (y + 1) * width + x;
            //        int bottomRight = (y + 1) * width + (x + 1);

            //        // Kiểm tra các điểm hợp lệ
            //        if (IsValid(points[topLeft]) && IsValid(points[topRight]) && IsValid(points[bottomLeft]))
            //        {
            //            indices.Add(topLeft);
            //            indices.Add(bottomLeft);
            //            indices.Add(topRight);
            //        }

            //        if (IsValid(points[topRight]) && IsValid(points[bottomLeft]) && IsValid(points[bottomRight]))
            //        {
            //            indices.Add(topRight);
            //            indices.Add(bottomLeft);
            //            indices.Add(bottomRight);
            //        }
            //    }
            //}

            int[] indices = new int[(width - 1) * (height - 1) * 6]; // 2 triangles per grid cell
            int idx = 0;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int topLeft = y * width + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (y + 1) * width + x;
                    int bottomRight = bottomLeft + 1;

                    // First triangle
                    indices[idx++] = topLeft;
                    indices[idx++] = bottomLeft;
                    indices[idx++] = topRight;

                    // Second triangle
                    indices[idx++] = topRight;
                    indices[idx++] = bottomLeft;
                    indices[idx++] = bottomRight;
                }
            }

            return (vertices, indices);
        }

        private static bool IsValid(Vector3 point)
        {
            return !float.IsNaN(point.X) && !float.IsNaN(point.Y) && !float.IsNaN(point.Z);
        }
    }
}
