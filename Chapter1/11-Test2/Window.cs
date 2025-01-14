﻿using System;
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
using LearnOpenTK.Common;
using LearnOpenTK.Helpers;

public class Window : GameWindow
{
    private float[] _vertices;
    private int[] _indices;
    private int _vao, _vbo, _ebo;
    private readonly List<float> vertices = new List<float>();
    private float _rotationX = 0.0f, _rotationY = 0.0f;
    private Vector2 _lastMousePos;
    private bool _isDragging = false;

    private float[] _simplifiedVertices;
    private int[] _simplifiedIndices;

    private Matrix4 _model = Matrix4.Identity;
    private Matrix4 _view;
    private Matrix4 _projection;
    private Vector2 _lastMousePosition;

    private Camera _camera;
    private bool _firstMove = true;
    private Vector2 _lastPos;

    private Texture _texture;

    private Shader _lightingShader;
    private Shader _shader;

    private double _time;

    private readonly Vector3 _lightPos = new Vector3(1.2f, 1.0f, 2.0f);

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // Load depth map and generate point cloud
        string depthImagePath = "Resources/depth_map.jpg"; // Replace with your depth map path
        GenerateVerticesAndIndicesFromDepthImage(depthImagePath);
       
        // Create VAO and VBO
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vertices = vertices.ToArray();

        //Simplify vertices and indices
        Simplify(_vertices, _indices);

        if (!string.IsNullOrEmpty(depthImagePath) && _vertices.Length > 0 && _indices.Length > 0)
        {
            //RenderHelper.ExportToObjUsingAssimp(depthImagePath, _vertices, _indices);
        }

        if (_simplifiedVertices.Length > 0) { 
            _vertices = _simplifiedVertices;
        }

        if (_simplifiedIndices.Length > 0)
        {
            _indices = _simplifiedIndices;
        }

