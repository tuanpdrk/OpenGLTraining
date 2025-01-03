using OpenCvSharp;
using System.Collections.Generic;

public class DepthImageProcessor
{
    public static List<float[]> GenerateMeshFromDepth(string depthImagePath, float scaleFactor = 0.1f)
    {
        // Load depth image
        Mat depthMat = Cv2.ImRead(depthImagePath, ImreadModes.Grayscale);
        int rows = depthMat.Rows;
        int cols = depthMat.Cols;

        List<float[]> vertices = new List<float[]>();

        // Generate mesh vertices
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                byte depthValue = depthMat.At<byte>(y, x);
                float z = depthValue * scaleFactor; // Scale depth
                vertices.Add(new float[] { x, -y, -z }); // OpenGL coordinate system
            }
        }
        return vertices;
    }

    public static List<float[]> GenerateTriangleStripFromDepth(string depthImagePath, float scaleFactor = 0.1f)
    {
        // Load depth image
        Mat depthMat = Cv2.ImRead(depthImagePath, ImreadModes.Grayscale);
        int rows = depthMat.Rows;
        int cols = depthMat.Cols;

        List<float[]> vertices = new List<float[]>();

        // Create triangle strips row by row
        for (int y = 0; y < rows - 1; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                // Add vertex from current row
                byte depthValue = depthMat.At<byte>(y, x);
                float z1 = depthValue * scaleFactor;
                vertices.Add(new float[] { x, -y, -z1 });

                // Add vertex from next row
                depthValue = depthMat.At<byte>(y + 1, x);
                float z2 = depthValue * scaleFactor;
                vertices.Add(new float[] { x, -(y + 1), -z2 });
            }
        }
        return vertices;
    }
}
