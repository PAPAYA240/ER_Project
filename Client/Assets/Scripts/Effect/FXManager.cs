using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Data;
using static Data.EffectData;

namespace Assets.Scripts.Effect
{
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

        private static FXManager _instance;
        public static FXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FXManager>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(FXManager).Name);
                        _instance = singletonObject.AddComponent<FXManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LoadFxPrefabs();
            }
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

        public void PlayEffect(List<EffectData> effectData, Transform casterTransform, Vector3 targetPos = new Vector3())
        {
            foreach (EffectData data in effectData)
            {
                if (!fxPrefabs.ContainsKey(data.prefabName))
                {
                    Debug.LogError($"FxManager : {data.prefabName}을 찾을 수 없습니다.");
                    return;
                }
                GameObject fxPrefab = fxPrefabs[data.prefabName];

                // 이펙트가 생성될 위치
                Vector3 spawnPosition = Vector3.zero;
                Transform parentTransform = null;
                switch (data.target)
                {
                    case EEffectTarget.Self:
                        spawnPosition = casterTransform.position;
                        //parentTransform = casterTransform; // TODO : 캐릭터에 붙이느냐 붙이지 않느냐도 판단할 것(이펙트 따라다니면 붙이고 아님..말고)
                        break;
                    case EEffectTarget.Target:
                        spawnPosition = targetPos;
                        parentTransform = null;
                        break;
                    case EEffectTarget.Ground:
                        spawnPosition = data.position;
                        break;
                }
                // 이펙트가 바라볼 방향
                Quaternion spawnQuat = Quaternion.identity;
                spawnQuat = casterTransform.rotation;

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

                // 이펙트 재생
                fxObject.SetActive(false);
                SettingLayer(fxObject, fxLayer);

                // 지속 시간 후 비활
                if (data.duration > 0)
                    StartCoroutine(ReturnToPoolAfterDelay(fxObject, data.prefabName, data.delayTime, data.duration, casterTransform));
            }
        }


        private void SettingLayer(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
                SettingLayer(child.gameObject, newLayer);
        }

        private IEnumerator ReturnToPoolAfterDelay(GameObject fxObject, string prefabName, float delayTime, float duration, Transform casterTransform)
        {
            yield return new WaitForSeconds(delayTime);
            fxObject.SetActive(true);

            yield return new WaitForSeconds(duration);
            fxObject.SetActive(false);
        }
    }
}
