using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenCvSharp;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

class Window_old : GameWindow
{
    private int _vao, _vbo;
    private readonly List<float> _vertices = new List<float>();
    private float _rotationX = 0.0f, _rotationY = 0.0f;
    private Vector2 _lastMousePos;
    private bool _isDragging = false;

    public Window_old(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // Load depth map and generate point cloud
        string depthImagePath = "Resources/depth_map.jpg"; // Replace with your depth map path
        //GeneratePointCloudFromDepthImage(depthImagePath);

        GeneratePointCloudAndMeshFromDepthMap(depthImagePath);

        // Create VAO and VBO
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0); // Position
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float)); // Color
        GL.EnableVertexAttribArray(1);

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
    }

    private void GeneratePointCloudFromDepthImage(string imagePath)
    {
        Mat depthImage = Cv2.ImRead(imagePath, ImreadModes.Grayscale);

        int width = depthImage.Width;
        int height = depthImage.Height;

        // Camera intrinsic parameters (example values, adjust accordingly)
        float fx = width / 2.0f; // Focal length in pixels (x-axis)
        float fy = height / 2.0f; // Focal length in pixels (y-axis)
        float cx = width / 2.0f; // Principal point x
        float cy = height / 2.0f; // Principal point y

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte depthValue = depthImage.At<byte>(y, x);
                float z = depthValue / 255.0f; // Normalize depth (0-1)

                if (z > 0) // Skip points with no depth
                {
                    float x3D = (x - cx) * z / fx;
                    float y3D = (y - cy) * z / fy;
                    float z3D = z;

                    // Add vertex position
                    _vertices.Add(x3D);
                    _vertices.Add(y3D);
                    _vertices.Add(-z3D); // Flip Z for OpenGL coordinate system

                    // Add vertex color (normalized depth as RGB)
                    _vertices.Add(z);
                    _vertices.Add(1.0f - z);
                    _vertices.Add(0.0f);
                }
            }
        }
    }

    private (List<Vector3> points, List<int> indices) GeneratePointCloudAndMeshFromDepthMap(string filePath)
    {
        // Đọc ảnh depth map
        Mat depthImage = Cv2.ImRead(filePath, ImreadModes.Grayscale);
        int width = depthImage.Width;
        int height = depthImage.Height;

        // Camera intrinsic parameters
        float fx = width / 2.0f;
        float fy = height / 2.0f;
        float cx = width / 2.0f;
        float cy = height / 2.0f;

        var points = new List<Vector3>();
        var indices = new List<int>();

        // Tạo point cloud
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte depthValue = depthImage.At<byte>(y, x);
                float z = depthValue / 255.0f; // Normalize depth (0-1)

                if (z > 0) // Bỏ qua các điểm không hợp lệ
                {
                    float x3D = (x - cx) * z / fx;
                    float y3D = (y - cy) * z / fy;
                    float z3D = z;

                    points.Add(new Vector3(x3D, y3D, -z3D)); // OpenGL coordinate system
                }
                else
                {
                    points.Add(new Vector3(float.NaN, float.NaN, float.NaN)); // Placeholder cho điểm không hợp lệ
                }
            }
        }

        // Tạo mesh (tam giác)
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int topLeft = y * width + x;
                int topRight = y * width + (x + 1);
                int bottomLeft = (y + 1) * width + x;
                int bottomRight = (y + 1) * width + (x + 1);

                // Kiểm tra các điểm hợp lệ
                if (IsValid(points[topLeft]) && IsValid(points[topRight]) && IsValid(points[bottomLeft]))
                {
                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);
                }

                if (IsValid(points[topRight]) && IsValid(points[bottomLeft]) && IsValid(points[bottomRight]))
                {
                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }
        }

        return (points, indices);
    }

    private bool IsValid(Vector3 point)
    {
        return !float.IsNaN(point.X) && !float.IsNaN(point.Y) && !float.IsNaN(point.Z);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.Button == MouseButton.Left)
        {
            _isDragging = true;
            //_lastMousePos = new Vector2(e.X, e.Y);
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (e.Button == MouseButton.Left)
        {
            _isDragging = false;
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        if (_isDragging)
        {
            Vector2 currentMousePos = new Vector2(e.X, e.Y);
            Vector2 delta = currentMousePos - _lastMousePos;

            _rotationX += delta.Y * 0.5f;
            _rotationY += delta.X * 0.5f;

            _lastMousePos = currentMousePos;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 model = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_rotationX)) *
                        Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_rotationY));
        Matrix4 view = Matrix4.LookAt(new Vector3(0, 0, 2), Vector3.Zero, Vector3.UnitY);
        //Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Width / (float)Height, 0.1f, 100.0f);

        //Matrix4 mvp = model * view * projection;
        int mvpLocation = GL.GetUniformLocation(0, "u_MVP");
        //GL.UniformMatrix4(mvpLocation, false, ref mvp);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Points, 0, _vertices.Count / 6);



        Context.SwapBuffers();
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }
}
