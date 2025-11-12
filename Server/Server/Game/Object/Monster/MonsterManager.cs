using Google.Protobuf.Protocol;
using Server.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Server.Game
{
    public class LoadMonsterData
    {
        public MonsterType monsterType;
        public Vector3 posInfo;
        public Quaternion rotInfo;
    }

    public class RawMonsterList
    {
        public List<LoadMonsterData> monsters;
    }

    public class MonsterDataProcessor
    {
        public RawMonsterList ProcessAndGetJson(int phase)
        {
            string basePath = ConfigManager.Config.dataPaths["monster"];
            string navFilePath = Path.Combine(basePath, "MonsterData/SpawnMonsterData.json");
            string rawJson = File.ReadAllText(navFilePath);

            // 2. 원본 JSON을 C# 객체로 역직렬화
            RawMonsterList rawList = JsonConvert.DeserializeObject<RawMonsterList>(rawJson);

            // 3. 새로운 리스트를 만들어 데이터를 가공
            RawMonsterList cleanedList = new RawMonsterList();
            cleanedList.monsters = new List<LoadMonsterData>();

            foreach (var rawData in rawList.monsters)
            {
                // 정수 monsterType을 문자열로 변환
                MonsterType monsterTypeName = rawData.monsterType;

                // 페이즈대로 몬스터 호출
                if (!DataManager.MonsterDict.TryGetValue(monsterTypeName, out MonsterData monsterStat))
                    continue;
                if (monsterStat.activePhase != phase)
                    continue;

                cleanedList.monsters.Add(new LoadMonsterData
                {
                    monsterType = monsterTypeName,
                    posInfo = rawData.posInfo,
                    rotInfo = rawData.rotInfo,
                });
            }
            return cleanedList;
        }
    }

    public class MonsterManager
    {
        GameRoom _room;

        public void Init(GameRoom room)
        {
            _room = room;

            // MonsterData Load
            //Add(0);
        }

        public void Add(int phase)
        {
            MonsterDataProcessor processor = new MonsterDataProcessor();
            SpawnMonstersFromJson(processor.ProcessAndGetJson(phase));
        }

        public void SpawnMonstersFromJson(RawMonsterList monsterList)
        {
            if (_room == null || monsterList == null)
                return;

            foreach (var monsterData in monsterList.monsters)
            {
                Monster monster = ObjectManager.Instance.Add<Monster>();

                monster.Info.Monster = new MonsterInfo();
                monster.Info.PosInfo = new PositionInfo();
                monster.Info.RotInfo = new RotationInfo();

                monster.Info.PosInfo.PosX = monsterData.posInfo.X;
                monster.Info.PosInfo.PosY = monsterData.posInfo.Y;
                monster.Info.PosInfo.PosZ = monsterData.posInfo.Z;

                monster.Info.RotInfo.Qx = monsterData.rotInfo.X;
                monster.Info.RotInfo.Qy = monsterData.rotInfo.Y;
                monster.Info.RotInfo.Qz = monsterData.rotInfo.Z;
                monster.Info.RotInfo.Qw = monsterData.rotInfo.W;

                MonsterType type = monsterData.monsterType;
                monster.Info.Monster.MonsterType = type;

                monster.Info.Name = $"{monster.Id} {type}";
                monster.Info.PosInfo.State = CreatureState.Appear;

                MonsterData monsterStat = null;
                DataManager.MonsterDict.TryGetValue(type, out monsterStat);
                if (monsterStat != null)
                    monster.Stat.MergeFrom(monsterStat.stat);

                monster.Init(monster.Info.Monster.MonsterType);
                _room.Push(_room.EnterGame, monster);
            }
        }
    }
}
