using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BuffIconUI : MonoBehaviour
{
    [SerializeField] private Sprite[] _sprites;
    [SerializeField] private Color[] _Colors;          

    [SerializeField] private Image IconImage;          
    [SerializeField] private Color IconColor;          

    public void SetIcon(int index)
    {
        IconImage.sprite = _sprites[index];
        IconImage.color = _Colors[index];
    }
}

