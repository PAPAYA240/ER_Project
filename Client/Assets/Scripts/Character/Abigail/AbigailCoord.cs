using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AbigailCoord : MonoBehaviour
{
    [SerializeField] private Image image;

    public void ActivateAbigailCoord(float duration)
    {
        StartCoroutine(RenderForTime(duration));
    }

    public void DeactivateAbigailCoord()
    {
        image.enabled = false;
    }

    IEnumerator RenderForTime(float duration)
    {
        image.enabled = true;
        yield return new WaitForSeconds(duration);
        image.enabled = false;
    }
}
