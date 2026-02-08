//using Data;
//using System;
//using UnityEngine;
//using Google.Protobuf.Protocol;
//using System.Collections.Generic;


//public class IndicatorRunTimeData
//{
//    public GameObject owner;
//    public GameObject indicatorObject;
//    public Canvas canvas;
//    public string prefabName;
//    public Transform scaleTarget;
//    public Transform positionTarget;
//    public List<Delegate> invokeList;
//    public bool isInitialized;
//}


//public class SkillIndicator : Monobehaviour
//{
//    private Dictionary<CharacterType, Dictionary<KeyCode, IndicatorRunTimeData>> _skillConfigs
//        = new Dictionary<CharacterType, Dictionary<KeyCode, IndicatorRunTimeData>>();

//    private Dictionary<ValueTuple<CharacterType, KeyCode>, List<Action<Canvas, GameObject, string>>> _activeSkillFuncs
//    = new Dictionary<ValueTuple<CharacterType, KeyCode>, List<Action<Canvas, GameObject, string>>>();

//    private PlayerController _owner;
//    private bool _bInitSetting = false;

//    private bool _setupAiming = false;
//    private Quaternion _fixedPlayerForward = default(Quaternion);

//    public override void Init()
//    {
//        LoadData();
//    }
//    private void Update()
//    {
//        foreach (var keyPair in _activeSkillFuncs)
//        {
//            var value = keyPair.Key;
//            List<Action<Canvas, GameObject, string>> funcList = keyPair.Value;

//            var runTimeData = _skillConfigs[value.Item1][value.Item2];
//            var map = runTimeData.indicatorObject;
//            var canvas = runTimeData.canvas;
//            var prefab = runTimeData.prefabName;

//            foreach (var func in funcList)
//                func.Invoke(canvas, map, prefab);
//        }
//    }

//    public void EnableIndicator(CharacterType charType, KeyCode key)
//    {
//        if (_skillConfigs.ContainsKey(charType) && _skillConfigs[charType].ContainsKey(key))
//        {
//            var runTimeData = _skillConfigs[charType][key];
//            var invokeFuncs = runTimeData.invokeList;
//            var canvas = runTimeData.canvas;

//            var value = new ValueTuple<CharacterType, KeyCode>(charType, key);

//            if (!_activeSkillFuncs.ContainsKey(value))
//                _activeSkillFuncs.Add(value, new List<Action<Canvas, GameObject, string>>());

//            canvas.enabled = true;

//            foreach (var InvokeFunc in invokeFuncs)
//                _activeSkillFuncs[value].Add((Action<Canvas, GameObject, string>)InvokeFunc);
//        }
//    }

//    public Transform FindIndicatorObject(string name)
//    {
//        foreach (var keyPair in _activeSkillFuncs)
//        {
//            var value = keyPair.Key;
//            List<Action<Canvas, GameObject, string>> funcList = keyPair.Value;

//            var runTimeData = _skillConfigs[value.Item1][value.Item2];
//            var map = runTimeData.indicatorObject;
//            return Util.FindChildByName(map.transform, name).transform;
//        }
//        return null;
//    }
//    public void DisableIndicator(CharacterType charType, KeyCode key)
//    {
//        var value = new ValueTuple<CharacterType, KeyCode>(charType, key);
//        if (_activeSkillFuncs.ContainsKey(value))
//        {
//            var canvas = _skillConfigs[charType][key].canvas;
//            if (canvas != null)
//                canvas.enabled = false;

//            _bInitSetting = false;
//            _activeSkillFuncs.Remove(value);
//        }

//        DisableSkillType(charType, key);
//    }

//    // 시각적으로만 활성화/비활성화
//    public void ActiveIndicator(CharacterType charType, KeyCode key, bool bActive)
//    {
//        var value = new ValueTuple<CharacterType, KeyCode>(charType, key);
//        if (_activeSkillFuncs.ContainsKey(value))
//        {
//            var canvas = _skillConfigs[charType][key].canvas;
//            if (canvas != null)
//                canvas.enabled = bActive;
//        }
//    }

//    // 주 Object는 Indicator 이름으로 통일
//    private void TrackMouseCursor(Canvas canvas, GameObject map, string prefabName)
//    {
//        Vector3 position = GetMousePosition();

//        Util.FindChildByName(map.transform, prefabName).transform.position = position;

//        Quaternion rot = Quaternion.LookRotation(position - transform.position);
//        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, 0);

//        map.transform.rotation = rot;
//    }
//    private void AimAtMousePosition(Canvas canvs, GameObject map, string prefabName)
//    {
//        Vector3 position = GetMousePosition();

//        Quaternion atMouse = Quaternion.LookRotation(position - transform.position);
//        atMouse.eulerAngles = new Vector3(0, atMouse.eulerAngles.y, atMouse.eulerAngles.z);
//        map.transform.rotation = Quaternion.Lerp(atMouse, map.transform.rotation, 0);
//    }

