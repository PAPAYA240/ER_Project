using Data;
using System;
using UnityEngine;
using Google.Protobuf.Protocol;
using System.Collections.Generic;


public class IndicatorRunTimeData
{
    public GameObject owner;
    public GameObject indicatorObject;
    public Canvas canvas;
    public Transform scaleTarget;
    public Transform positionTarget;
    public List<Delegate> invokeList;
    public bool isInitialized;
}


public class SkillIndicator : UI_Base
{
    private Dictionary<CharacterType, Dictionary<KeyCode, IndicatorRunTimeData>> _skillConfigs 
        = new Dictionary< CharacterType, Dictionary<KeyCode, IndicatorRunTimeData>>();

    private Dictionary<ValueTuple<CharacterType, KeyCode>, List<Action<Canvas, GameObject>>> _activeSkillFuncs
    = new Dictionary<ValueTuple<CharacterType, KeyCode>, List<Action<Canvas, GameObject>>>();

    private bool _bInitSetting = false;

    public override void Init()
    {
        LoadData();
    }
    private void Update()
    {
        foreach (var keyPair in _activeSkillFuncs)
        {
            var value= keyPair.Key;
            List<Action<Canvas, GameObject>> funcList = keyPair.Value;

            var runTimeData = _skillConfigs[value.Item1][value.Item2]; 
            var map = runTimeData.indicatorObject;
            var canvas = runTimeData.canvas;

            foreach (var func in funcList)
                func.Invoke(canvas, map);
        }
    }
    public void EnableIndicator(CharacterType charType, KeyCode key)
    {
        if (_skillConfigs.ContainsKey(charType) && _skillConfigs[charType].ContainsKey(key))
        {
            var runTimeData = _skillConfigs[charType][key];
            var invokeFuncs = runTimeData.invokeList;
            var canvas = runTimeData.canvas;

            var value = new ValueTuple<CharacterType, KeyCode>(charType, key);

            if (!_activeSkillFuncs.ContainsKey(value))
                _activeSkillFuncs.Add(value, new List<Action<Canvas, GameObject>>());

            canvas.enabled = true;

            foreach (var InvokeFunc in invokeFuncs)
                _activeSkillFuncs[value].Add((Action<Canvas, GameObject>)InvokeFunc);
        }
    }

    public void DisableIndicator(CharacterType charType, KeyCode key)
    {
        var value = new ValueTuple<CharacterType, KeyCode>(charType, key);
        if(_activeSkillFuncs.ContainsKey(value))
        {
            var canvas = _skillConfigs[charType][key].canvas;
            if (canvas != null)
                canvas.enabled = false;

            _bInitSetting = false;
            _activeSkillFuncs.Remove(value);
        }
    }

    // 주 Object는 Indicator 이름으로 통일
    private void TrackMouseCursor(Canvas canvas, GameObject map)
    {
        Vector3 position = GetMousePosition();

        Quaternion rot = Quaternion.LookRotation(position - transform.position);
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, rot.eulerAngles.z);
        canvas.transform.rotation = Quaternion.Lerp(rot, canvas.transform.rotation, 0);

        Util.FindChildByName(map.transform, "Indicator").transform.position = position;
    }
    private void AimAtMousePosition(Canvas canvs, GameObject map)
    {
        //ExpandScaleOverTime();

        Vector3 position = GetMousePosition();

        Quaternion atMouse = Quaternion.LookRotation(position - transform.position);
        atMouse.eulerAngles = new Vector3(0, atMouse.eulerAngles.y, atMouse.eulerAngles.z);
        map.transform.rotation = Quaternion.Lerp(atMouse, map.transform.rotation, 0);
    }

    #region Theodore Action
    // 스케일을 점점 확대시키는 함수

    private const float SCALE_SPEED = 1.5f;
    private Vector3 _targetScaled = new Vector3();

    private void ExpandScaleOverTime(Canvas canvas, GameObject map)
    {
        Transform inCircleTransform = Util.FindChildByName(map.transform, "InCircle").transform;
        if (!_bInitSetting)
        {
            _bInitSetting = true;
            _targetScaled = Util.FindChildByName(map.transform, "OutCircle").transform.localScale;
            inCircleTransform.localScale = Vector3.zero;
        }

        if (Vector3.Distance(inCircleTransform.localScale, _targetScaled) < 0.01f)
        {
            inCircleTransform.localScale = _targetScaled;
            return;
        }
        inCircleTransform.localScale = Vector3.Lerp(
                inCircleTransform.localScale,
                _targetScaled,
                Time.deltaTime * SCALE_SPEED
            );
    }
    #endregion

    #region Utils
    private void LoadData()
    {
        ICollection<CharacterType> allCharacts = DataManager.IndicatorDict.Keys;
        Type thisType = this.GetType();

        foreach (CharacterType character in allCharacts)
        {
            MyPlayerController owner = GetComponentInParent<MyPlayerController>();

            if (!_skillConfigs.ContainsKey(character))
                _skillConfigs.Add(character, new Dictionary<KeyCode, IndicatorRunTimeData>());

            var keyConfigs = _skillConfigs[character];
            Dictionary<KeyCode, SkillIndicatorConfig> config = DataManager.IndicatorDict[character];
            ICollection<KeyCode> allKeyCodes = config.Keys;

            foreach (KeyCode key in allKeyCodes)
            {
                keyConfigs[key] = new IndicatorRunTimeData();
                keyConfigs[key].invokeList = new List<Delegate>();

                var prefabAddress = config[key].indicatorPrefabPath;
                keyConfigs[key].indicatorObject = Managers.Resource.Instantiate(prefabAddress);

                keyConfigs[key].canvas = keyConfigs[key].indicatorObject.GetComponent<Canvas>();
                keyConfigs[key].canvas.transform.SetParent(owner.transform, false);
                keyConfigs[key].canvas.enabled = false;

                foreach (string funcName in config[key].invokeFuncs)
                {
                    System.Reflection.MethodInfo method = thisType.GetMethod(funcName,
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance);

                    if (method != null)
                    {
                        var actionDelegate = (Action<Canvas, GameObject>)
                            Delegate.CreateDelegate(typeof(Action<Canvas, GameObject>), this, method);
                        keyConfigs[key].invokeList.Add(actionDelegate);
                    }
                }
            }
        }
    }
    private Vector3 GetMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            return new Vector3(hit.point.x, hit.point.y, hit.point.z);
        return Vector3.zero;
    }
    #endregion

}
