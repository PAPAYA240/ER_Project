using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
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

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Player"))
            gameObject.SetActive(false);
    }

    public void Run(Vector3 startPos, Vector3 startforward)
    {
        gameObject.transform.position = startPos;
        _lastForward = startforward;

        StartCoroutine(CoThrow());
    }

}