//    #region Theodore Action
//    // 테오도르 개인 스킬 취소 함수
//    private void DisableSkillType(CharacterType charType, KeyCode key, bool finalDeactivation = true)
//    {
//        if (key == KeyCode.F1)
//        {
//            var runTimeData = _skillConfigs[charType][key];
//            SkillDReset(runTimeData.indicatorObject);
//            _setupAiming = false;
//        }
//    }


//    private void ObjectAimAtMousePosition(Canvas canvas, GameObject map, string prefabName)
//    {
//        if (!_setupAiming)
//        {
//            _setupAiming = true;
//            _fixedPlayerForward = Quaternion.LookRotation(_owner.transform.forward);
//        }

//        map.transform.rotation = _fixedPlayerForward;

//        // 무조건 플레이어가 바라보는 방향대로 
//        Vector3 position = GetMousePosition();
//        Quaternion atMouse = Quaternion.LookRotation(position - transform.position);
//        atMouse.eulerAngles = new Vector3(0, atMouse.eulerAngles.y, atMouse.eulerAngles.z);

//        GameObject aimObject = Util.FindChildByName(map.transform, prefabName);
//        aimObject.transform.rotation = Quaternion.Lerp(atMouse, aimObject.transform.rotation, 0);

//        // 화살 제한 : 위치가 L보다 커야 하고 R보다 작아야 함 
//        Vector3 aimPos = aimObject.transform.rotation.eulerAngles;
//        Vector3 rotationYAxis = Vector3.up;
//        Vector3 centerForward = map.transform.forward; // 기준 방향
//        Vector3 targetForward = aimObject.transform.right; // 목표 방향

//        Vector3 LLimitPos = Util.FindChildByName(map.transform, "L_Line").transform.right;
//        Vector3 RLimitPos = Util.FindChildByName(map.transform, "R_Line").transform.right;

//        float currentAngle = Vector3.SignedAngle(centerForward, targetForward, rotationYAxis);
//        float LAngle = Vector3.SignedAngle(centerForward, LLimitPos, rotationYAxis);
//        float RAngle = Vector3.SignedAngle(centerForward, RLimitPos, rotationYAxis);

//        // 제한 범위 내에 넘어가지 않았다면
//        if (currentAngle >= LAngle && currentAngle <= RAngle)
//        {
//        }
//        else
//        {
//            float clampedAngle
//                = Mathf.Clamp(currentAngle, LAngle, RAngle);

//            Quaternion angleAdjustment
//                = Quaternion.AngleAxis(clampedAngle, rotationYAxis);

//            Vector3 newRightDirection
//                = angleAdjustment * centerForward;

//            Vector3 finalForward
//                = Quaternion.AngleAxis(-90f, rotationYAxis) * newRightDirection;

//            Quaternion clampedRotation
//                = Quaternion.LookRotation(finalForward, rotationYAxis);

//            aimObject.transform.rotation = clampedRotation;
//        }
//    }

//    private const float SCALE_SPEED = 1.5f;
//    private Vector3 _targetScaled = new Vector3();
//    string[,] subName =
//      {
//            { "L_StartLine", "R_StartLine" }, // 활성화
//            { "L_Line", "R_Line" } // 비활성화
//        };
//    private void ArrowSkillMotion(Canvas canvas, GameObject map, string prefabName)
//    {
//        ChangeArrowLine(map); // L, R

//        for (int i = 0; i < subName.GetLength(0); i++)
//        {
//            GameObject lineObject = Util.FindChildByName(map.transform, subName[0, i]);
//            if (lineObject == null)
//                return;

//            float elapsed = 0f;    // 경과 시간
//            elapsed += Time.deltaTime;
//            float t = elapsed / 0.4f;
//            lineObject.transform.position = Vector3.Lerp(lineObject.transform.position, targetPosition[i], t);
//            lineObject.transform.rotation = Quaternion.Lerp(lineObject.transform.rotation, targetRotation[i], t);
//        }
//    }

//    Vector3[] targetPosition = new Vector3[2];
//    Quaternion[] targetRotation = new Quaternion[2];
//    Vector3[] _originPosition = new Vector3[2];
//    Quaternion[] _originRotation = new Quaternion[2];
//    bool _bArrowInit = false;

//    private void ChangeArrowLine(GameObject map)
//    {
//        if (_bArrowInit)
//            return;
//        #region Shit 더러운 하드코딩
//        Transform LStart = Util.FindChildByName(map.transform, "L_StartLine").transform;
//        Transform RStart = Util.FindChildByName(map.transform, "R_StartLine").transform;
//        Transform L = Util.FindChildByName(map.transform, "L_Line").transform;
//        Transform R = Util.FindChildByName(map.transform, "R_Line").transform;


