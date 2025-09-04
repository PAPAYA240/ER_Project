using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Level : UI_Base
{

    enum Images
    {
        ExpBar
    }

    enum Texts
    {
        LevelText
    }

    enum GameObjects
    {
        CombatImage
    }

    public Action<int> OnLevelUp = null;  

    int _currentLevel;
    int _maxExp = 1000;
    int _curExp = 0;

    public int CurrentLevel { get { return _currentLevel; } set { _currentLevel = value; SetLevelText(); } }
    //public int CurrentExp { get { return _curExp; } set { _curExp = value; } }
    public int MaxExp { get { return _maxExp; } set { _maxExp = value; } }

    //TODO 최대 경험치를 배열로 관리해야 될까?

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        CurrentLevel = 1;
        ChangeExpBarFillAmount();
        ActivateCombatImg(false);
    }

    void Update()
    {
        
    }

    void SetLevelText()
    {
        GetText((int)Texts.LevelText).text = _currentLevel.ToString();
    }

    void ChangeExpBarFillAmount()
    {
        GetImage((int)Images.ExpBar).fillAmount = (float)_curExp / _maxExp;
    }

    public void EarnExp(int exp)
    {
        _curExp += exp;

        if( _curExp >= _maxExp )
        {
            int Level = 0;
            //레벨업
            while(_curExp >= _maxExp)
            {
                _curExp -= _maxExp;
                CurrentLevel += 1;
                Level += 1;
            }

            OnLevelUp?.Invoke(Level);
        }

        //경험치 바 갱신
        ChangeExpBarFillAmount();
    }

    public void ActivateCombatImg(bool activate)
    {
        GetObject((int)GameObjects.CombatImage).SetActive(activate);
    }
}
