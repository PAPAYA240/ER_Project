using Google.Protobuf.Protocol;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class MonsterSaveData
{
    public MonsterType monsterType;
    public Vector3 posInfo;
    public Quaternion rotInfo;

    public MonsterSaveData(MonsterType type, Vector3 pos, Quaternion rot)
    {
        this.monsterType = type;
        this.posInfo = pos;
        this.rotInfo = rot;
    }
}

[System.Serializable]
public class MonsterList
{
    public List<MonsterSaveData> monsters;
}

public class MonsterSaverEditor : MonoBehaviour
{
    const string _path = "Assets/Resources/Prefabs/Creature/Monster/MonsterSpawnPoints.prefab";

    [MenuItem("Tools/> Save Monster Spawn Data")]
     public static void SaveMonsterSaveData()
     {
         string _savePath = Application.dataPath + "/Resources/Data/MonsterData/SpawnMonsterData.json";

         GameObject SpawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_path);
         SpawnPrefab.transform.position = Vector3.zero;
         SpawnPrefab.transform.rotation = Quaternion.identity;
         if (SpawnPrefab == null)
         {
             Debug.LogError("Error: 프리팹을 찾을 수 없습니다. 경로를 확인하세요: " + _path);
             return;
         }

         MonsterList monsterDataWrapper = new MonsterList();
         monsterDataWrapper.monsters = new List<MonsterSaveData>();

         foreach (Transform child in SpawnPrefab.transform)
         {
             MonsterType type = GetMonsterTypeFromName(child.name);
             if (type != MonsterType.MonsterNone)
             {
                 MonsterSaveData data = new MonsterSaveData(type, child.position, child.rotation);
                 monsterDataWrapper.monsters.Add(data);
             }
         }

         string jsonData = JsonUtility.ToJson(monsterDataWrapper, true);
         string directoryPath = Path.GetDirectoryName(_savePath);
         if (!Directory.Exists(directoryPath))
             Directory.CreateDirectory(directoryPath);

         File.WriteAllText(_savePath, jsonData);
         Debug.Log("몬스터 데이터가 " + _savePath + " 경로에 성공적으로 저장되었습니다.");
     }

    private static MonsterType GetMonsterTypeFromName(string name)
    {
        if (System.Enum.TryParse(name, true, out MonsterType type))
            return type;

        return MonsterType.MonsterNone;
    }
}

[System.Serializable]
public class EnvSaveData
{
    public EnvType envType;
    public Vector3 posInfo;
    public Quaternion rotInfo;

    public EnvSaveData(EnvType type, Vector3 pos, Quaternion rot)
    {
        this.envType = type;
        this.posInfo = pos;
        this.rotInfo = rot;
    }
}

[System.Serializable]
public class EnvList
{
    public List<EnvSaveData> EnvObjects;
}

public class EnvInfoComponent : MonoBehaviour
{
    public EnvType monsterType;
}
public class EnvSaverEditor : MonoBehaviour
{
    const string _envHealpath = "Assets/Resources/GameObject/SpawnPoint/SupportPackSpawnPoints.prefab";
    const string _Turbinepath = "Assets/Resources/GameObject/SpawnPoint/TurbineSpawnPoints.prefab";

    [MenuItem("Tools/Save EnvObject Data")]
    public static void SaveEnvSaveData()
    {
        GameObject HealSpawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_envHealpath);
        if (HealSpawnPrefab == null)
        {
            Debug.LogError("Error: SupportPack 프리팹을 찾을 수 없습니다. 경로를 확인하세요: " + _envHealpath);
            return;
        }

        HealSpawnPrefab.transform.position = Vector3.zero;
        HealSpawnPrefab.transform.rotation = Quaternion.identity;

        EnvList envList = new EnvList();
        envList.EnvObjects = new List<EnvSaveData>();

        foreach (Transform envInfo in HealSpawnPrefab.transform)
        {
            Vector3 pos = envInfo.transform.position;
            Quaternion rot = envInfo.transform.rotation;

            EnvSaveData data = new EnvSaveData(EnvType.HealPack, pos, rot);
            envList.EnvObjects.Add(data);
        }

        GameObject TurbineSpawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_Turbinepath);

        if (TurbineSpawnPrefab == null)
        {
            Debug.LogError("Error: Turbine 프리팹을 찾을 수 없습니다. 경로를 확인하세요: " + _Turbinepath);
        }
        else
        {
            TurbineSpawnPrefab.transform.position = Vector3.zero;
            TurbineSpawnPrefab.transform.rotation = Quaternion.identity;

            foreach (Transform envInfo in TurbineSpawnPrefab.transform)
            {
                Vector3 pos = envInfo.transform.position;
                Quaternion rot = envInfo.transform.rotation;

                EnvSaveData data = new EnvSaveData(EnvType.Turbine, pos, rot);
                envList.EnvObjects.Add(data);
            }
        }

        string jsonData = JsonUtility.ToJson(envList, true);

        string path = Application.dataPath + "/Resources/Data/Env/SpawnEnvData.json";
        File.WriteAllText(path, jsonData);

        AssetDatabase.Refresh();

        Debug.Log($"**환경 데이터** ({envList.EnvObjects.Count}개 객체)가 **{path}** 경로에 성공적으로 저장되었습니다.");
    }
}

