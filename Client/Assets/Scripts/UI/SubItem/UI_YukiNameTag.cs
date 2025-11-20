using UnityEngine;

public class UI_YukiNameTag : UI_PlayerNameTag
{
    UI_YukiStud _uiStud;

    public override void Init()
    {
        base.Init();

        _uiStud = GetComponentInChildren<UI_YukiStud>();
    }

    public void SetStud(int count)
    {
        if(null != _uiStud)
            _uiStud.SetStud(count);
    }
}