//        _bArrowInit = true;

//        LStart.gameObject.SetActive(true);
//        RStart.gameObject.SetActive(true);
//        L.gameObject.SetActive(false);
//        R.gameObject.SetActive(false);

//        _originPosition[0] = LStart.position;
//        _originPosition[1] = RStart.position;
//        _originRotation[0] = LStart.rotation;
//        _originRotation[1] = RStart.rotation;

//        targetPosition[0] = L.position;
//        targetPosition[1] = R.position;
//        targetRotation[0] = L.rotation;
//        targetRotation[1] = R.rotation;
//        #endregion 
//    }

//    private void SkillDReset(GameObject map)
//    {
//        #region shit
//        Transform LStart = Util.FindChildByName(map.transform, "L_StartLine").transform;
//        Transform RStart = Util.FindChildByName(map.transform, "R_StartLine").transform;
//        LStart.transform.position = _originPosition[0];
//        RStart.transform.position = _originPosition[1];

//        LStart.transform.rotation = _originRotation[0];
//        RStart.transform.rotation = _originRotation[1];
//        #endregion
//    }
//    private void ExpandScaleOverTime(Canvas canvas, GameObject map, string prefabName)
//    {
//        Transform inCircleTransform = Util.FindChildByName(map.transform, "InCircle").transform;
//        if (!_bInitSetting)
//        {
//            _bInitSetting = true;
//            _targetScaled = Util.FindChildByName(map.transform, "OutCircle").transform.localScale;
//            inCircleTransform.localScale = Vector3.zero;
//        }

//        if (Vector3.Distance(inCircleTransform.localScale, _targetScaled) < 0.01f)
//        {
//            inCircleTransform.localScale = _targetScaled;
//            return;
//        }
//        inCircleTransform.localScale = Vector3.Lerp(
//                inCircleTransform.localScale,
//                _targetScaled,
//                Time.deltaTime * SCALE_SPEED
//            );
//    }
//    #endregion

//    #region Utils
//    private void LoadData()
//    {
//        ICollection<CharacterType> allCharacts = DataManager.IndicatorDict.Keys;
//        System.Type thisType = this.GetType();

//        foreach (CharacterType character in allCharacts)
//        {
//            _owner = GetComponentInParent<MyPlayerController>();

//            if (!_skillConfigs.ContainsKey(character))
//                _skillConfigs.Add(character, new Dictionary<KeyCode, IndicatorRunTimeData>());

//            var keyConfigs = _skillConfigs[character];
//            Dictionary<KeyCode, SkillIndicatorConfig> config = DataManager.IndicatorDict[character];
//            ICollection<KeyCode> allKeyCodes = config.Keys;

//            foreach (KeyCode key in allKeyCodes)
//            {
//                keyConfigs[key] = new IndicatorRunTimeData();
//                keyConfigs[key].invokeList = new List<Delegate>();

//                var prefabAddress = config[key].indicatorPrefabPath;
//                keyConfigs[key].indicatorObject = Managers.Resource.Instantiate(prefabAddress);
//                keyConfigs[key].prefabName = config[key].prefabName;
//                keyConfigs[key].indicatorObject.transform.SetParent(_owner.transform);
//                keyConfigs[key].indicatorObject.transform.localPosition = Vector3.zero;

//                keyConfigs[key].canvas = keyConfigs[key].indicatorObject.GetComponent<Canvas>();
//                keyConfigs[key].canvas.transform.SetParent(_owner.transform, false);
//                keyConfigs[key].canvas.transform.localPosition = Vector3.zero;
//                keyConfigs[key].canvas.enabled = false;

//                foreach (string funcName in config[key].invokeFuncs)
//                {
//                    System.Reflection.MethodInfo method = thisType.GetMethod(funcName,
//                        System.Reflection.BindingFlags.NonPublic |
//                        System.Reflection.BindingFlags.Public |
//                        System.Reflection.BindingFlags.Instance);

//                    if (method != null)
//                    {
//                        var actionDelegate = (Action<Canvas, GameObject, string>)
//                            Delegate.CreateDelegate(typeof(Action<Canvas, GameObject, string>), this, method);
//                        keyConfigs[key].invokeList.Add(actionDelegate);
//                    }
//                }
//            }
//        }
//    }
//    private Vector3 GetMousePosition()
//    {
//        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

//        // 고정된 Y=0 평면과의 교차점 계산
//        float rayDistance;
//        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

//        if (groundPlane.Raycast(ray, out rayDistance))
//        {
//            Vector3 point = ray.GetPoint(rayDistance);
//            return new Vector3(point.x, 0f, point.z); // Y를 0으로 고정
//        }

//        return Vector3.zero;
//    }
//    #endregion

//}
