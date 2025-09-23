using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Data;
using static Data.EffectData;

public class FXManager : MonoBehaviour
{
    /*
     *  ======= 고칠 놈 =======
        1. 캐릭터에 붙이느냐 붙이지 않느냐도 판단할 것
        2. 이펙트가 어느시점까지 플레이어(혹은 적)을 찾는 지 판단해야 함
     */
    private Dictionary<string, GameObject> fxPrefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, List<GameObject>> fxPool = new Dictionary<string, List<GameObject>>();
    private int fxLayer;
   
    public void Init()
    {
        if (fxPrefabs.Count > 0)
            return;
        LoadFxPrefabs();
        fxLayer = LayerMask.NameToLayer("FX");
    }
  
    private void LoadFxPrefabs()
    {
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>("effects/prefab");
        foreach (var prefab in loadedPrefabs)
        {
            fxPrefabs[prefab.name] = prefab;
            fxPool[prefab.name] = new List<GameObject>();
        }
        Debug.Log($"총 {loadedPrefabs.Length}개의 프리팹이 로드되었습니다.");
    }
    private GameObject GetFxFromPool(string prefabName)
    {
        if (fxPool.ContainsKey(prefabName))
        {
            foreach (var fx in fxPool[prefabName])
            {
                if (!fx.activeSelf)
                    return fx;
            }
        }
        return null;
    }

    public List<GameObject> PlayEffect(List<EffectData> effectData, Transform casterTransform, Vector3 targetPos = new Vector3(), Quaternion rot = new Quaternion())
    {
        if (effectData == null)
            return null;

        List<GameObject> effectList = new List<GameObject>();
        foreach (EffectData data in effectData)
        {
            if (!fxPrefabs.ContainsKey(data.prefabName))
            {
                Debug.LogError($"FxManager : {data.prefabName}을 찾을 수 없습니다.");
                return null;
            }

            GameObject fxPrefab = fxPrefabs[data.prefabName];
            Vector3 spawnPosition = Vector3.zero;
            Transform parentTransform = null;

            switch (data.target)
            {
                case EEffectTarget.Self:
                    spawnPosition = casterTransform.position + data.position;
                    parentTransform = casterTransform; // TODO : 캐릭터에 붙이느냐 붙이지 않느냐도 판단할 것(이펙트 따라다니면 붙이고 아님..말고)
                    break;
                case EEffectTarget.Target:
                    spawnPosition = targetPos;
                    parentTransform = null;
                    break;
                case EEffectTarget.Ground:
                    spawnPosition = data.position;
                    break;
            }

            Quaternion spawnQuat = Quaternion.identity;
            spawnQuat = rot;

            // 이펙트 생성
            GameObject fxObject = GetFxFromPool(data.prefabName);
            if (fxObject == null)
            {
                fxObject = Instantiate(fxPrefab, spawnPosition, spawnQuat, parentTransform);
                if (!fxPool.ContainsKey(data.prefabName))
                    fxPool[data.prefabName] = new List<GameObject>();
                fxPool[data.prefabName].Add(fxObject);
            }
            else
            {
                fxObject.transform.position = spawnPosition;
                fxObject.transform.SetParent(parentTransform);
            }

            effectList.Add(fxObject);
            SettingLayer(fxObject, fxLayer);
            if (fxObject)
                fxObject.SetActive(false);

            // 이펙트 시작
            activeCoroutines[fxObject] = StartCoroutine(ReturnToPoolAfterDelay(fxObject, data.prefabName, data.delayTime, data.duration, casterTransform));
        }
        return effectList;
    }
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();
    public void StopAndReturnEffect(GameObject effect)
    {
        if (activeCoroutines.ContainsKey(effect))
        {
            StopCoroutine(activeCoroutines[effect]); 
            activeCoroutines.Remove(effect);
        }
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject fxObject, string prefabName, float delayTime, float duration, Transform casterTransform)
    {
        yield return new WaitForSeconds(delayTime);
        if (fxObject)
            fxObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (fxObject)
            fxObject.SetActive(false);
    }
     private void SettingLayer(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SettingLayer(child.gameObject, newLayer);
    }

    public void Clear()
    {
    }
}
