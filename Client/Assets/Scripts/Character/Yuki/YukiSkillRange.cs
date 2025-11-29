using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class YukiSkillRange : MonoBehaviour, IEffect
{
    private Coroutine _co;
    public Image _backgroundImage;
    public Image _foregroundImage;

    void Start()
    {
        transform.localPosition = Vector3.zero;

        _backgroundImage.type = Image.Type.Filled;
        _backgroundImage.fillMethod = Image.FillMethod.Radial360; // 원형
        _backgroundImage.fillOrigin = 3; // 0: 위
        _backgroundImage.fillClockwise = true; // 시계방향
        _backgroundImage.fillAmount = 0f; // 시작은 비어있게

        _foregroundImage.type = Image.Type.Filled;
        _foregroundImage.fillMethod = Image.FillMethod.Vertical;
        _foregroundImage.fillOrigin = 1; // 0: 왼쪽, 1: 오른쪽
        _foregroundImage.fillAmount = 0.5f; // 50%만 그리기
    }

    public void Play()
    {
        gameObject.SetActive(true);

        if (_co != null)
            StopCoroutine(_co);

        _co = StartCoroutine(FillAndHide(1f));
    }

    private IEnumerator FillAndHide(float duration)
    {
        float timer = 0f;
        _backgroundImage.fillAmount = 0.0f;

        // 0~0.5 범위로 1초 동안 채우기
        while (timer < duration)
        {
            timer += Time.deltaTime;
            _backgroundImage.fillAmount = Mathf.Clamp01(timer / duration * 0.5f);
            yield return null;
        }

        Managers.EffectHandler.PlayEffect(SkillEffectType.RShadow);
        Managers.EffectHandler.PlayEffect(SkillEffectType.RAttack);

        _backgroundImage.fillAmount = 0.5f; // 확실하게 반만 채움

        // 잠깐 보여주고 숨기기
        yield return new WaitForSeconds(0.1f);

        gameObject.SetActive(false);
        _co = null;
    }

    public void Stop()
    {
        throw new System.NotImplementedException();
    }
}
