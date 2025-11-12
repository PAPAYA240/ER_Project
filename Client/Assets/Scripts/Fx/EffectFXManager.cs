using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Data;
using static Data.EffectData;
using Google.Protobuf.Protocol;

public class EffectFXManager : MonoBehaviour
{
    private Dictionary<int, List<GameObject>> currentlyPlayingEffects = new Dictionary<int, List<GameObject>>();
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();
    private int fxLayer;


    public void Init()
    {
        LoadFxPrefabs();
        fxLayer = LayerMask.NameToLayer("FX");
    }

    private GameObject GetFxPrefab(int ownerId, string prefabName)
    {
        GameObject owner = Managers.Object.FindById(ownerId);
        CreatureController ownerCreature = owner?.GetComponent<CreatureController>();
        GameObject fxPrefab = null;
        if (ownerCreature is PlayerController)
        {
            CharacterType type = ownerCreature.ObjInfo.Player.CharType;
            fxPrefab = Managers.Resource.Load<GameObject>($"effects/prefab/{type}/{prefabName}");
        }
        else
            fxPrefab = Managers.Resource.Load<GameObject>($"effects/prefab/Monster/{prefabName}");

        return fxPrefab;
    }

    public List<GameObject> PlayEffect(int ownerId, List<EffectData> effectData, Transform casterTransform, Vector3 targetPos = new Vector3(), Quaternion rot = new Quaternion())
    {
        if (effectData == null || effectData.Count == 0)
            return null;

        List<GameObject> effectList = new List<GameObject>();

        foreach (EffectData data in effectData)
        {
            GameObject fxPrefab = GetFxPrefab(ownerId, data.prefabName);
            if(fxPrefab == null)
            {
                Debug.LogWarning($"FX Prefab not found: {data.prefabName}");
                continue;
            }

            GameObject fxObject = Managers.FX.Pop(fxPrefab, null);
            if (fxObject == null)
            {
                Debug.LogError($"Failed to pop FX from pool: {data.prefabName}");
                continue;
            }

            Transform copyTransform = casterTransform;
            if (casterTransform != null && data.attachBoneName != null)
                copyTransform = Util.FindChildByName(casterTransform, data.attachBoneName).transform;

            fxObject.transform.SetPositionAndRotation(
                 GetSpawnPosition(ownerId, data, copyTransform, targetPos, out Transform parentTransform),
                 GetSpawnRotation(data, copyTransform, targetPos, rot));
            fxObject.transform.SetParent(parentTransform);

            SettingLayer(fxObject, fxLayer);
            StartEffectLogic(ownerId, fxObject, data, copyTransform);
            effectList.Add(fxObject);
        }

        // 진행 중인 이펙트 리스트
        if (effectList != null && effectList.Count > 0)
        {
            if (!currentlyPlayingEffects.ContainsKey(ownerId))
                currentlyPlayingEffects[ownerId] = new List<GameObject>();
            currentlyPlayingEffects[ownerId].AddRange(effectList);
        }
        return effectList;
    }

    private void StartEffectLogic(int ownerId, GameObject fxObject, EffectData data, Transform casterTransform)
    {
        if (casterTransform == null) 
            return;

        fxObject.SetActive(false);

        activeCoroutines[fxObject] = StartCoroutine(ReturnToPoolAfterDelay(ownerId, fxObject, data.prefabName, data.delayTime, data.duration, casterTransform));

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

    public void StopAndReturnEffect(GameObject effect)
    {
        if (activeCoroutines.ContainsKey(effect))
        {
            StopCoroutine(activeCoroutines[effect]);
            activeCoroutines.Remove(effect);
        }
    }
    private IEnumerator ReturnToPoolAfterDelay(int ownerId, GameObject fxObject, string prefabName, float delayTime, float duration, Transform casterTransform)
    {
        yield return new WaitForSeconds(delayTime);
        if (fxObject) fxObject.SetActive(true);

        yield return new WaitForSeconds(duration);
        RemoveEffect(ownerId, fxObject);
    }
    #endregion

    #region Transform Helpers
    private Vector3 GetSpawnPosition(int id, EffectData data, Transform casterTransform, Vector3 targetPos, out Transform parentTransform)
    {
        switch (data.target)
        {
            case EEffectTarget.Self:
                parentTransform = casterTransform;
                return casterTransform.position + data.position;

            case EEffectTarget.Target:
                parentTransform = null;
                //CreatureController cc = casterTransform.GetComponent<CreatureController>();
                //if (cc == null) return Vector3.zero;

                return targetPos;

            case EEffectTarget.Mouse:
                parentTransform = null;
                GameObject go = Managers.Object.FindById(id);
                if(go == null) return Vector3.zero;

                PlayerController pc = go.GetComponent<PlayerController>();
                if (pc == null) return Vector3.zero;
                return pc.GetMouseWorldPosition();

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
                return casterTransform.rotation;

            case EEffectTarget.Target:
                return Quaternion.identity;

            case EEffectTarget.Mouse:
            case EEffectTarget.Shoot:
                return rot;

            default:
                return Quaternion.identity;
        }
    }
    #endregion

    #region Utils
    public GameObject FindEffect(int ownerId, string prefabName)
    {
        if (currentlyPlayingEffects.TryGetValue(ownerId, out List<GameObject> effectList))
        {
            foreach (GameObject effect in effectList)
            {
                if (effect.name.Replace("(Clone)", "") == prefabName)
                    return effect;
            }
        }
        return null;
    }
    public void RemoveAllEffect(int ownerId)
    {
        if (currentlyPlayingEffects.TryGetValue(ownerId, out List<GameObject> effectList))
        {
            foreach (GameObject effect in new List<GameObject>(effectList))
                StopAndReturnEffect(effect);

            effectList.Clear();
            currentlyPlayingEffects.Remove(ownerId);
        }
    }
    public void RemoveEffect(int ownerId, GameObject fxObject)
    {
        if (currentlyPlayingEffects.TryGetValue(ownerId, out List<GameObject> effectList))
        {
            StopAndReturnEffect(fxObject);
            effectList.Remove(fxObject);

            if (effectList.Count == 0)
                currentlyPlayingEffects.Remove(ownerId);
        }
    }
    private void LoadFxPrefabs()
    {
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>("effects/prefab");
        foreach (var prefab in loadedPrefabs)
        {
            Managers.FX.CreatePool(prefab, 5);
        }
    }

    private void SettingLayer(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SettingLayer(child.gameObject, newLayer);
    }
    public void Clear()
    {
        List<int> allOwnerIds = new List<int>(currentlyPlayingEffects.Keys);

        foreach (int ownerId in allOwnerIds)
            RemoveAllEffect(ownerId);

        currentlyPlayingEffects.Clear();
        activeCoroutines.Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }
    #endregion

}