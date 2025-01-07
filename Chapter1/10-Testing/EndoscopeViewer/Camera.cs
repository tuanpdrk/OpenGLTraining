using OpenTK;
using System;

public class Camera
{
    public Vector3 Position { get; private set; }
    public Vector3 Target { get; private set; }
    public Vector3 Up { get; private set; }

    public Camera(Vector3 position, Vector3 target, Vector3 up)
    {
        Position = position;
        Target = target;
        Up = up;
    }

    // Get the View Matrix based on the camera position and target
    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Target, Up);
    }

    // Get the Projection Matrix
    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), 1.33f, 0.1f, 100f);
    }
}
