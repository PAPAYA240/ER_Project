using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_IndicatorTheodore : UI_Base
{
    public MyPlayerController Owner { get; private set; } = null;
    private readonly Dictionary<KeyCode, GameObject> _indicatorMap = new Dictionary<KeyCode, GameObject>();
    
    Canvas _qIndicatorCanvas = null;
    Canvas _wIndicatorCanvas = null;
    Canvas _eIndicatorCanvas = null;
    Canvas _rIndicatorCanvas = null;

    Vector3 _targetScaled = new Vector3(1, 1, 1);
    private const float SCALE_SPEED = 1.5f;

    private readonly Dictionary<KeyCode, Action> _skillUpdateMap = new Dictionary<KeyCode, Action>();
    public KeyCode CurrentActiveKey { get; private set; } = KeyCode.None;

    public override void Init()
    {
        Owner = GetComponentInParent<MyPlayerController>();

        // Q Indicator
        _indicatorMap[KeyCode.Q] = Managers.Resource.Instantiate($"UI/Character/Theodore/IndicatorQ");
        _qIndicatorCanvas = _indicatorMap[KeyCode.Q].GetComponent<Canvas>();
        if (_qIndicatorCanvas != null)
        {
            _qIndicatorCanvas.transform.SetParent(Owner.transform, false);
            _qIndicatorCanvas.enabled = false;
        }

        Transform Outpos = Util.FindChildByName(_indicatorMap[KeyCode.Q].transform, "OutCircle");
        _targetScaled = Outpos.localScale + new Vector3(0.01f, 0.01f, 0);
        Transform pos = Util.FindChildByName(_indicatorMap[KeyCode.Q].transform, "InCircle");
        pos.localScale = Vector3.zero;

        // W Indicator
        _indicatorMap[KeyCode.W] = Managers.Resource.Instantiate($"UI/Character/Theodore/IndicatorW");
        _wIndicatorCanvas = _indicatorMap[KeyCode.W].GetComponent<Canvas>();
        if (_wIndicatorCanvas != null)
        {
             _wIndicatorCanvas.transform.SetParent(Owner.transform, false);
            _wIndicatorCanvas.enabled = false;
        }

        // E Indicator
        _indicatorMap[KeyCode.E] = Managers.Resource.Instantiate($"UI/Character/Theodore/IndicatorE");
        _eIndicatorCanvas = _indicatorMap[KeyCode.E].GetComponent<Canvas>();
        if (_eIndicatorCanvas != null)
        {
            _eIndicatorCanvas.transform.SetParent(Owner.transform, false);
            _eIndicatorCanvas.enabled = false;
        }

        // R Indicator
        _indicatorMap[KeyCode.R] = Managers.Resource.Instantiate($"UI/Character/Theodore/IndicatorR");
        _rIndicatorCanvas = _indicatorMap[KeyCode.R].GetComponent<Canvas>();
        if (_rIndicatorCanvas != null)
        {
            _rIndicatorCanvas.transform.SetParent(Owner.transform, false);
            _rIndicatorCanvas.enabled = false;
        }
        _skillUpdateMap[KeyCode.Q] = AbilityQ; 
        _skillUpdateMap[KeyCode.W] = AbilityW;
        _skillUpdateMap[KeyCode.E] = AbilityE;
        _skillUpdateMap[KeyCode.R] = AbilityR;
    }


    private void Update()
    {
        if (CurrentActiveKey == KeyCode.None)
            return;

        // Dictionary를 사용하여 O(1) 시간 복잡도로 스킬 로직 호출
        if (_skillUpdateMap.TryGetValue(CurrentActiveKey, out Action updateAction))
            updateAction?.Invoke();
    }

    public void EnableIndicator(KeyCode key)
    {
        CurrentActiveKey = key;
    }

    public void DisableAllIndicators()
    {
        if (CurrentActiveKey != KeyCode.None)
            DisableIndicator(CurrentActiveKey);

        CurrentActiveKey = KeyCode.None;
    }
    private void DisableIndicator(KeyCode key)
    {
        if (_indicatorMap.TryGetValue(key, out GameObject go))
        {
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = false; 

            if (key == KeyCode.Q)
            {
                Transform pos = Util.FindChildByName(_indicatorMap[KeyCode.Q].transform, "InCircle");
                pos.localScale = Vector3.zero;
            }
        }
    }

    #region Skill W
    private void AbilityW()
    {
        _wIndicatorCanvas.enabled = true;

        Vector3 position = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            position = new Vector3(hit.point.x, hit.point.y, hit.point.z);

        Quaternion rot = Quaternion.LookRotation(position - transform.position);
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, rot.eulerAngles.z);
        _wIndicatorCanvas.transform.rotation = Quaternion.Lerp(rot, _wIndicatorCanvas.transform.rotation, 0);

        Util.FindChildByName(_indicatorMap[KeyCode.W].transform, "Indicator").transform.position = position;
    }
    #endregion

    #region Skill Q
    private void AbilityQ()
    {
        _qIndicatorCanvas.enabled = true;

        Scale();

        Vector3 position = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            position = new Vector3(hit.point.x, hit.point.y, hit.point.z);
        }

        Quaternion rot = Quaternion.LookRotation(position - transform.position);
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, rot.eulerAngles.z);
        _qIndicatorCanvas.transform.rotation = Quaternion.Lerp(rot, _qIndicatorCanvas.transform.rotation, 0);
    }

    private void Scale()
    {
        Transform _inCircleTransform = Util.FindChildByName(_indicatorMap[KeyCode.Q].transform, "InCircle");
        if (Vector3.Distance(_inCircleTransform.localScale, _targetScaled) < 0.01f)
        {
            _inCircleTransform.localScale = _targetScaled;
            return;
        }
        _inCircleTransform.localScale = Vector3.Lerp(
                _inCircleTransform.localScale,
                _targetScaled,
                Time.deltaTime * SCALE_SPEED
            );
    }
    #endregion

    #region Skill E
    private void AbilityE()
    {
        _eIndicatorCanvas.enabled = true;

        Vector3 position = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            position = new Vector3(hit.point.x, hit.point.y, hit.point.z);
        }

        Quaternion rot = Quaternion.LookRotation(position - transform.position);
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, rot.eulerAngles.z);
        _eIndicatorCanvas.transform.rotation = Quaternion.Lerp(rot, _eIndicatorCanvas.transform.rotation, 0);
    }
    #endregion

    #region Skill R
    private void AbilityR()
    {
        _rIndicatorCanvas.enabled = true;

        Vector3 position = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            position = new Vector3(hit.point.x, hit.point.y, hit.point.z);
        }

        Quaternion rot = Quaternion.LookRotation(position - transform.position);
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, rot.eulerAngles.z);
        _rIndicatorCanvas.transform.rotation = Quaternion.Lerp(rot, _rIndicatorCanvas.transform.rotation, 0);
    }
#endregion
}
