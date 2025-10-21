using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class Projectile : MonoBehaviour
{
    public GameObject Owner { get; set; } = null;

    private Vector3 _lastForward;

    void Start()
    {
        gameObject.SetActive(false);
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
        // 스크린 활용 시 모든 몬스터와 플레이어도 맞게 할 수 있음
        if (other.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            gameObject.SetActive(false);

            CreatureController targetController = other.GetComponent<CreatureController>();

            PlayerController ownerController = Owner.GetComponent<PlayerController>();

            List<CreatureController> hitList = new List<CreatureController>();
            hitList.Add(targetController);
            ownerController.LaunchProjectile(hitList);

            // 마크 활성화 시 구속 가능
            targetController.HasCrowdControl = true;
        }
    }
    public void Run(Vector3 startPos, Vector3 startforward)
    {
        gameObject.transform.position = startPos;
        _lastForward = startforward;

        StartCoroutine(CoThrow());
    }
}
