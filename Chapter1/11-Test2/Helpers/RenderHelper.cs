using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;

namespace LearnOpenTK.Helpers
{
    public static class RenderHelper
    {
        public static void ExportToObjUsingAssimp(string filePath, float[] vertices, int[] indices)
        {
            Scene scene = new Scene();

            // Create a mesh
            Mesh mesh = new Mesh(PrimitiveType.Triangle);
            for (int i = 0; i < vertices.Length; i += 3)
            {
                mesh.Vertices.Add(new Vector3D(vertices[i], vertices[i + 1], vertices[i + 2]));
            }
            for (int i = 0; i < indices.Length; i += 3)
            {
                Face face = new Face(new int[] { indices[i], indices[i + 1], indices[i + 2] });
                mesh.Faces.Add(face);
            }

            // Add the mesh to the scene
            scene.Meshes.Add(mesh);
            scene.RootNode = new Node("Root");
            scene.RootNode.MeshIndices.Add(0);

            // Export the scene
            AssimpContext context = new AssimpContext();

            if (scene.Meshes.Count == 0 || scene.Meshes[0].Vertices.Count == 0)
            {
                throw new InvalidOperationException("Scene contains no valid mesh data!");
            }

            var formats = context.GetSupportedExportFormats();
            foreach (var format in formats)
            {
                Console.WriteLine($"Extension: {format.FileExtension}, Description: {format.Description}");
            }

            context.ExportFile(scene, filePath, "obj");


        }

        
    }
}
