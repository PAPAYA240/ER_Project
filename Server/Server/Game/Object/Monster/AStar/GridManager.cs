using Server.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Game.Object.Monster.AStar
{
    [System.Serializable]
    public class MapData
    {
        public int width;
        public int depth;
        public bool[] walkable;
        public float[] height;
    }
    public class GridManager
    {
        public static GridManager Instance { get; } = new GridManager();

        // 데이터
        private bool[,] _walkable;
        private float[,] _height;
        private int _width;
        private int _depth;

        public void LoadData(string mapName)
        {
            string filePath = $"{ConfigManager.Config.dataPath}/{mapName}.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: Map data file not found at {filePath}");
                return;
            }

            string text = File.ReadAllText(filePath);

            MapData mapData = Newtonsoft.Json.JsonConvert.DeserializeObject<MapData>(text);
            _width = mapData.width;
            _depth = mapData.depth;
            _walkable = new bool[_width, _depth];
            _height = new float[_width, _depth];

            for (int z = 0; z < _depth; z++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _walkable[x, z] = mapData.walkable[z * _width + x];
                    _height[x, z] = mapData.height[z * _width + x];
                }
            }

            Console.WriteLine($"Map data [{mapName}] loaded!");
        }

        public bool IsWalkable(int x, int z)
        {
            if (x < 0 || x >= _width || z < 0 || z >= _depth)
                return false;
            return _walkable[x, z];
        }

        public float GetHeight(int x, int z)
        {
            if (x < 0 || x >= _width || z < 0 || z >= _depth)
                return 0;
            return _height[x, z];
        }
    }
}
