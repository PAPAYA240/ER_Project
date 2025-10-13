using System.Collections;
using UnityEngine;

public class UI_TargetingMark : UI_Base
{
    private RectTransform uiElementRect;
    private GameObject _target;
    private Canvas canvas;
    private float _duration;

    private Vector3 offset = new Vector3(0, 2.5f, 0);

    public override void Init()
    {
        gameObject.SetActive(false);
    }

    float _elapsedTime = 0;
    Coroutine _co = null;
    public GameObject ShowCCMark(GameObject target, float duration = 5.0f)
    {
        gameObject.SetActive(true);
        _target = target;
        _duration = duration;
        _elapsedTime = 0;
        _co = StartCoroutine(CoActive());

        return this.gameObject;
    }

    public void HideCCMark()
    {
        StopCoroutine(_co);
        gameObject.SetActive(false);

        _co = null;
        _elapsedTime = 0;
    }

    IEnumerator CoActive()
    {
        while (_elapsedTime < _duration)
        {
            _elapsedTime += Time.deltaTime;
            yield return null;
        }

         _co = null;
        gameObject.SetActive(false);
    }
}
