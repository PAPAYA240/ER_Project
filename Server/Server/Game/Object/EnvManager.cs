using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using Server.Data;
using Server.Game.Object.Monster;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Game.Object
{
    public class LoadEnvData
    {
        public EnvType envType;
        public float xPos;
        public float yPos;
        public float zPos;
    }

    public class RawEnvList
    {
        [JsonProperty("EnvObjects")]
        public List<LoadEnvData> EnvObjects;
    }

    public class EnvDataProcessor
    {
        public RawEnvList ProcessAndGetJson()
        {
            string basePath = ConfigManager.Config.dataPaths["monster"];
            string navFilePath = Path.Combine(basePath, "Env/SpawnEnvData.json");
            string rawJson = File.ReadAllText(navFilePath);

            // 2. 원본 JSON을 C# 객체로 역직렬화
            RawEnvList rawList = JsonConvert.DeserializeObject<RawEnvList>(rawJson);

            // 3. 새로운 리스트를 만들어 데이터를 가공
            RawEnvList cleanedList = new RawEnvList();
            cleanedList.EnvObjects = new List<LoadEnvData>();

            foreach (var rawData in rawList.EnvObjects)
            {
                // 정수 monsterType을 문자열로 변환
                EnvType monsterTypeName = rawData.envType;

                cleanedList.EnvObjects.Add(new LoadEnvData
                {
                    envType = monsterTypeName,
                    xPos = (float)rawData.xPos,
                    yPos = (float)rawData.yPos,
                    zPos = (float)rawData.zPos
                });
            }
            return cleanedList;
        }
    }

    public class EnvManager
    {
        GameRoom _room;

        public void Init(GameRoom room)
        {
            _room = room;

            EnvDataProcessor processor = new EnvDataProcessor();
            SpawnObjectFromJson(processor.ProcessAndGetJson());
        }

        public void Add(int cnt, EnvType type = EnvType.EnvNone)
        {
            if (type == EnvType.EnvNone)
                return;

            for (int i = 0; i < cnt; i++)
            {
                // 몬스터를 즉시 생성하는 Spawn 로직을 이곳에 복사합니다.
                EnvironmentObj env = ObjectManager.Instance.Add<EnvironmentObj>();
                env.Info.Name = $"{env.Id} Environment";
                env.Info.PosInfo.PosX = 0;
                env.Info.PosInfo.PosY = 0;
                env._envType = type;

                DataManager.EnvironmentObjDict.TryGetValue(type, out ObjectInfo envStat);
                _room.Push(_room.EnterGame, env);
            }
        }
        public void SpawnObjectFromJson(RawEnvList envList)
        {
            if (_room == null || envList == null)
                return;

            foreach (var eData in envList.EnvObjects)
            {
                EnvironmentObj env = ObjectManager.Instance.Add<EnvironmentObj>();

                env.Info.PosInfo.PosX = eData.xPos;
                env.Info.PosInfo.PosY = eData.yPos;
                env.Info.PosInfo.PosZ = eData.zPos;

                EnvType type = eData.envType;
                env.Info.Name = $"{env.Id} Environment";
                env.Info.EnvType = type;
                env.Init();
                _room.Push(_room.EnterGame, env);
            }
        }
    }
}
