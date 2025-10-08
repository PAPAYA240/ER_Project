using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Data;
using static Data.EffectData;
using Google.Protobuf.Protocol;

public class FXManager : MonoBehaviour
{
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

    // casterTransform = 붙여줄 캐릭터(본체) Transform (특정 뼈대에 붙이고자 한다면 json에 따로 추가)
    public List<GameObject> PlayEffect(List<EffectData> effectData, Transform casterTransform, Vector3 targetPos = new Vector3(), Quaternion rot = new Quaternion())
    {
        if (effectData == null || effectData.Count == 0)
            return null;

        List<GameObject> effectList = new List<GameObject>();

        foreach (EffectData data in effectData)
        {
            if (!fxPrefabs.ContainsKey(data.prefabName))
            {
                Debug.LogError($"FxManager : {data.prefabName}을 찾을 수 없습니다.");
                continue;
            }

            GameObject fxPrefab = fxPrefabs[data.prefabName];
            GameObject fxObject = GetFxFromPool(data.prefabName);

            if (fxObject == null)
            {
                fxObject = Instantiate(fxPrefab);
                if (!fxPool.ContainsKey(data.prefabName))
                    fxPool[data.prefabName] = new List<GameObject>();
               fxObject.gameObject.AddComponent<FxController>();
                fxPool[data.prefabName].Add(fxObject);
            }

            if (data.attachBoneName != null)
                casterTransform = Util.FindChildByName(casterTransform, data.attachBoneName);

            fxObject.transform.SetPositionAndRotation(
                GetSpawnPosition(data, casterTransform, targetPos, out Transform parentTransform), 
                GetSpawnRotation(data, casterTransform, targetPos, rot));
            fxObject.transform.SetParent(parentTransform);

            SettingLayer(fxObject, fxLayer);
            StartEffectLogic(fxObject, data, casterTransform);
            effectList.Add(fxObject);
        }
        return effectList;
    }

    private Vector3 GetSpawnPosition(EffectData data, Transform casterTransform, Vector3 targetPos, out Transform parentTransform)
    {
        switch (data.target)
        {
            case EEffectTarget.Self:
                parentTransform = casterTransform;
                return casterTransform.position + data.position;

            case EEffectTarget.Relative:
                parentTransform = casterTransform;
                Quaternion yawRotationOnly = Quaternion.Euler(0, casterTransform.eulerAngles.y, 0);
                return casterTransform.position + yawRotationOnly * data.position;

            case EEffectTarget.Target:
                parentTransform = null;
                return targetPos;

            case EEffectTarget.Ground:
                parentTransform = null;
                return data.position;

            case EEffectTarget.Shoot:
                parentTransform = null;
                return casterTransform.position + data.position;

            default:
                parentTransform = null;
                return Vector3.zero;
        }
    }
    private Quaternion GetSpawnRotation(EffectData data, Transform casterTransform, Vector3 targetPos, Quaternion rot)
    {
        switch (data.target)
        {
            case EEffectTarget.Self:
            case EEffectTarget.Relative:
                return casterTransform.rotation;

            case EEffectTarget.Target:
            case EEffectTarget.Shoot:
                return rot;

            default:
                return Quaternion.identity;
        }
    }

    private void StartEffectLogic(GameObject fxObject, EffectData data, Transform casterTransform)
    {
        fxObject.SetActive(false);

        activeCoroutines[fxObject] = StartCoroutine(ReturnToPoolAfterDelay(fxObject, data.prefabName, data.delayTime, data.duration, casterTransform));

        if (data.target == EEffectTarget.Shoot)
            StartCoroutine(ControlEffect(fxObject, casterTransform.forward, data.duration));
    }

    #region Shooting
    IEnumerator ControlEffect(GameObject effect, Vector3 forwardTrans, float duration)
    {
        float timer = 0f;
        float moveSpeed = 20f;

        while (timer < duration)
        {
            Vector3 nextPosition = effect.transform.position + forwardTrans * moveSpeed * Time.deltaTime;
            RaycastHit hit;

            if (Physics.Raycast(effect.transform.position, forwardTrans, out hit, 
                (nextPosition - effect.transform.position).magnitude, LayerMask.GetMask("Monster")) ||
                Physics.Raycast(effect.transform.position, forwardTrans, out hit,
                (nextPosition - effect.transform.position).magnitude, LayerMask.GetMask("Player")))
            {
                effect.transform.position = hit.point;
                yield break;
            }

            effect.transform.position = nextPosition;
            timer += Time.deltaTime;
            yield return null;
        }
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
    #endregion

    #region Util
    public List<EffectData> GetSkillEffectList(CharacterType charType, CreatureState state, KeyCode keyCode, bool bTarget = false)
    {
        // 1단계: CharacterType 확인
        if (DataManager.PlayerFxDict== null || !DataManager.PlayerFxDict.TryGetValue(charType, out var stateDict))
            return null;

        // 2단계: CreatureState 확인
        if (!stateDict.TryGetValue(state, out var keyCodeDict))
            return null;

        if (keyCodeDict.TryGetValue(keyCode, out var effectList))
        {
            if (bTarget == false)
                return effectList.Caster;
            else
                return effectList.HitTarget;
        }
        else
            return null;
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
                if (fx == null)
                    continue;

                if (!fx.activeSelf)
                    return fx;
            }
        }
        return null;
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
    #endregion
}
