using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UI_MinimapCharIcon;

public class UI_MinimapCharIcon : Monobehaviour
{
    enum Images
    { CharImg, BoundaryCircle }

    public enum IconType { None, MyPlayer, TeamPlayer, EnemyPlayer }
    public PlayerController Target { get; set; }

    Quaternion _quat = Quaternion.AngleAxis(225, Vector3.up);

    

    IconType _iconType;
    public IconType Type
    { 
        get { return _iconType; } 
        set {
            _iconType = value;
            switch (_iconType)
            {
                case IconType.None:
                    GetImage((int)Images.BoundaryCircle).color = Color.white;
                    break;
                case IconType.MyPlayer:
                    GetImage((int)Images.BoundaryCircle).color = Color.green;
                    break;
                case IconType.TeamPlayer:
                    GetImage((int)Images.BoundaryCircle).color = Color.blue;
                    break;
                case IconType.EnemyPlayer:
                    GetImage((int)Images.BoundaryCircle).color = Color.red;
                    break;
            }
        } 
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector3.zero;
        rectTransform.anchoredPosition3D = Vector3.zero;
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
        //��ġ�� ������Ʈ�Ѵ�.

        if (Target != null)
        {
            Vector3 targetPos = Target.transform.position;
            RectTransform rectTransform = GetComponent<RectTransform>();

            Vector3 newPos = Vector3.zero;
            newPos.x = -targetPos.x - targetPos.z;
            newPos.y = targetPos.x - targetPos.z;


            rectTransform.anchoredPosition = newPos;
            //rectTransform.position = Vector3.zero;
        }
    }

    public void SetCharIcon(CharacterType type)
    {
        GetImage((int)Images.CharImg).sprite = Managers.Resource.Load<Sprite>($"Sprite/CharMap_{type.ToString()}_S000");
    }
}
