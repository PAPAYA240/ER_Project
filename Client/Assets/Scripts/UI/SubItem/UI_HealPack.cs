using TMPro;
using UnityEngine;

public class UI_HealPack : UI_Base
{
    enum Texts
    {
        Sec_Text,
    }

    enum Images
    {
        Progress,
    }

    const float MAX_TIME = 75;
    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<UnityEngine.UI.Image>(typeof(Images));
    }


    public void SetSecText(int sec)
    {
        var tmp = GetText((int)Texts.Sec_Text);
        if (tmp == null || tmp.gameObject == null)
            return;

        if (sec <= 0)
            tmp.gameObject.SetActive(false);
        else
            tmp.gameObject.SetActive(true);

        tmp.text = $"{sec.ToString()}s";
    }

    public void SetProgressAmount(float currentRemainingTime)
    {
        float ratio = currentRemainingTime / MAX_TIME; 
        float progress = 1.0f - ratio;
        GetImage((int)Images.Progress).fillAmount = progress;
    }
}
