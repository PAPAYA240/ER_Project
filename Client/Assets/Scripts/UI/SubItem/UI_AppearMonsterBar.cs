using TMPro;
using System.Collections;
using UnityEngine;

public class UI_AppearMonsterBar : UI_Base
{
    [SerializeField] public CanvasGroup targetCanvasGroup;
    enum Texts
    {
        Text,
    }
    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
    }

    private IEnumerator FadeToAlpha(float targetAlpha, float duration)
    {
        if (targetCanvasGroup == null)
            yield break;

        float startAlpha = targetCanvasGroup.alpha;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            targetCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        targetCanvasGroup.alpha = targetAlpha;
    }

    public IEnumerator FadeUpAndDown(float fadeDuration, float waitTime)
    {
        yield return StartCoroutine(FadeToAlpha(1.0f, fadeDuration));
        yield return new WaitForSeconds(waitTime);
        yield return StartCoroutine(FadeToAlpha(0f, fadeDuration));
    }
    public void Active(int phase)
    {
        // 몬스터만 쓰는 바인줄 알았음 ㅎ
        if (GetText((int)Texts.Text) == null)
            return;

        if (0 == phase)
            GetText((int)Texts.Text).text = "실험을 곧 시작합니다.";
        else if (1 == phase)
            GetText((int)Texts.Text).text = "전투 지역이 개방되었습니다.";
        else if (2 == phase)
            GetText((int)Texts.Text).text = "오메가가 출현했습니다.";
        else if (3 == phase)
            GetText((int)Texts.Text).text = "감마가 출현했습니다.";

        StartCoroutine(FadeUpAndDown(0.5f, 2.0f));
    }

}

