using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;

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
        if (other.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            gameObject.SetActive(false);

            CreatureController targetController = other.GetComponent<CreatureController>();
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
