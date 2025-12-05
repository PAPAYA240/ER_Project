using Data;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
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

    private GameObject GetFxPrefab(int ownerId, string prefabName, bool isCommon = false)
    {
        GameObject owner = Managers.Object.FindById(ownerId);
        CreatureController ownerCreature = owner?.GetComponent<CreatureController>();
        GameObject fxPrefab = null;
        if (ownerCreature is PlayerController)
        {
            if(!isCommon)
            {
                CharacterType type = ownerCreature.ObjInfo.Player.CharType;
                fxPrefab = Managers.Resource.Load<GameObject>($"effects/prefab/{type}/{prefabName}");
            }
            else
            {
                fxPrefab = Managers.Resource.Load<GameObject>($"effects/prefab/Common/{prefabName}");
            }
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
        Quaternion rot = new Quaternion(),
        bool isCommon = false)
    {
        if (effectData == null || effectData.Count == 0)
            return null;

        List<GameObject> effectList = new List<GameObject>();

        foreach (EffectData data in effectData)
        {
            GameObject fxPrefab = GetFxPrefab(ownerId, data.prefabName, isCommon);
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
            else if (data.target == EEffectTarget.Enemy)
            {
                fxObject.transform.SetParent(casterTransform);
                fxObject.transform.SetPositionAndRotation(spawnPos, spawnRot);
            }
            else if (data.target == EEffectTarget.TargetNoRotation)
            {
                Transform followTarget = casterTransform;
                fxObject.transform.position = spawnPos;
                Quaternion fixedRot = Quaternion.identity;
                fxObject.transform.rotation = fixedRot;

                var follow = fxObject.GetComponent<FX_TargetNoRotation>();
                if (follow == null)
                    follow = fxObject.AddComponent<FX_TargetNoRotation>();

                // data.position 을 world offset으로 쓰고 싶으면
                follow.Setup(followTarget, data.position, fixedRot, faceCamera: false);
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
    public void PlayEffect(string path, Transform casterTransform, float duration, Vector3 offset)
    {
        GameObject prefab = Managers.Resource.Load<GameObject>(path);
        if (prefab == null)
            return;

        GameObject fx = GameObject.Instantiate(prefab);
        fx.transform.SetParent(casterTransform);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;
        fx.transform.localPosition += offset;
        fx.SetActive(true);

        Destroy(fx, duration);
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

        effect?.transform.SetParent(null);
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
            case EEffectTarget.EnemyHit:
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

            case EEffectTarget.Default:
                parentTransform = null;
                Vector3 flatOffset = new Vector3(data.position.x, 0, data.position.z);
                Vector3 worldOffsetDefault = spawnRot * flatOffset;
                return casterTransform.position + worldOffsetDefault + new Vector3(0, data.position.y, 0);

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
            case EEffectTarget.EnemyHit:
                return casterTransform.rotation;

            case EEffectTarget.Target:
                return rot;

            case EEffectTarget.Mouse:
            case EEffectTarget.Shot:
                return rot;

            case EEffectTarget.Default:
                Quaternion baseRot = rot != Quaternion.identity ? rot : casterTransform.rotation;
                return baseRot ;

            default:
                return Quaternion.identity;
        }
    }
    #endregion

    #region Utils
    public GameObject FindCurrentPlayEffect(int ownerId, string prefabName)
    {
        if (currentlyPlayingEffects.TryGetValue(ownerId, out List<GameObject> effectList))
        {
            foreach (GameObject effect in effectList)
            {
                if (effect == null)
                    continue;

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

        if (!currentlyPlayingEffects.TryGetValue(packet.ObjectId, out List<GameObject> activeFxList))
            return;

        if (!DataManager.PlayerFxDict.TryGetValue(pc.ObjInfo.Player.CharType, out var stateDict)) return;
        if (!stateDict.TryGetValue(CreatureState.Skill, out var skillDict)) return;
        if (!skillDict.TryGetValue((KeyCode)packet.KeyCode, out SkillEffectList myEffectList)) return;

        if(packet.Type == "Caster")
        {
            foreach (EffectData data in myEffectList.Caster)
            {
                GameObject fxObjectToRemove = FindCurrentPlayEffect(packet.ObjectId, data.prefabName);

                if (fxObjectToRemove != null)
                    RemoveEffect(packet.ObjectId, fxObjectToRemove);
            }
        }
        else if(packet.Type == "Select")
        {
            foreach (EffectData data in myEffectList.Select)
            {
                if(data.prefabName == packet.FxName)
                {
                    GameObject fxObjectToRemove = FindCurrentPlayEffect(packet.ObjectId, data.prefabName);

                    if (fxObjectToRemove != null)
                        RemoveEffect(packet.ObjectId, fxObjectToRemove);

                    break;
                }                    
            }
        }
    }

    public void RemoveCommonEffect(S_RemoveEffect packet)
    {
        if (!packet.IsCommon)
            return;

        GameObject go = Managers.Object.FindById(packet.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponentInChildren<PlayerController>();
        if (pc == null)
            return;

        SkillEffectList myEffectList = null;

        // 공통 이펙트: CommonFxDict에서 가져오기
        if (!DataManager.CommonFxDict.TryGetValue(packet.CommonName, out myEffectList))
            return;

        // 1) 실제로 재생 중인 FX 목록 있는지 체크
        if (!currentlyPlayingEffects.TryGetValue(packet.ObjectId, out List<GameObject> activeFxList))
            return;

        // 2) Caster / Select 기준으로 제거
        if (packet.IsCaster)
        {
            // Caster 그룹 전체 제거
            foreach (EffectData data in myEffectList.Caster)
            {
                GameObject fxObjectToRemove = FindCurrentPlayEffect(packet.ObjectId, data.prefabName);
                if (fxObjectToRemove != null)
                    RemoveEffect(packet.ObjectId, fxObjectToRemove);
            }
        }
        else
        {
            // Select 그룹에서 FxName과 일치하는 FX만 제거
            foreach (EffectData data in myEffectList.Select)
            {
                if (data.prefabName == packet.FxName)
                {
                    GameObject fxObjectToRemove = FindCurrentPlayEffect(packet.ObjectId, data.prefabName);
                    if (fxObjectToRemove != null)
                        RemoveEffect(packet.ObjectId, fxObjectToRemove);
                    break;
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