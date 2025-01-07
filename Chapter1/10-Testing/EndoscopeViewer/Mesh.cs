using OpenTK;
using System;
using System.Collections.Generic;

public class Mesh
{
    public List<Vector3> Vertices { get; private set; }
    public List<int> Indices { get; private set; }

    public Mesh(List<Vector3> vertices, List<int> indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}
