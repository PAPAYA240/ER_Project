using Google.Protobuf.Protocol;
using Server.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Server.Game.Object.Monster
{
    public class LoadMonsterData
    {
        public MonsterType monsterType;
        public float xPos;
        public float yPos;
        public float zPos;
    }

    public class RawMonsterList
    {
        public List<LoadMonsterData> monsters;
    }

    public class MonsterDataProcessor
    {
        public RawMonsterList ProcessAndGetJson()
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

                cleanedList.monsters.Add(new LoadMonsterData
                {
                    monsterType = monsterTypeName,
                    xPos = (float)rawData.xPos,
                    yPos = (float)rawData.yPos,
                    zPos = (float)rawData.zPos
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
            MonsterDataProcessor processor = new MonsterDataProcessor();
            SpawnMonstersFromJson(processor.ProcessAndGetJson());
        }

        public void Add(int monsterCnt, MonsterType type = MonsterType.MonsterNone)
        {
            if (type == MonsterType.MonsterNone)
                return;

            for (int i = 0; i < monsterCnt; i++)
            {
                // 몬스터를 즉시 생성하는 Spawn 로직을 이곳에 복사합니다.
                Monster monster = ObjectManager.Instance.Add<Monster>();
                monster.Info.Name = $"{monster.Id} Monster";
                monster.Info.PosInfo.State = CreatureState.Idle;
                monster.Info.PosInfo.PosX = 0;
                monster.Info.PosInfo.PosY = 0;
                monster.Info.MonsterType = type;

                MonsterData monsterStat = null;
                DataManager.MonsterDict.TryGetValue(type.ToString(), out monsterStat);
                monster.Stat.MergeFrom(monsterStat.stat);

                monster.Init(monster.Info.MonsterType.ToString());
                _room.Push(_room.EnterGame, monster);
            }
        }
        public void SpawnMonstersFromJson(RawMonsterList monsterList)
        {
            if (_room == null || monsterList == null)
                return;

            foreach (var monsterData in monsterList.monsters)
            {
                Monster monster = ObjectManager.Instance.Add<Monster>();

                monster.Info.PosInfo.PosX = monsterData.xPos;
                monster.Info.PosInfo.PosY = monsterData.yPos;
                monster.Info.PosInfo.PosZ = monsterData.zPos;

                MonsterType type =monsterData.monsterType;
                monster.Info.MonsterType = type;
                monster.Info.Name = $"{monster.Id} Monster";
                monster.Info.PosInfo.State = CreatureState.Idle;

                MonsterData monsterStat = null;
                DataManager.MonsterDict.TryGetValue(type.ToString(), out monsterStat);
                monster.Stat.MergeFrom(monsterStat.stat);

                monster.Init(monster.Info.MonsterType.ToString());
                _room.Push(_room.EnterGame, monster);
            }
        }
    }
}
