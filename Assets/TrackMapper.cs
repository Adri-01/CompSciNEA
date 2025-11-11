using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TrackMapper : MonoBehaviour
{
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Mesh _mesh;

    public TextAsset waypointFile; // Assign this in Unity Editor
    public Material[] redWhiteMaterial; // Assign red-white alternating material in Unity
    public Material roadMaterial; // Assign blue-white alternating material in

    private List<Vector2> centreWaypoints = new List<Vector2>();
    private List<Vector2> trackWidths = new List<Vector2>();

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    private int textureIndex = 1; // Switch between red and white

    void Start()
    {
        if (waypointFile != null)
        {
            LoadFile();
            GenerateBorderMesh();
            GenerateFirstBorderMeshes();
            GenerateTrackMesh();
        }
    }

    void LoadFile()
    {
        string[] lines = waypointFile.text.Split('\n');
        foreach (string line in lines)
        {
            string[] values = line.Split(',');

            if (values.Length >= 4)
            {
                if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y))
                {
                    centreWaypoints.Add(new Vector2(x, y));
                }
                if (float.TryParse(values[2], out float R) && float.TryParse(values[3], out float L))
                {
                    trackWidths.Add(new Vector2(R, L));
                }
            }
        }
    }

    void GenerateBorderMesh()
    {
        _mesh = new Mesh();
        _mesh.name = "Left Border Mesh";

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshCollider = gameObject.AddComponent<MeshCollider>();

        Vector2 lastTrackLeft = new Vector2();
        Vector2 lastFarTrackLeft = new Vector2();
        Vector2 lastTrackRight = new Vector2();
        Vector2 lastFarTrackRight = new Vector2();

        for (int i = 0; i < centreWaypoints.Count; i++)
        {
            int nextIndex = (i + 1) % centreWaypoints.Count;

            float dx = centreWaypoints[nextIndex].x - centreWaypoints[i].x;
            float dy = centreWaypoints[nextIndex].y - centreWaypoints[i].y;

            float length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length == 0) continue;

            float unit_x = dx / length;
            float unit_y = dy / length;

            float perp_x = -unit_y;
            float perp_y = unit_x;

            // Left-side calculations
            Vector2 currentTrackLeft = new Vector2(
                centreWaypoints[i].x + perp_x * trackWidths[i].y,
                centreWaypoints[i].y + perp_y * trackWidths[i].y
            );

            Vector2 currentFarTrackLeft = new Vector2(
                centreWaypoints[i].x + perp_x * (trackWidths[i].y + 1),
                centreWaypoints[i].y + perp_y * (trackWidths[i].y + 1)
            );

            if (i > 0 || centreWaypoints.Count == 2) AddCube(lastTrackLeft, currentTrackLeft, lastFarTrackLeft, currentFarTrackLeft, true);


            // Right-side calculations
            Vector2 currentTrackRight = new Vector2(
                centreWaypoints[i].x - perp_x * trackWidths[i].y,
                centreWaypoints[i].y - perp_y * trackWidths[i].y
            );

            Vector2 currentFarTrackRight = new Vector2(
                centreWaypoints[i].x - perp_x * (trackWidths[i].y + 1),
                centreWaypoints[i].y - perp_y * (trackWidths[i].y + 1)
            );

            if (i > 0 || centreWaypoints.Count == 2) {
                AddCube(currentTrackRight, lastTrackRight, currentFarTrackRight, lastFarTrackRight, false);
                AddTrackMesh(lastTrackLeft, lastTrackRight, currentTrackLeft, currentTrackRight);
            }

            lastTrackRight = currentTrackRight;
            lastFarTrackRight = currentFarTrackRight;
            lastTrackLeft = currentTrackLeft;
            lastFarTrackLeft = currentFarTrackLeft;

        }

        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;
    }
    void GenerateFirstBorderMeshes()
    {
        float dx = centreWaypoints[0].x - centreWaypoints.Last().x;
        float dy = centreWaypoints[0].y - centreWaypoints.Last().y;

        float length = Mathf.Sqrt(dx * dx + dy * dy);

        float unit_x = dx / length;
        float unit_y = dy / length;

        float perp_x = -unit_y;
        float perp_y = unit_x;

        // Left-side calculations
        Vector2 bottomLeft = new Vector2(
            centreWaypoints.Last().x + perp_x * trackWidths.Last().y,
            centreWaypoints.Last().y + perp_y * trackWidths.Last().y
        );

        Vector2 FarTrackLeft = new Vector2(
            centreWaypoints.Last().x + perp_x * (trackWidths.Last().y + 1),
            centreWaypoints.Last().y + perp_y * (trackWidths.Last().y + 1)
        );
        Vector2 bottomRight = new Vector2(
            centreWaypoints[0].x + perp_x * trackWidths[0].y,
            centreWaypoints[0].y + perp_y * trackWidths[0].y
        );

        Vector2 FarTrackRight = new Vector2(
            centreWaypoints[0].x + perp_x * (trackWidths[0].y + 1),
            centreWaypoints[0].y + perp_y * (trackWidths[0].y + 1)
        );
        AddCube(bottomLeft, bottomRight, FarTrackLeft, FarTrackRight, true);
        Vector2 topLeft = bottomLeft;
        Vector2 topRight = bottomRight;

        // Right-side calculations
        bottomLeft = new Vector2(
            centreWaypoints.Last().x - perp_x * trackWidths.Last().y,
            centreWaypoints.Last().y - perp_y * trackWidths.Last().y
        );

       FarTrackLeft = new Vector2(
            centreWaypoints.Last().x - perp_x * (trackWidths.Last().y + 1),
            centreWaypoints.Last().y - perp_y * (trackWidths.Last().y + 1)
        );
        bottomRight = new Vector2(
            centreWaypoints[0].x - perp_x * trackWidths[0].y,
            centreWaypoints[0].y - perp_y * trackWidths[0].y
        );

        FarTrackRight = new Vector2(
            centreWaypoints[0].x - perp_x * (trackWidths[0].y + 1),
            centreWaypoints[0].y - perp_y * (trackWidths[0].y + 1)
        );
        AddCube(FarTrackLeft, FarTrackRight, bottomLeft, bottomRight, false);
        AddTrackMesh(topLeft, bottomLeft, topRight, bottomRight);
    }

    void GenerateTrackMesh(){
        _mesh = new Mesh();
        _mesh.name = "Generated Track Mesh";
        if (!_meshFilter) _meshFilter = gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _meshFilter.mesh = _mesh;
        _meshRenderer.material = roadMaterial;
    }
    void AddCube(Vector2 bottomLeft, Vector2 bottomRight, Vector2 topLeft, Vector2 topRight, bool textureToggle)
    {
        GameObject cubeObj = new GameObject("TrackBorderCube");
        cubeObj.transform.parent = this.transform;

        MeshFilter meshFilter = cubeObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = cubeObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = cubeObj.AddComponent<MeshCollider>();

        if (redWhiteMaterial.Length == 2)
        {
            meshRenderer.material = redWhiteMaterial[textureIndex];
            if (textureToggle) textureIndex = (textureIndex == 1) ? 0 : 1;
        }

        Mesh cubeMesh = new Mesh();

        List<Vector3> cubeVertices = new List<Vector3>
        {
            new Vector3(bottomLeft.x, 0, bottomLeft.y), // 0
            new Vector3(bottomRight.x, 0, bottomRight.y), // 1
            new Vector3(topLeft.x, 0, topLeft.y), // 2
            new Vector3(topRight.x, 0, topRight.y), // 3

            new Vector3(bottomLeft.x, 0.03f, bottomLeft.y), // 4
            new Vector3(bottomRight.x, 0.03f, bottomRight.y), // 5
            new Vector3(topLeft.x, 0.03f, topLeft.y), // 6
            new Vector3(topRight.x, 0.03f, topRight.y) // 7
        };

        List<int> cubeTriangles = new List<int>
        {
            // Top face
            4, 6, 5, 6, 7, 5,

            // Front wall
            2, 3, 6, 6, 3, 7,

            // Back wall
            0, 4, 1, 1, 4, 5,
        };

        cubeMesh.vertices = cubeVertices.ToArray();
        cubeMesh.triangles = cubeTriangles.ToArray();
        cubeMesh.RecalculateNormals();
        cubeMesh.RecalculateBounds();

        meshFilter.mesh = cubeMesh;
        meshCollider.sharedMesh = cubeMesh; // Assigning the same mesh for collisions
    }

    void AddTrackMesh(Vector2 bottomLeft, Vector2 bottomRight, Vector2 topLeft, Vector2 topRight){
        int vertIndex = vertices.Count;

        vertices.Add(new Vector3(bottomLeft.x, 0.01f, bottomLeft.y));
        vertices.Add(new Vector3(bottomRight.x, 0.01f, bottomRight.y));
        vertices.Add(new Vector3(topLeft.x, 0.01f, topLeft.y));
        vertices.Add(new Vector3(topRight.x, 0.01f, topRight.y));

        // Define two triangles to form a quad
        triangles.Add(vertIndex);
        triangles.Add(vertIndex + 2);
        triangles.Add(vertIndex + 1);

        triangles.Add(vertIndex + 1);
        triangles.Add(vertIndex + 2);
        triangles.Add(vertIndex + 3);
   
    }
}