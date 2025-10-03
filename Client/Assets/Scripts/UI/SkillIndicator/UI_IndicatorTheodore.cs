using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UI_IndicatorTheodore : UI_Base
{
    public MyPlayerController _owner = null;
    Canvas _indicatorCanvas = null;
    Canvas _indicatorCanvasW = null;
    Vector3 _targetScaled = new Vector3(1, 1, 1);
    private Dictionary<KeyCode, GameObject> _indicatorMap = new Dictionary<KeyCode, GameObject>();

    public KeyCode _lastKey = KeyCode.None;
    public override void Init()
    {
        // Q Indicator
        _indicatorMap[KeyCode.Q] = Managers.Resource.Instantiate($"UI/Character/Theodore/IndicatorQ");
        _indicatorCanvas = _indicatorMap[KeyCode.Q].GetComponent<Canvas>();
        if (_indicatorCanvas != null)
        {
            _indicatorCanvas.transform.SetParent(_owner.transform, false);
            _indicatorCanvas.enabled = false;
        }

        Transform pos = Util.FindChildByName(_indicatorMap[KeyCode.Q].transform, "OutCircle");
        _targetScaled = pos.localScale + new Vector3(0.01f, 0.01f, 0);

        // W Indicator
        _indicatorMap[KeyCode.W] = Managers.Resource.Instantiate($"UI/Character/Theodore/IndicatorW");
        _indicatorCanvasW = _indicatorMap[KeyCode.W].GetComponent<Canvas>();
        if (_indicatorCanvasW != null)
        {
             _indicatorCanvasW.transform.SetParent(_owner.transform, false);
            _indicatorCanvasW.enabled = false;
        }
    }

    public void SetLastKey(KeyCode key)
    {
        _lastKey = key;
    }
    private void Update()
    {
        if (_indicatorCanvas == null)
            return;

        switch (_lastKey)
        {
            case KeyCode.Q:
                AbilityQ();
            break;

            case KeyCode.W:
                AbilityW();
            break;
        }

        //_indicatorCanvas.enabled = false;
        //Transform pos = Util.FindChildByName(_indicatorMap[KeyCode.Q].transform, "InCircle");
        //pos.localScale = Vector3.zero;
    }

    #region Skill W
    private void AbilityW()
    {
        _indicatorCanvasW.enabled = true;

        Vector3 position = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            position = new Vector3(hit.point.x, hit.point.y, hit.point.z);

        Quaternion rot = Quaternion.LookRotation(position - transform.position);
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, rot.eulerAngles.z);
        _indicatorCanvasW.transform.rotation = Quaternion.Lerp(rot, _indicatorCanvasW.transform.rotation, 0);

        Util.FindChildByName(_indicatorMap[KeyCode.W].transform, "Indicator").transform.position = position;
    }
    #endregion

    #region Skill Q
    private void AbilityQ()
    {
        _indicatorCanvas.enabled = true;

        Scale();

        Vector3 position = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            position = new Vector3(hit.point.x, hit.point.y, hit.point.z);
        }

        Quaternion rot = Quaternion.LookRotation(position - transform.position);
        rot.eulerAngles = new Vector3(0, rot.eulerAngles.y, rot.eulerAngles.z);
        _indicatorCanvas.transform.rotation = Quaternion.Lerp(rot, _indicatorCanvas.transform.rotation, 0);
    }

    private float _scaleSpeed = 1.5f;
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
                Time.deltaTime * _scaleSpeed
            );
    }
    #endregion
}
