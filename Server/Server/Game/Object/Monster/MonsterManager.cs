using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Google.Protobuf.Protocol;
using Server.Data;
using static Google.Protobuf.WellKnownTypes.Field.Types;

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
            if (_room == null)
            {
                Console.WriteLine("Failed _room = MonsterManager.Update()");
                return;
            }

            if (_nextSpawnTick > Environment.TickCount64) return; 

            _nextSpawnTick = Environment.TickCount64 + 1000;

            if (_monsterCount < _keepMonsterCount) 
                Spawn();
        }

        private void Spawn()
        {
            Monster monster = ObjectManager.Instance.Add<Monster>();
            monster.Info.Name = $"Monster_TestMonster";
            monster.Info.PosInfo.State = CreatureState.Idle;
            monster.Info.PosInfo.PosX = 0;
            monster.Info.PosInfo.PosY = 0;

            StatInfo stat = null;
            DataManager.StatDict.TryGetValue(2, out stat);
            monster.Stat.MergeFrom(stat);

            monster.Init("Alpha");
            //monster.Cell = new Vector3(0, 0, 0);
            _room.Push(_room.EnterGame, monster);
            _monsterCount++;
        }
    }
}
