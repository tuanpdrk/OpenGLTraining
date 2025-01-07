using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

public class MeshRenderer
{
    private int vbo, ebo;
    private int vertexCount;
    private Camera camera;

    public MeshRenderer(Camera camera)
    {
        this.camera = camera;
        InitializeOpenGL();
    }

    private void InitializeOpenGL()
    {
        GL.ClearColor(Color4.CornflowerBlue);
        GL.Enable(EnableCap.DepthTest);
    }

    // Method to render the mesh to screen
    public void Render(Mesh mesh)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Create Vertex Buffer Object (VBO)
        vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, mesh.Vertices.Count * Vector3.SizeInBytes, mesh.Vertices.ToArray(), BufferUsageHint.StaticDraw);

        // Create Element Buffer Object (EBO)
        ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Count * sizeof(int), mesh.Indices.ToArray(), BufferUsageHint.StaticDraw);

        // Enable vertex attributes (position)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(0);

        // Set up the camera projection
        Matrix4 projection = camera.GetProjectionMatrix();
        Matrix4 modelview = camera.GetViewMatrix();
        Matrix4 mvp = modelview * projection;

        // Set up shaders here (simplified for this example)
        GL.UseProgram(0); // Default shader program

        // Draw the mesh
        GL.DrawElements(PrimitiveType.Triangles, mesh.Indices.Count, DrawElementsType.UnsignedInt, 0);

        GL.DisableVertexAttribArray(0);
    }
}
