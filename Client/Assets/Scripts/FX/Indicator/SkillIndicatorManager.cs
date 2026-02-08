using System.Collections.Generic;
using UnityEngine;
using Data;
using Google.Protobuf.Protocol;

public class SkillIndicatorManager : MonoBehaviour
{
    private Dictionary<KeyCode, List<IIndicatorStrategy>> _strategies
        = new Dictionary<KeyCode, List<IIndicatorStrategy>>();

    private List<IIndicatorStrategy> _activeStrategies = new List<IIndicatorStrategy>();

    private PlayerController _owner;
    private Camera _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;
        _owner = GetComponentInParent<PlayerController>();
    }

    public void Init(CharacterType myCharType)
    {
        if (!DataManager.IndicatorDict.ContainsKey(myCharType)) 
            return;

        var configDict = DataManager.IndicatorDict[myCharType];

        foreach (var kvp in configDict)
        {
            KeyCode key = kvp.Key;
            SkillIndicatorConfig data = kvp.Value;

            GameObject instance = Managers.Resource.Instantiate(data.indicatorPrefabPath);
            instance.transform.SetParent(transform);
            instance.transform.localPosition = Vector3.zero;
            instance.SetActive(false);

            if (!_strategies.ContainsKey(key))
                _strategies[key] = new List<IIndicatorStrategy>();

            foreach (string funcName in data.invokeFuncs)
            {
                IIndicatorStrategy strategy = IndicatorStrategyFactory.Create(funcName);

                if (strategy != null)
                {
                    strategy.Init(instance, _owner, data.prefabName);
                    _strategies[key].Add(strategy); 
                }
            }
        }
    }

    private void Update()
    {
        if (_activeStrategies.Count == 0) return;

        Vector3 mousePos = GetMouseWorldPosition();

        for (int i = 0; i < _activeStrategies.Count; i++)
        {
            _activeStrategies[i].UpdateStrategy(mousePos);
        }
    }

    public void EnableIndicator(KeyCode key)
    {
        if (_strategies.ContainsKey(key))
        {
            foreach (var strategy in _strategies[key])
            {
                if (!_activeStrategies.Contains(strategy))
                {
                    strategy.Activate();
                    _activeStrategies.Add(strategy);
                }
            }
        }
    }

    public void DisableIndicator(KeyCode key)
    {
        if (_strategies.ContainsKey(key))
        {
            foreach (var strategy in _strategies[key])
            {
                strategy.Deactivate();
                _activeStrategies.Remove(strategy);
            }
        }
    }
    public void ToggleVisual(KeyCode key, bool isVisible)
    {
        if (_strategies.ContainsKey(key))
        {
            foreach (var strategy in _strategies[key])
            {
                if (_activeStrategies.Contains(strategy))
                {
                    strategy.SetVisible(isVisible);
                }
            }
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return Vector3.zero;
    }
}