using System;
using System.Collections;
using UnityEngine;


public class UI_TargetingMark : UI_Base
{
    private GameObject _target;
    private Camera _camera;
    private Coroutine _lifetimeCoroutine;
    private Coroutine _updatePositionCoroutine;
    private Action _onComplete;
    private float _duration;

    private Vector3 _offset = new Vector3(0, 2.5f, 0);

    public override void Init()
    {
        _camera = Camera.main;
        if (_camera == null)
            _camera = FindFirstObjectByType<Camera>();
    }
    public void Show(GameObject target, float duration, Action onComplete)
    {
        _target = target;
        _duration = duration;
        _onComplete = onComplete;

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<Camera>();
        }

        gameObject.SetActive(true);

        // 기존 코루틴들이 있으면 중지
        if (_lifetimeCoroutine != null)
            StopCoroutine(_lifetimeCoroutine);

        if (_updatePositionCoroutine != null)
            StopCoroutine(_updatePositionCoroutine);

        // 위치 업데이트 코루틴 시작
        _updatePositionCoroutine = StartCoroutine(Co_UpdatePosition());

        // 생명주기 코루틴 시작
        _lifetimeCoroutine = StartCoroutine(Co_Lifetime(duration));
    }

    public void Hide()
    {
        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        if (_updatePositionCoroutine != null)
        {
            StopCoroutine(_updatePositionCoroutine);
            _updatePositionCoroutine = null;
        }

        Poolable poolable = GetComponent<Poolable>();
        if (poolable != null)
        {
            Managers.Pool.Push(poolable);
        }
        else
        {
            gameObject.SetActive(false);
            Debug.Log($"Mark Hide");
        }
    }

    private IEnumerator Co_UpdatePosition()
    {
        while (gameObject.activeInHierarchy)
        {
            MonsterController mc = _target.GetComponentInChildren<MonsterController>();
            if(mc != null)
                transform.position = mc.transform.position + _offset;
            else
                transform.position = transform.position + _offset;
        yield return null;
        }
    }

    private IEnumerator Co_Lifetime(float duration)
    {
        Debug.Log($"Mark 시간 시작: {duration}초");
        yield return new WaitForSeconds(duration);
        Debug.Log($"Mark 시간 끝");

        _onComplete?.Invoke();
        _lifetimeCoroutine = null;

        // 자동으로 Hide 호출
        Hide();
    }
}
