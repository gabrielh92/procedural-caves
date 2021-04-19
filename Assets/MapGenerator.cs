using System;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [Range(0,100)][SerializeField]
    int randomFillPercent = 50;

    [SerializeField] int width = 100;
    [SerializeField] int height = 100;

    [SerializeField] int borderSize = 5;
    [SerializeField] int minWallRegionSize = 50;
    [SerializeField] int minRoomRegionSize = 50;
    [SerializeField] int passageWidth = 1;

    [Header("Seed Settings")]
    [SerializeField] string seed = "hello world";
    [SerializeField] bool useRandomSeed = false;

    [Header("Smoothness Settings")]
    [SerializeField] int smoothLoops = 5;
    [SerializeField] int surroundThreshold = 4;

    int[,] map;

    private void Start() {
        GenerateMap();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Space)) {
            GenerateMap();
        }
    }

    public void GenerateMap() {
        map = new int[width, height];
        RandomFillMap();

        for(int i = 0 ; i < smoothLoops ; i++) {
            SmoothMap();
        }

        RemoveSmallMapRegions();

        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];
        for (int x = 0 ; x < borderedMap.GetLength(0) ; x++) {
            for (int y = 0 ; y < borderedMap.GetLength(1) ; y++) {
                if(x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
                    borderedMap[x,y] = map[x - borderSize, y - borderSize];
                } else {
                    borderedMap[x,y] = 1;
                }
            }
        }

        MeshGenerator meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.GenerateMesh(borderedMap, 1);
    }

    private void RandomFillMap() {
        if(useRandomSeed) {
            seed = Time.time.ToString();
        }
        
        System.Random pseudoRNG = new System.Random(seed.GetHashCode());

        for(int x = 0 ; x < width ; x++) {
            for(int y = 0 ; y < height ; y++) {
                if(x == 0 || x == width - 1 || y == 0 || y == height - 1) {
                    map[x,y] = 1;
                } else {
                    map[x, y] = (pseudoRNG.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    private void SmoothMap() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int _surroundingWallCount = GetSurroundingWallCount(x, y);
                if(_surroundingWallCount > surroundThreshold) {
                    map[x,y] = 1;
                } else if(_surroundingWallCount < surroundThreshold) {
                    map[x,y] = 0;
                }
            }
        }
    }

    private int GetSurroundingWallCount(int _x, int _y) {
        int wallCount = 0;
        for(int neighbourX = _x - 1 ; neighbourX <= _x + 1 ; neighbourX++) {
            for(int neighbourY = _y - 1 ; neighbourY <= _y + 1 ; neighbourY++) {
                if (IsInMapRange(neighbourX, neighbourY)) {
                    if(neighbourX != _x || neighbourY != _y) {
                        wallCount += map[neighbourX, neighbourY];
                    }
                } else {
                    wallCount++;
                }
            }
        }
        return wallCount++;
    }

    private bool IsInMapRange(int _x, int _y) {
        return _x >= 0 && _x < width && _y >= 0 && _y < height;
    }

    private void RemoveSmallMapRegions() {
        List<List<Coord>> _wallRegions = GetRegions(1);
        foreach(List<Coord> _wallRegion in _wallRegions) {
            if(_wallRegion.Count < minWallRegionSize) {
                foreach(Coord _coord in _wallRegion) {
                    map[_coord.tileX, _coord.tileY] = 0;
                }
            }
        }

        List<List<Coord>> _roomRegions = GetRegions(0);
        List<Room> _survivingRooms = new List<Room>();
        foreach (List<Coord> _roomRegion in _roomRegions) {
            if (_roomRegion.Count < minRoomRegionSize) {
                foreach (Coord _coord in _roomRegion) {
                    map[_coord.tileX, _coord.tileY] = 1;
                }
            } else {
                _survivingRooms.Add(new Room(_roomRegion, map));
            }
        }

        _survivingRooms.Sort();
        _survivingRooms[0].isMainRoom = true;
        _survivingRooms[0].isAccessibleFromMainRoom = true;

        ConnectClosestRooms(_survivingRooms);
    }

    private void ConnectClosestRooms(List<Room> _rooms, bool _forceAccessibilityFromMainRoom = false) {
        List<Room> _roomListA = new List<Room>(), _roomListB = new List<Room>();
        
        if(_forceAccessibilityFromMainRoom) {
            foreach(Room _room in _rooms) {
                if(_room.isAccessibleFromMainRoom) {
                    _roomListB.Add(_room);
                } else {
                    _roomListA.Add(_room);
                }
            }
        } else {
            _roomListA = _rooms;
            _roomListB = _rooms;
        }

        int _minDistance = 0;
        Coord _minCoordA = new Coord(), _minCoordB = new Coord();
        Room _minRoomA = new Room(), _minRoomB = new Room();
        bool _connectionFound = false;

        foreach(Room _a in _roomListA) {
            if(!_forceAccessibilityFromMainRoom) {
                _connectionFound = false;
                if(_a.connectedRooms.Count > 0) {
                    continue;
                }
            }

            foreach(Room _b in _roomListB) {
                if(_a == _b || _a.IsConnected(_b)) continue;

                for(int _coordIndexA = 0 ; _coordIndexA < _a.edgeCoords.Count ; _coordIndexA++) {
                    for (int _coordIndexB = 0; _coordIndexB < _b.edgeCoords.Count; _coordIndexB++) {
                        Coord _coordA = _a.edgeCoords[_coordIndexA];
                        Coord _coordB = _b.edgeCoords[_coordIndexB];

                        int _distance = (int)(Mathf.Pow(_coordA.tileX - _coordB.tileX, 2) + Mathf.Pow(_coordA.tileY - _coordB.tileY, 2));
                        if(_distance < _minDistance || !_connectionFound) {
                            _connectionFound = true;
                            _minDistance = _distance;
                            
                            _minCoordA = _coordA;
                            _minCoordB = _coordB;

                            _minRoomA = _a;
                            _minRoomB = _b;
                        }
                    }
                }
            }

            if (_connectionFound && !_forceAccessibilityFromMainRoom) {
                CreatePassageBetweenRooms(_minRoomA, _minRoomB, _minCoordA, _minCoordB);
            }
        }

        if (_connectionFound && _forceAccessibilityFromMainRoom) {
            CreatePassageBetweenRooms(_minRoomA, _minRoomB, _minCoordA, _minCoordB);
            ConnectClosestRooms(_rooms, true);
        }

        if(!_forceAccessibilityFromMainRoom) {
            ConnectClosestRooms(_rooms, true);
        }
    }

    private void CreatePassageBetweenRooms(Room _a, Room _b, Coord _coordA, Coord _coordB) {
        Room.ConnectRooms(_a, _b);

        List<Coord> _line = GetLine(_coordA, _coordB);
        foreach(Coord _coord in _line) {
            DrawCircle(_coord, passageWidth);
        }
    }

    private void DrawCircle(Coord _c, int _radius) {
        for(int x = -_radius ; x <= _radius ; x++) {
            for(int y = -_radius ; y <= _radius ; y++) {
                if(x*x + y*y <= _radius*_radius) {
                    int _realX = _c.tileX + x;
                    int _realY = _c.tileY + y;

                    if(IsInMapRange(_realX, _realY)) {
                        map[_realX, _realY] = 0;
                    }
                }
            }
        }
    }

    private List<Coord> GetLine(Coord _from, Coord _to) {
        List<Coord> _line = new List<Coord>();
        
        int _x = _from.tileX;
        int _y = _from.tileY;

        int _dx = _to.tileX - _from.tileX;
        int _dy = _to.tileY - _from.tileY;

        int _step = Math.Sign(_dx);
        int _gradientStep = Math.Sign(_dy);

        int _longest = Math.Abs(_dx);
        int _shortest = Math.Abs(_dy);

        bool _inverted = false;
        if(_longest < _shortest) {
            _inverted = true;

            _longest = Math.Abs(_dy);
            _shortest = Math.Abs(_dx);

            _step = Math.Sign(_dy);
            _gradientStep = Math.Sign(_dx);
        }

        int _gradientAccumulation = _longest / 2;
        for(int i = 0 ; i < _longest ; i++) {
            _line.Add(new Coord(_x, _y));
            if(_inverted) {
                _y += _step;
            } else {
                _x += _step;
            }

            _gradientAccumulation += _shortest;
            if(_gradientAccumulation >= _longest) {
                if(_inverted) {
                    _x += _gradientStep;
                } else { 
                    _y += _gradientStep;
                }

                _gradientAccumulation -= _longest;
            }
        }

        return _line;
    }

    private Vector3 CoordToWorldPoint(Coord _c) {
        return new Vector3(-width/2 + 0.5f + _c.tileX, 2, -height/2 + 0.5f + _c.tileY);
    }

    private List<List<Coord>> GetRegions(int _tileType) {
        List<List<Coord>> _regions = new List<List<Coord>>();
        int[,] _visited = new int[width, height];

        for(int x = 0 ; x < width ; x++) {
            for(int y = 0 ; y < height ; y++) {
                if(_visited[x,y] == 0 && map[x,y] == _tileType) {
                    List<Coord> _newRegion = GetRegionTiles(x, y);
                    _regions.Add(_newRegion);

                    foreach(Coord _coord in _newRegion) {
                        _visited[_coord.tileX, _coord.tileY] = 1;
                    }
                }
            }
        }
        return _regions;
    }

    private List<Coord> GetRegionTiles(int _startX, int _startY) {
        List<Coord> _tiles = new List<Coord>();
        int[,] _visited = new int[width, height];
        int _tyleType = map[_startX, _startY];

        Queue<Coord> _queue = new Queue<Coord>();
        _queue.Enqueue(new Coord(_startX, _startY));
        _visited[_startX, _startY] = 1;
        
        while(_queue.Count > 0) {
            Coord _curr = _queue.Dequeue();
            _tiles.Add(_curr);

            for(int x = _curr.tileX - 1 ; x <= _curr.tileX + 1 ; x++) {
                for(int y = _curr.tileY - 1 ; y <= _curr.tileY + 1 ; y++) {
                    if(IsInMapRange(x,y) && (y == _curr.tileY || x == _curr.tileX)) {
                        if(_visited[x,y] == 0 && map[x,y] == _tyleType) {
                            _visited[x,y] = 1;
                            _queue.Enqueue(new Coord(x,y));
                        }
                    }
                }
            }
        }
        return _tiles;
    }

    struct Coord {
        public int tileX, tileY;

        public Coord(int _x, int _y) {
            tileX = _x;
            tileY = _y;
        }
    }

    class Room : IComparable<Room> {
        public List<Coord> coords;
        public List<Coord> edgeCoords;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom, isMainRoom;

        public Room() {

        }

        public Room(List<Coord> _coords, int[,] _map) {
            coords = _coords;
            roomSize = coords.Count;
            connectedRooms = new List<Room>();
            edgeCoords = new List<Coord>();

            foreach(Coord _coord in coords) {
                for(int x = _coord.tileX - 1 ; x <= _coord.tileX + 1 ; x++) {
                    for(int y = _coord.tileY - 1 ; y <= _coord.tileY + 1 ; y++) {
                        if(x == _coord.tileX || y == _coord.tileY) {
                            if(x > 0 && x < _map.GetLength(0) && y > 0 && y < _map.GetLength(1)) {
                                if (_map[x, y] == 1) {
                                    edgeCoords.Add(_coord);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetAccessibleFromMainRoom() {
            if(!isAccessibleFromMainRoom) {
                isAccessibleFromMainRoom = true;
                foreach(Room _room in connectedRooms) {
                    _room.SetAccessibleFromMainRoom();
                }
            }
        }

        public static void ConnectRooms(Room _a, Room _b) {
            if(_a.isAccessibleFromMainRoom) {
                _b.SetAccessibleFromMainRoom();
            } else if(_b.isAccessibleFromMainRoom) {
                _a.SetAccessibleFromMainRoom();
            }
        
            _a.connectedRooms.Add(_b);
            _b.connectedRooms.Add(_a);
        }

        public bool IsConnected(Room _other) {
            return connectedRooms.Contains(_other);
        }

        public int CompareTo(Room _other) {
            return _other.roomSize.CompareTo(roomSize);
        }
    }
}
