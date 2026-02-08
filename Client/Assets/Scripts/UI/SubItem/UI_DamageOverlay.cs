using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UI_DamageOverlay : Monobehaviour 
{
    enum Images
    {
        DamageImage
    }

    public float fadeDuration = 0.2f;

    private Color _flashColor = new Color(1f, 1f, 1f, 0.0f); 

    private readonly Color _targetColor = new Color(1f, 1f, 1f, 0.4f);

    private Image _overlayImage;

    public override void Init()
    {
        Bind<Image>(typeof(Images));

        _overlayImage = GetImage((int)Images.DamageImage);
        _overlayImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
    }

    private Coroutine _co = null;
    public void ActiveDamageScreen()
    {
        if (_overlayImage == null)
            return;

        if (_co != null)
        {
            StopCoroutine(_co);
            _overlayImage.color = _flashColor;
        }
        gameObject.SetActive(true);
        _co = StartCoroutine(StartDamageScreenCo());
    }

    private IEnumerator StartDamageScreenCo()
    {
        if (_overlayImage == null)
            yield break;

        float timer = 0f;
        Color startColor = _flashColor; 
        Color endColor = _targetColor;  

        while (timer < fadeDuration) 
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            _overlayImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        timer = 0f;
        startColor = _targetColor; 
        endColor = new Color(_targetColor.r, _targetColor.g, _targetColor.b, 0f); // Alpha 0.0 (완전 투명)

        while (timer < fadeDuration) 
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            _overlayImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        _overlayImage.color = endColor;
        gameObject.SetActive(false);
        _co = null;
    }
}
