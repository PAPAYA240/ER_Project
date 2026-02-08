using UnityEngine;

//  1. AimAtMousePosition : 플레이어는 고정 상태에서 마우스를 저격하는 Indicator
public class AimStrategy : IIndicatorStrategy
{
    private Transform _root;

    public void Init(GameObject root, PlayerController owner, string prefabName = null)
    {
        _root = root.transform;
    }
    public void Activate() => _root.gameObject.SetActive(true);
    public void Deactivate() => _root.gameObject.SetActive(false);

    public void UpdateStrategy(Vector3 mousePos)
    {
        Vector3 dir = (mousePos - _root.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            _root.rotation = Quaternion.LookRotation(dir);
    }
    public void SetVisible(bool isVisible)
    {
        if (_root != null)
        {
            _root.gameObject.SetActive(isVisible);
        }
    }
}


// 2. TrackMouseCursor : 특정 오브젝트가 마우스를 따라가는 Indicator
public class TrackMouseStrategy : IIndicatorStrategy
{
    private Transform _root;         
    private Transform _targetObject; 
    private Transform _ownerTransform;

    public void Init(GameObject root, PlayerController owner, string prefabName = null)
    {
        _root = root.transform;
        _ownerTransform = owner.transform;
        GameObject child = Util.FindChildByName(root.transform, "Indicator");

        if (child != null)
        {
            _targetObject = child.transform;
        }
        else
        {
            _targetObject = _root;
        }
    }

    public void Activate() => _root.gameObject.SetActive(true);
    public void Deactivate() => _root.gameObject.SetActive(false);
    public void SetVisible(bool isVisible)
    {
        if (_root != null)
        {
            _root.gameObject.SetActive(isVisible);
        }
    }
    public void UpdateStrategy(Vector3 mousePos)
    {
        _root.position = _ownerTransform.position;

        _targetObject.position = new Vector3(mousePos.x, 0.1f, mousePos.z); 

        Vector3 dir = mousePos - _ownerTransform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            _root.rotation = Quaternion.Euler(0, rot.eulerAngles.y, 0);
        }
    }
}

// ExpandScaleOverTime : 크기가 점점 커지는 Indicator
public class ExpandStrategy : IIndicatorStrategy
{
    private Transform _root;  
    private Transform _inCircle;
    private Vector3 _targetScale;
    private const float SPEED = 1.5f;

    public void Init(GameObject root, PlayerController owner, string prefabName = null)
    {
        _inCircle = Util.FindChildByName(root.transform, "InCircle")?.transform;
        var outCircle = Util.FindChildByName(root.transform, "OutCircle")?.transform;
        if (outCircle != null) _targetScale = outCircle.localScale;
    }

    public void Activate()
    {
        if (_inCircle) _inCircle.localScale = Vector3.zero;
    }

    public void UpdateStrategy(Vector3 mousePos)
    {
        if (_inCircle == null) return;
        _inCircle.localScale = Vector3.Lerp(_inCircle.localScale, _targetScale, Time.deltaTime * SPEED);
    }
    public void Deactivate() { }

    public void SetVisible(bool isVisible)
    {
        if (_root != null)
        {
            _root.gameObject.SetActive(isVisible);
        }
    }
}

// ObjectAimAtMousePosition : 테오도르 스나이퍼 전용 Indicator
public class TheodoreSniperStrategy : IIndicatorStrategy
{
    private GameObject _root;
    private PlayerController _owner;

    private Transform _aimObject;

    private Transform _lLine, _rLine;
    private Transform _lStart, _rStart;

    private bool _isAiming = false;
    private Quaternion _fixedPlayerForward;

    private float _animTime = 0f;

    private Vector3 _lStartPos, _rStartPos, _lTargetPos, _rTargetPos;
    private Quaternion _lStartRot, _rStartRot, _lTargetRot, _rTargetRot;

