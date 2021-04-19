using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [SerializeField] float wallHeight = 5f;

    public SquareGrid squareGrid;
    public MeshFilter walls;

    List<Vector3> vertices;
    List<int> triangles;

    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(int[,] _map, float _squareSize) {
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(_map, _squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++) {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++) {
                TriangulateSquare(squareGrid.squares[x,y]);
            }
        }

        Mesh _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.RecalculateNormals();

        CreateWallMesh();
    }

    private void CreateWallMesh() {
        CalculateMeshOutlines();

        List<Vector3> _wallVertices = new List<Vector3>();
        List<int> _wallTriangles = new List<int>();
        Mesh _wallMesh = new Mesh();

        foreach(List<int> _outline in outlines) {
            for(int i = 0 ; i < _outline.Count - 1 ; i++) {
                int _startIndex = _wallVertices.Count;
                _wallVertices.Add(vertices[_outline[i]]); // left
                _wallVertices.Add(vertices[_outline[i+1]]); // right
                _wallVertices.Add(vertices[_outline[i]] - Vector3.up * wallHeight); // bottom left
                _wallVertices.Add(vertices[_outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                _wallTriangles.Add(_startIndex + 0);
                _wallTriangles.Add(_startIndex + 2);
                _wallTriangles.Add(_startIndex + 3);

                _wallTriangles.Add(_startIndex + 3);
                _wallTriangles.Add(_startIndex + 1);
                _wallTriangles.Add(_startIndex + 0);
            }
        }

        _wallMesh.vertices = _wallVertices.ToArray();
        _wallMesh.triangles = _wallTriangles.ToArray();
        walls.mesh = _wallMesh;
    }

    private void TriangulateSquare(Square _square) {
        switch(_square.configuration) {
            case 0: // 0 points
                break;
            case 1: // 1 point
                MeshFromPoints(_square.centerLeft, _square.centerBottom, _square.bottomLeft);
                break;
            case 2: // 1 point
                MeshFromPoints(_square.bottomRight, _square.centerBottom, _square.centerRight);
                break;
            case 3: // 2 points
                MeshFromPoints(_square.centerRight, _square.bottomRight, _square.bottomLeft, _square.centerLeft);
                break;
            case 4: // 1 point
                MeshFromPoints(_square.topRight, _square.centerRight, _square.centerTop);
                break;
            case 5: // 2 points
                MeshFromPoints(_square.centerTop, _square.topRight, _square.centerRight, _square.centerBottom, _square.bottomLeft, _square.centerLeft);
                break;
            case 6: // 2 points
                MeshFromPoints(_square.centerTop, _square.topRight, _square.bottomRight, _square.centerBottom);
                break;
            case 7: // 3 points
                MeshFromPoints(_square.centerTop, _square.topRight, _square.bottomRight, _square.bottomLeft, _square.centerLeft);
                break;
            case 8: // 1 point
                MeshFromPoints(_square.topLeft, _square.centerTop, _square.centerLeft);
                break;
            case 9: // 2 points
                MeshFromPoints(_square.topLeft, _square.centerTop, _square.centerBottom, _square.bottomLeft);
                break;
            case 10: // 2 points
                MeshFromPoints(_square.topLeft, _square.centerTop, _square.centerRight, _square.bottomRight, _square.centerBottom, _square.centerLeft);
                break;
            case 11: // 3 points
                MeshFromPoints(_square.topLeft, _square.centerTop, _square.centerRight, _square.bottomRight, _square.bottomLeft);
                break;
            case 12: // 2 points
                MeshFromPoints(_square.topLeft, _square.topRight, _square.centerRight, _square.centerLeft);
                break;
            case 13: // 3 points
                MeshFromPoints(_square.topLeft, _square.topRight, _square.centerRight, _square.centerBottom, _square.bottomLeft);
                break;
            case 14: // 3 points
                MeshFromPoints(_square.topLeft, _square.topRight, _square.bottomRight, _square.centerBottom, _square.centerLeft);
                break;
            case 15: // 4 points
                MeshFromPoints(_square.topLeft, _square.topRight, _square.bottomRight, _square.bottomLeft);
                checkedVertices.Add(_square.topLeft.vertexIndex);
                checkedVertices.Add(_square.topRight.vertexIndex);
                checkedVertices.Add(_square.bottomRight.vertexIndex);
                checkedVertices.Add(_square.bottomLeft.vertexIndex);
                break;
            default:
                Debug.LogError($"Unexpected configuration ({_square.configuration}) for square with topLeft at {_square.topLeft.position}");
                break;
        }
    }

    private void MeshFromPoints(params Node[] _points) {
        AssignVertices(_points);
        if(_points.Length >= 3) CreateTriangle(_points[0], _points[1], _points[2]);
        if(_points.Length >= 4) CreateTriangle(_points[0], _points[2], _points[3]);
        if(_points.Length >= 5) CreateTriangle(_points[0], _points[3], _points[4]);
        if(_points.Length >= 6) CreateTriangle(_points[0], _points[4], _points[5]);
    }

    private void AssignVertices(Node[] _points) {
        for(int i = 0 ; i < _points.Length ; i++) {
            if(_points[i].vertexIndex == -1) {
                _points[i].vertexIndex = vertices.Count;
                vertices.Add(_points[i].position);
            }
        }
    }

    private void CreateTriangle(Node _a, Node _b, Node _c) {
        triangles.Add(_a.vertexIndex);
        triangles.Add(_b.vertexIndex);
        triangles.Add(_c.vertexIndex);

        Triangle _triangle = new Triangle(_a.vertexIndex, _b.vertexIndex, _c.vertexIndex);
        AddTriangleToDictionary(_triangle.vertexIndexA, _triangle);
        AddTriangleToDictionary(_triangle.vertexIndexB, _triangle);
        AddTriangleToDictionary(_triangle.vertexIndexC, _triangle);
    }

    private void AddTriangleToDictionary(int _vertexIndexKey, Triangle _triangle) {
        if(triangleDictionary.ContainsKey(_vertexIndexKey)) {
            triangleDictionary[_vertexIndexKey].Add(_triangle);
        } else {
            List<Triangle> _triangles = new List<Triangle>();
            _triangles.Add(_triangle);
            triangleDictionary.Add(_vertexIndexKey, _triangles);
        }
    }

    private void CalculateMeshOutlines() {
        for(int _vertexIndex = 0 ; _vertexIndex < vertices.Count ; _vertexIndex++) {
            if(!checkedVertices.Contains(_vertexIndex)) {
                int _newOutlineVertex = GetConnectedOutlineVertex(_vertexIndex);
                if(_newOutlineVertex != -1) {
                    checkedVertices.Add(_vertexIndex);
                    List<int> _newOutline = new List<int>();
                    _newOutline.Add(_vertexIndex);
                    outlines.Add(_newOutline);
                    FollowOutline(_newOutlineVertex, outlines.Count-1);
                    outlines[outlines.Count-1].Add(_vertexIndex);
                }
            }
        }
    }

    private void FollowOutline(int _vertexIndex, int _outlineIndex) {
        outlines[_outlineIndex].Add(_vertexIndex);
        checkedVertices.Add(_vertexIndex);
        int _nextVertexIndex = GetConnectedOutlineVertex(_vertexIndex);
        if(_nextVertexIndex != -1) {
            FollowOutline(_nextVertexIndex, _outlineIndex);
        }
    }

    private int GetConnectedOutlineVertex(int _vertexIndex) {
        List<Triangle> _triangles = triangleDictionary[_vertexIndex];
        for(int i = 0 ; i < _triangles.Count ; i++) {
            Triangle _triangle = _triangles[i];
            for(int j = 0 ; j < 3 ; j++) {
                int _vertexB = _triangle[j];
                if(_vertexIndex != _vertexB && !checkedVertices.Contains(_vertexB)) {
                    if(IsOutlineEdge(_vertexIndex, _vertexB)) return _vertexB;
                }
            }
        }

        return -1;
    }

    private bool IsOutlineEdge(int _a, int _b) {
        List<Triangle> _aTriangles = triangleDictionary[_a];
        int _sharedTriangles = 0;
        for(int i = 0 ; i < _aTriangles.Count ; i++) {
            if(_aTriangles[i].Contains(_b)) {
                _sharedTriangles++;
                // if the edge is shared by more than one triangle
                // we don't care about the rest, we already know it's not an outline edge
                if(_sharedTriangles > 1) break;
            }
        }
        return _sharedTriangles == 1;
    }

    struct Triangle {
        public int vertexIndexA, vertexIndexB, vertexIndexC;
        int[] vertices;

        public Triangle(int _a, int _b, int _c) {
            vertexIndexA = _a;
            vertexIndexB = _b;
            vertexIndexC = _c;
            vertices = new int[3];
            vertices[0] = vertexIndexA;
            vertices[1] = vertexIndexB;
            vertices[2] = vertexIndexC;
        }

        public int this[int i] {
            get {
                return vertices[i];
            }
        }

        public bool Contains(int _vertexIndex) {
            return _vertexIndex == vertexIndexA || _vertexIndex == vertexIndexB || _vertexIndex == vertexIndexC;
        }
    }

    public class Node {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _position) {
            position = _position;
        }
    }

    public class ControlNode : Node {
        public bool isActive;
        public Node above, right;

        public ControlNode(Vector3 _position, bool _isActive, float _squareSize) : base(_position) {
            isActive = _isActive;
            above = new Node(this.position + Vector3.forward * _squareSize/2f);
            right = new Node(this.position + Vector3.right * _squareSize/2f);
        }
    }

    public class Square {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;
        public int configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centerTop = topLeft.right;
            centerRight = bottomRight.above;
            centerBottom = bottomLeft.right;
            centerLeft = bottomLeft.above;

            if(topLeft.isActive) configuration += 8;
            if(topRight.isActive) configuration += 4;
            if(bottomRight.isActive) configuration += 2;
            if(bottomLeft.isActive) configuration += 1;
        }
    }

    public class SquareGrid {
        public Square[,] squares;

        public SquareGrid(int[,] _map, float _squareSize) {
            int _nodeCountX = _map.GetLength(0);
            int _nodeCountY = _map.GetLength(1);
            float _mapWidth = _nodeCountX * _squareSize;
            float _mapHeight = _nodeCountY * _squareSize;

            ControlNode[,] controlNodes = new ControlNode[_nodeCountX, _nodeCountY];

            for(int x = 0 ; x < _nodeCountX ; x++) {
                for(int y = 0 ; y < _nodeCountY ; y++) {
                    Vector3 _position = new Vector3(-_mapWidth/2 + x * _squareSize + _squareSize/2,
                                                    0,
                                                    -_mapHeight/2 + y * _squareSize + _squareSize/2);
                    controlNodes[x,y] = new ControlNode(_position, _map[x,y] == 1, _squareSize);
                }
            }

            squares = new Square[_nodeCountX-1, _nodeCountY-1];
            for (int x = 0 ; x < _nodeCountX - 1 ; x++) {
                for (int y = 0 ; y < _nodeCountY - 1 ; y++) {
                    squares[x,y] = new Square(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1,y], controlNodes[x,y]);
                }
            }           
        }
    }
}
