using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Data.EffectData;

public class EffectFXManager : MonoBehaviour
{
    private Dictionary<int, List<GameObject>> currentlyPlayingEffects = new Dictionary<int, List<GameObject>>();
    private Dictionary<int, Coroutine> activeCoroutines = new Dictionary<int, Coroutine>();
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

    public List<GameObject> PlayEffect
        (int ownerId, 
        List<EffectData> effectData, 
        Transform casterTransform,
        Vector3 mousePos,
        Vector3 targetPos, 
        Quaternion rot = new Quaternion())
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

            // CasterTransform 부모 설정
            Transform copyTransform = casterTransform;
            if (casterTransform != null && data.attachBoneName != null)
            {
                copyTransform = Util.FindChildByName(casterTransform, data.attachBoneName).transform;
            }

            // Transform 설정
            Quaternion spawnRot
                = GetSpawnRotation(data, copyTransform, rot);
            Vector3 spawnPos 
                = GetSpawnPosition(ownerId, data, copyTransform, mousePos, targetPos, spawnRot, out Transform parentTransform);

            if (data.target == EEffectTarget.Self)
            {
                fxObject.transform.SetParent(copyTransform);
                fxObject.transform.localPosition = data.position;
                fxObject.transform.localRotation = Quaternion.identity;
            }
            else if(data.target == EEffectTarget.Enemy)
            {
                fxObject.transform.SetParent(casterTransform);
                fxObject.transform.SetPositionAndRotation(spawnPos, spawnRot);
            }
            else
            {
                fxObject.transform.SetParent(null);
                fxObject.transform.SetPositionAndRotation(spawnPos, spawnRot);
            }

            // Moving 동작
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

        activeCoroutines[fxObject.GetInstanceID()] = StartCoroutine(ReturnToPoolAfterDelay(ownerId, fxObject, data.prefabName, data.delayTime, data.duration, casterTransform));

        if (data.target == EEffectTarget.Shot)
        {
            GameObject go = Managers.Object.FindById(ownerId);
            BaseController bc = go.GetComponentInChildren<BaseController>();
            if (bc == null)
                return;
            GameObjectType objectType = ObjectManager.GetObjectTypeById(bc.Id);

            if (objectType == GameObjectType.Monster)
            {
                MonsterController mc = bc as MonsterController;
                StartCoroutine(ControlEffect(fxObject, mc.GetTargetForwardVector(), data.duration));
            }
            else
                StartCoroutine(ControlEffect(fxObject, casterTransform.forward, data.duration));
        }
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
        if (effect == null)
            return;

        if (activeCoroutines.ContainsKey(effect.GetInstanceID()))
        {
            StopCoroutine(activeCoroutines[effect.GetInstanceID()]);
            activeCoroutines.Remove(effect.GetInstanceID());
        }

        Managers.FX.Push(effect);

        effect.transform.SetParent(null);
    }

    private IEnumerator ReturnToPoolAfterDelay(int ownerId, GameObject fxObject, string prefabName, float delayTime, float duration, Transform casterTransform)
    {
        if (fxObject == null)
            yield break;

        yield return new WaitForSeconds(delayTime);
        if (fxObject == null)
            yield break;
        fxObject.SetActive(true);

        yield return new WaitForSeconds(duration);
        if (fxObject == null)
            yield break;
        RemoveEffect(ownerId, fxObject);
    }
    #endregion

    #region Transform Helpers
    private Vector3 GetSpawnPosition(int id, EffectData data, Transform casterTransform, Vector3 mousePos, Vector3 targetPos, Quaternion spawnRot, out Transform parentTransform)
    {
        switch (data.target)
        {
            case EEffectTarget.Self:
            case EEffectTarget.Enemy:
                parentTransform = casterTransform;
                return casterTransform.position + data.position;

            case EEffectTarget.Target:
                parentTransform = null;
                Vector3 worldOffset = spawnRot * data.position;
                return targetPos + worldOffset;

            case EEffectTarget.Mouse:
                parentTransform = null;
                return mousePos;

            case EEffectTarget.Shot:
                parentTransform = null;
                return casterTransform.position + data.position;
            default:
                parentTransform = null;
                return Vector3.zero;
        }
    }
    private Quaternion GetSpawnRotation(EffectData data, Transform casterTransform, Quaternion rot)
    {
        switch (data.target)
        {
            case EEffectTarget.Self:
            case EEffectTarget.Enemy:
                return casterTransform.rotation;

            case EEffectTarget.Target:
                return rot;

            case EEffectTarget.Mouse:
            case EEffectTarget.Shot:
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
    public void RemoveEffect(S_RemoveEffect packet)
    {
        GameObject go = Managers.Object.FindById(packet.ObjectId);
        if (go == null) return;

        PlayerController pc = go.GetComponentInChildren<PlayerController>();
        if (pc == null) return;

        SkillEffectList myEffectList = DataManager.PlayerFxDict[pc.ObjInfo.Player.CharType][CreatureState.Skill][(KeyCode)packet.KeyCode];
        List<EffectData> dataList = new List<EffectData>();

        if (currentlyPlayingEffects.TryGetValue(packet.ObjectId, out List<GameObject> activeFxList))
        {
            if(packet.IsCaster)
            {
                foreach (EffectData data in myEffectList.Caster)
                {
                    GameObject fxObjectToRemove = FindEffect(packet.ObjectId, data.prefabName);

                    if (fxObjectToRemove != null)
                        RemoveEffect(packet.ObjectId, fxObjectToRemove);
                }
            }
            else
            {
                foreach (EffectData data in myEffectList.Select)
                {
                    if(data.prefabName == packet.FxName)
                    {
                        GameObject fxObjectToRemove = FindEffect(packet.ObjectId, data.prefabName);

                        if (fxObjectToRemove != null)
                            RemoveEffect(packet.ObjectId, fxObjectToRemove);

                        break;
                    }                    
                }
            }
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