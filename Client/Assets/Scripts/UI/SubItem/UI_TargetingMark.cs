using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class UI_TargetingMark : UI_Base
{
    private RectTransform uiElementRect;
    private GameObject _target;
    private Canvas canvas;
    private Coroutine _lifetimeCoroutine;
    private Action _onComplete;
    private float _duration;

    private Vector3 _offset = new Vector3(0, 2.5f, 0);

    public override void Init()
    {
        gameObject.SetActive(false);
    }

    float _elapsedTime = 0;
    Coroutine _co = null;
    public void Show(GameObject target, float duration, Action onComplete)
    {
        _target = target;
        _onComplete = onComplete;
        gameObject.SetActive(true);

        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
        }
        _lifetimeCoroutine = StartCoroutine(Co_Lifetime(duration));
    }

    public void Hide()
    {
        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        Poolable poolable = GetComponent<Poolable>();
        if (poolable != null)
        {
            Managers.Pool.Push(poolable);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator Co_Lifetime(float duration)
    {
        yield return new WaitForSeconds(duration);
        _onComplete?.Invoke();
        _lifetimeCoroutine = null;
    }
    

}
