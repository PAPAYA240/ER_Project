using UnityEngine;

public class Yuki_Trail : MonoBehaviour, IEffect
{
    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Play()
    {
        gameObject.SetActive(true);
    }

    public void Stop()
    {
        gameObject.SetActive(false);
    }
}
