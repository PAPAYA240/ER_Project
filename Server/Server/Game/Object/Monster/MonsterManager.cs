using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object.Monster
{
    public class MonsterManager
    {
        GameRoom _room;
        int _monsterCount = 0;
        int _keepMonsterCount = 10;
        long _nextSpawnTick = 0;
        public void Init(GameRoom room, int keepMonsterCount = 0)
        {
            _room = room;
            _keepMonsterCount = keepMonsterCount;
        }

        public void Add(int monsterCnt)
        {
            _monsterCount += monsterCnt;
        }

        public void Update()
        {
            if (_nextSpawnTick > Environment.TickCount64) return; 

            _nextSpawnTick = Environment.TickCount64 + 1000;

            if (_monsterCount < _keepMonsterCount)
                Spawn();
        }

        private void Spawn()
        {
            Monster monster = ObjectManager.Instance.Add<Monster>();

            //monster.CellPos = new Vector3(0, 0, 0);
            _room.Push(_room.EnterGame, monster);
            _monsterCount++;
        }
    }
}
