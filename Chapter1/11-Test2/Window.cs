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

    private Matrix4 _model = Matrix4.Identity;
    private Matrix4 _view;
    private Matrix4 _projection;
    private Vector2 _lastMousePosition;

    private Camera _camera;
    private bool _firstMove = true;
    private Vector2 _lastPos;

    private Texture _texture;
    private Shader _shader;
    private double _time;

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

        //GeneratePointCloudAndMeshFromDepthMap(depthImagePath);

        // Set to Wireframe Mode
        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

        

        // Create VAO and VBO
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vertices = vertices.ToArray();

        if (!string.IsNullOrEmpty(depthImagePath) && _vertices.Length > 0 && _indices.Length > 0)
        {
            //RenderHelper.ExportToObjUsingAssimp(depthImagePath, _vertices, _indices);
        }

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

        //_shader.SetInt("texture0", 0);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        // Thiết lập ma trận view và projection
        _view = Matrix4.LookAt(new Vector3(0, 0, 2), Vector3.Zero, Vector3.UnitY);
        _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), Size.X / (float)Size.Y, 0.1f, 100f);

        _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

        //Generate to 3d physical file
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

        _shader.SetMatrix4("model", Matrix4.Identity);
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
