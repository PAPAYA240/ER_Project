using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Object.Monster
{
    public class MonsterManager
    {
        GameRoom _room;
        int _monsterCount = 0;
        int _keepMonsterCount = 0;
        long _nextSpawnTick = 0;

        Stack<MonsterType> reserveMonster = new Stack<MonsterType>();
        public void Init(GameRoom room, int keepMonsterCount = 0)
        {
            _room = room;
            _keepMonsterCount = keepMonsterCount;
        }

        public void Add(int monsterCnt, MonsterType type = MonsterType.MonsterNone)
        {
            _keepMonsterCount += monsterCnt;
            if (type != MonsterType.MonsterNone)
            { 
                for (int i = 0; i < monsterCnt; i++)
                    reserveMonster.Push(type);
            }
        }

        public void Update()
        {
            if (_room == null)
                return;

            if (_nextSpawnTick > Environment.TickCount64) return; 
            _nextSpawnTick = Environment.TickCount64 + 1000;

            if (_monsterCount < _keepMonsterCount) 
                Spawn();
        }

        private void Spawn()
        {
            if (reserveMonster.Count() == 0)
            {
                _keepMonsterCount = 0;
                Console.WriteLine("Monster Spawn 실패");
                return;
            }

            Monster monster = ObjectManager.Instance.Add<Monster>();
            monster.Info.Name = $"{monster.Id} Monster";
            monster.Info.PosInfo.State = CreatureState.Idle;
            monster.Info.PosInfo.PosX = 0;
            monster.Info.PosInfo.PosY = 0;
            monster.Info.MonsterType = reserveMonster.Peek();
            reserveMonster.Pop();

            StatInfo stat = null;
            DataManager.StatDict.TryGetValue(2, out stat);
            monster.Stat.MergeFrom(stat);

            monster.Init(monster.Info.MonsterType.ToString());
            _room.Push(_room.EnterGame, monster);
            _monsterCount++;
        }
    }
}
