using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ScoreBar : UI_Base
{
    enum Texts { ScoreText }
    enum GameObjects { Guage }

    int _curScore;
    int _maxScore;

    public int CurrentScore { get { return _curScore; } 
        set { _curScore = Mathf.Max(0, value); UpdateText();  UpdateFillAmount(); } }

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        _maxScore = 40;
        CurrentScore = _maxScore;
    }
    private void Awake()
    {
        Init();
    }

    void Start()
    {
        
    }

    void Update()
    {

    }
    void UpdateText()
    {
        GetText((int)Texts.ScoreText).text = $"{_curScore}";
    }

    void UpdateFillAmount()
    {
        GetObject((int)GameObjects.Guage).GetComponent<RawImage>().material.SetFloat("_Fill", _curScore / (float)_maxScore);
    }
}
