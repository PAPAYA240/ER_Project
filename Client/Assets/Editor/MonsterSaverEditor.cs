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
    public float xPos;
    public float yPos;
    public float zPos;

    // 생성자
    public MonsterSaveData(MonsterType type, Vector3 pos)
    {
        this.monsterType = type;
        this.xPos = pos.x;
        this.yPos = pos.y;
        this.zPos = pos.z;
    }
}

// MonsterList.cs
[System.Serializable]
public class MonsterList
{
    public List<MonsterSaveData> monsters;
}

public class MonsterInfoComponent : MonoBehaviour
{
    // 원하는 몬스터 타입을 에디터에서 선택할 수 있도록 설정
    public MonsterType monsterType;
}
public class MonsterSaverEditor : MonoBehaviour
{
    [MenuItem("Tools/Save Monster Data")]
    public static void SaveMonsterSaveData()
    {
        MonsterController[] monstersInScene = FindObjectsOfType<MonsterController>();

        MonsterList monsterList = new MonsterList();
        monsterList.monsters = new List<MonsterSaveData>();

        foreach (MonsterController monsterInfo in monstersInScene)
        {
            Vector3 pos = monsterInfo.transform.position;
            MonsterSaveData data = new MonsterSaveData(monsterInfo._monsterType, pos);
            monsterList.monsters.Add(data);
        }

        string jsonData = JsonUtility.ToJson(monsterList, true); // true는 가독성을 위한 들여쓰기 옵션입니다.

        string path = Application.dataPath + "/MonsterData.json";
        File.WriteAllText(path, jsonData);
        Debug.Log("몬스터 데이터가 " + path + " 경로에 성공적으로 저장되었습니다.");
    }
}




[System.Serializable]
public class EnvSaveData
{
    public EnvType envType;
    public float xPos;
    public float yPos;
    public float zPos;

    // 생성자
    public EnvSaveData(EnvType type, Vector3 pos)
    {
        this.envType = type;
        this.xPos = pos.x;
        this.yPos = pos.y;
        this.zPos = pos.z;
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
    [MenuItem("Tools/Save EnvObject Data")]
    public static void SaveMonsterSaveData()
    {
        EnvironmentObjController[] monstersInScene = FindObjectsOfType<EnvironmentObjController>();

        EnvList envList = new EnvList();
        envList.EnvObjects = new List<EnvSaveData>();

        foreach (EnvironmentObjController envInfo in monstersInScene)
        {
            Vector3 pos = envInfo.transform.position;
            EnvSaveData data = new EnvSaveData(envInfo._envType, pos);
            envList.EnvObjects.Add(data);
        }

        string jsonData = JsonUtility.ToJson(envList, true); 

        string path = Application.dataPath + "/Resources/Data/Env/SpawnEnvData.json";
        File.WriteAllText(path, jsonData);
        Debug.Log("몬스터 데이터가 " + path + " 경로에 성공적으로 저장되었습니다.");
    }
}