        // Set to Wireframe Mode
        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(uint), _vertices, BufferUsageHint.StaticDraw);

        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

        _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
        _shader.Use();

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        //_texture = Texture.LoadFromFile("Resources/image.jpg");
        _texture = Texture.LoadFromFile("Resources/container.png");
        _texture.Use(TextureUnit.Texture0);
        _shader.SetInt("texture0", 0);

        //Set background color
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        // Thiết lập ma trận view và projection
        _view = Matrix4.LookAt(new Vector3(0, 0, 2), Vector3.Zero, Vector3.UnitY);
        _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), Size.X / (float)Size.Y, 0.1f, 100f);

        _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);       
    }

    private void GenerateVerticesAndIndicesFromDepthImage(string imagePath)
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
                    vertices.Add(x3D);
                    vertices.Add(y3D);
                    vertices.Add(-z3D); // Flip Z for OpenGL coordinate system

                    // Add vertex color (normalized depth as RGB)
                    vertices.Add(z);
                    vertices.Add(1.0f - z);
                    vertices.Add(0.0f);
                }
            }
        }

        //Generate indices
        _indices = new int[(width - 1) * (height - 1) * 6]; // 2 triangles per grid cell
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
                _indices[idx++] = topLeft;
                _indices[idx++] = bottomLeft;
                _indices[idx++] = topRight;

                // Second triangle
                _indices[idx++] = topRight;
                _indices[idx++] = bottomLeft;
                _indices[idx++] = bottomRight;
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

    private void DrawSimplifiedWireframe(float[] vertices, int[] indices)
    {
        HashSet<(int, int)> uniqueEdges = new HashSet<(int, int)>();

        // Extract unique edges
        for (int i = 0; i < indices.Length; i += 3)
        {
            int v1 = indices[i];
            int v2 = indices[i + 1];
            int v3 = indices[i + 2];

            AddEdge(uniqueEdges, v1, v2);
            AddEdge(uniqueEdges, v2, v3);
            AddEdge(uniqueEdges, v3, v1);
        }

        // Create a list of unique edge vertices
        List<float> edgeVertices = new List<float>();
        foreach (var edge in uniqueEdges)
        {
            edgeVertices.Add(vertices[edge.Item1 * 3 + 0]);
            edgeVertices.Add(vertices[edge.Item1 * 3 + 1]);
            edgeVertices.Add(vertices[edge.Item1 * 3 + 2]);

            edgeVertices.Add(vertices[edge.Item2 * 3 + 0]);
            edgeVertices.Add(vertices[edge.Item2 * 3 + 1]);
            edgeVertices.Add(vertices[edge.Item2 * 3 + 2]);
        }

        // Upload edgeVertices to a buffer and draw as lines
        GL.BindVertexArray(_vao);
        GL.BufferData(BufferTarget.ArrayBuffer, edgeVertices.Count * sizeof(float), edgeVertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.DrawArrays(PrimitiveType.Lines, 0, edgeVertices.Count / 3);
    }

    void AddEdge(HashSet<(int, int)> edges, int v1, int v2)
    {
        if (v1 > v2) (v1, v2) = (v2, v1); // Ensure consistent ordering
        edges.Add((v1, v2));
    }

    public void Simplify(float[] vertices, int[] indices)
    {
        // Step 1: Create a mapping of unique vertices
        Dictionary<string, int> vertexMap = new Dictionary<string, int>();
        List<float> uniqueVertices = new List<float>();
        List<int> newIndices = new List<int>();

        int currentIndex = 0;

        for (int i = 0; i < indices.Length; i++)
        {
            // Get the index and corresponding vertex
            int vertexIndex = indices[i];
            int vertexStart = vertexIndex * 3;

            string vertexKey = $"{vertices[vertexStart]:F6},{vertices[vertexStart + 1]:F6},{vertices[vertexStart + 2]:F6}";

            // Check if the vertex is already in the map
            if (!vertexMap.TryGetValue(vertexKey, out int newIndex))
            {
                // Add the unique vertex to the list
                uniqueVertices.Add(vertices[vertexStart]);
                uniqueVertices.Add(vertices[vertexStart + 1]);
                uniqueVertices.Add(vertices[vertexStart + 2]);

                // Map the old index to the new index
                newIndex = currentIndex++;
                vertexMap[vertexKey] = newIndex;
            }

            // Add the new index to the new indices list
            newIndices.Add(newIndex);
        }

        // Step 2: Remove degenerate triangles (optional)
        List<int> validIndices = new List<int>();
        for (int i = 0; i < newIndices.Count; i += 3)
        {
            int i1 = newIndices[i];
            int i2 = newIndices[i + 1];
            int i3 = newIndices[i + 2];

            // Check for degenerate triangles (e.g., overlapping vertices)
            if (i1 != i2 && i2 != i3 && i3 != i1)
            {
                validIndices.Add(i1);
                validIndices.Add(i2);
                validIndices.Add(i3);
            }
        }

        // Return the simplified vertices and indices
        _simplifiedVertices = uniqueVertices.ToArray();
        _simplifiedIndices = validIndices.ToArray();
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

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        _time += 4.0 * args.Time;

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Sử dụng VAO và vẽ
        GL.BindVertexArray(_vao);

        _shader.Use();

        var model = Matrix4.Identity * Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(_time));
        _shader.SetMatrix4("model", model);
        _shader.SetMatrix4("view", _camera.GetViewMatrix());
        _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
                   

        GL.DrawElements(PrimitiveType.TriangleStrip, _indices.Length, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        // Dọn dẹp tài nguyên
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
        _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), Size.X / (float)Size.Y, 0.1f, 100f);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        var mouse = MouseState;



        var input = KeyboardState;
        if (input.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        const float cameraSpeed = 1.5f;
        const float sensitivity = 0.2f;

        if (input.IsKeyDown(Keys.W))
        {
            _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
        }
        if (input.IsKeyDown(Keys.S))
        {
            _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
        }
        if (input.IsKeyDown(Keys.A))
        {
            _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
        }
        if (input.IsKeyDown(Keys.D))
        {
            _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
        }
        if (input.IsKeyDown(Keys.Space))
        {
            _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
        }
        if (input.IsKeyDown(Keys.LeftShift))
        {
            _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
        }

        if (_firstMove)
        {
            _lastPos = new Vector2(mouse.X, mouse.Y);
            _firstMove = false;
        }
        else
        {
            var deltaX = mouse.X - _lastPos.X;
            var deltaY = mouse.Y - _lastPos.Y;
            _lastPos = new Vector2(mouse.X, mouse.Y);

            _camera.Yaw += deltaX * sensitivity;
            _camera.Pitch -= deltaY * sensitivity;
        }
    }


}
