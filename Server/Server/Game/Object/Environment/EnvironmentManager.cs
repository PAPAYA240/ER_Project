using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using Server.Data;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Server.Game
{
    #region Data
    public class LoadEnvData
    {
        public EnvType envType;
        public Vector3 posInfo;
        public Quaternion rotInfo;
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
            string basePath = ConfigManager.Config.dataPaths["player"];
            string navFilePath = Path.Combine(basePath, "SpawnEnvData.json");
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
                    posInfo = rawData.posInfo,
                    rotInfo = rawData.rotInfo
                });
            }
            return cleanedList;
        }
    }

    #endregion

    public class EnvironmentManager
    {
        GameRoom _room;

        public void Init(GameRoom room)
        {
            _room = room;
            EnvDataProcessor processor = new EnvDataProcessor();
            SpawnObjectFromJson(processor.ProcessAndGetJson());
        }

        public void GiveRewardToPlayer(int playerId, EnvType envType)
        {
            // 플레이어에게 보상 지급
            switch (envType)
            {
                case EnvType.HealPack:
                    break;

                default:
                    break;
            }
        }

        public void SpawnObjectFromJson(RawEnvList envList)
        {
            if (_room == null || envList == null)
                return;

            foreach (var eData in envList.EnvObjects)
            {
                EnvironmentObject env = ObjectManager.Instance.Add<EnvironmentObject>();

                env.Info.PosInfo.PosX = eData.posInfo.X;
                env.Info.PosInfo.PosY = eData.posInfo.Y;
                env.Info.PosInfo.PosZ = eData.posInfo.Z;

                env.Info.RotInfo.Qx = eData.rotInfo.X;
                env.Info.RotInfo.Qy = eData.rotInfo.Y;
                env.Info.RotInfo.Qz = eData.rotInfo.Z;
                env.Info.RotInfo.Qw = eData.rotInfo.W;

                env.Info.Env = new EnvInfo();
                env.Info.Env.EnvType = eData.envType;
                env.Info.Name = $"{env.Id} Environment";
                _room.Push(_room.EnterGame, env, 0);
            }
        }
    }
}
