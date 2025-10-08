using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Projectile : MonoBehaviour
{
    private Vector3 _lastForward;
    private UI_TargetingMark targetingMark = null;

    void Start()
    {
        gameObject.SetActive(false);
        // EX ) E Mark
        GameObject mark = Managers.Resource.Instantiate($"UI/Character/Theodore/MarkE");
        targetingMark = mark.gameObject.AddComponent<UI_TargetingMark>();

    }

    IEnumerator CoThrow()
    {
        float elapsedTime = 0f;
        float duration = 3.0f;
        float speed = 10f;

        Vector3 startPosition = gameObject.transform.position;
        while (elapsedTime < duration)
        {
            gameObject.transform.position += _lastForward * speed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            gameObject.SetActive(false);

            // EX ) 테오도르 공격 시, 추가 필요시 로직 수정
            targetingMark.SetTarget(other.gameObject);
            other.GetComponent<CreatureController>().IsStun = true;
        }
    }
    public void Run(Vector3 startPos, Vector3 startforward)
    {
        gameObject.transform.position = startPos;
        _lastForward = startforward;

        StartCoroutine(CoThrow());
    }
}