    public void Init(GameObject root, PlayerController owner, string prefabName)
    {
        _root = root;
        _owner = owner;
        Transform t = root.transform;

        if (!string.IsNullOrEmpty(prefabName))
            _aimObject = Util.FindChildByName(t, prefabName)?.transform;

        if (_aimObject == null)
            _aimObject = Util.FindChildByName(t, "ArrowDirection")?.transform;

        _lLine = Util.FindChildByName(t, "L_Line")?.transform;
        _rLine = Util.FindChildByName(t, "R_Line")?.transform;
        _lStart = Util.FindChildByName(t, "L_StartLine")?.transform;
        _rStart = Util.FindChildByName(t, "R_StartLine")?.transform;

        if (_lLine && _lStart)
        {
            _lStartPos = _lStart.localPosition; _lStartRot = _lStart.localRotation;
            _lTargetPos = _lLine.localPosition; _lTargetRot = _lLine.localRotation;
        }
        if (_rLine && _rStart)
        {
            _rStartPos = _rStart.localPosition; _rStartRot = _rStart.localRotation;
            _rTargetPos = _rLine.localPosition; _rTargetRot = _rLine.localRotation;
        }
    }

    public void Activate()
    {
        _root.SetActive(true);
        _isAiming = false;
        _animTime = 0f;

        // 라인 위치 초기화 (로컬 좌표 기준)
        if (_lStart)
        {
            _lStart.gameObject.SetActive(true);
            _lStart.localPosition = _lStartPos;
            _lStart.localRotation = _lStartRot;
        }
        if (_rStart)
        {
            _rStart.gameObject.SetActive(true);
            _rStart.localPosition = _rStartPos;
            _rStart.localRotation = _rStartRot;
        }

        if (_lLine) _lLine.gameObject.SetActive(false);
        if (_rLine) _rLine.gameObject.SetActive(false);
    }

    public void UpdateStrategy(Vector3 mousePos)
    {
        if (!_isAiming)
        {
            _isAiming = true;
            _fixedPlayerForward = Quaternion.LookRotation(_owner.transform.forward);
        }
        _root.transform.rotation = _fixedPlayerForward;
        _root.transform.position = _owner.transform.position;


        if (_aimObject != null && _lLine != null && _rLine != null)
        {
            Vector3 dir = (mousePos - _root.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion atMouse = Quaternion.LookRotation(dir);
                atMouse = Quaternion.Euler(0, atMouse.eulerAngles.y, 0);
                _aimObject.rotation = atMouse;
            }

            Vector3 centerForward = _root.transform.forward; 
            Vector3 targetForward = _aimObject.right;      

            Vector3 lLimitPos = _lLine.right;
            Vector3 rLimitPos = _rLine.right;
            Vector3 rotationYAxis = Vector3.up;

            float currentAngle = Vector3.SignedAngle(centerForward, targetForward, rotationYAxis);
            float lAngle = Vector3.SignedAngle(centerForward, lLimitPos, rotationYAxis);
            float rAngle = Vector3.SignedAngle(centerForward, rLimitPos, rotationYAxis);

            if (!(currentAngle >= lAngle && currentAngle <= rAngle))
            {
                float clampedAngle = Mathf.Clamp(currentAngle, lAngle, rAngle);

                Quaternion angleAdjustment = Quaternion.AngleAxis(clampedAngle, rotationYAxis);
                Vector3 newRightDirection = angleAdjustment * centerForward;

                Vector3 finalForward = Quaternion.AngleAxis(-90f, rotationYAxis) * newRightDirection;

                Quaternion clampedRotation = Quaternion.LookRotation(finalForward, rotationYAxis);
                _aimObject.rotation = clampedRotation;
            }
        }

        PlayLineAnimation();
    }

    private void PlayLineAnimation()
    {
        if (_lStart == null || _rStart == null) return;

        _animTime += Time.deltaTime;
        float t = _animTime / 0.4f; 

        if (t <= 1.0f)
        {
            _lStart.localPosition = Vector3.Lerp(_lStartPos, _lTargetPos, t);
            _lStart.localRotation = Quaternion.Lerp(_lStartRot, _lTargetRot, t);

            _rStart.localPosition = Vector3.Lerp(_rStartPos, _rTargetPos, t);
            _rStart.localRotation = Quaternion.Lerp(_rStartRot, _rTargetRot, t);
        }
    }

    public void Deactivate() => _root.SetActive(false);

    public void SetVisible(bool isVisible)
    {
        if (_root != null) _root.gameObject.SetActive(isVisible);
    }
}