using System.Collections;
using UnityEngine;

public class Fx_Hyunwoo_Skill04_Range : MonoBehaviour
{
    float _maxTime = 1.2f;
    float _elapsed = 0;
    //Coroutine _coroutine;


    void Start()
    {
        //_coroutine = StartCoroutine(CoStart());
    }


    void Update()
    {
        _elapsed += Time.deltaTime;
        //Debug.Log($"@@elapsed = {_elapsed}");
        if (_elapsed < _maxTime)
        {
            float ratio = Mathf.Min(1, _elapsed / _maxTime);
            gameObject.transform.localScale = new Vector3(1, ratio, 1);
            //Debug.Log($"@@Ratio = {ratio}");
        }
    }

    //IEnumerator CoStart()
    //{
    //    float elapsed = 0;
    //    while (true)
    //    {
    //        elapsed += Time.deltaTime;
    //        Debug.Log($"@@elapsed = {elapsed}");
    //        if (elapsed < _maxTime)
    //        {
    //            float ratio = Mathf.Min(1, elapsed / _maxTime);
    //            gameObject.transform.localScale = new Vector3(1, ratio, 1);
    //            Debug.Log($"@@Ratio = {ratio}");
    //            yield return null;
    //        }

    //        break;
    //    }

    //    _coroutine = null;
    //}

    //private void OnDestroy()
    //{
    //    if(null != _coroutine)
    //    {
    //        StopCoroutine(_coroutine);
    //        _coroutine = null;
    //    }
    //}
}
