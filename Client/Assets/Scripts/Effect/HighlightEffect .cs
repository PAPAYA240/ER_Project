using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Highlight
{
    // 이거 쓰려면 스키닝 메시 + 아웃라인 머터리얼 + 충돌 캡슐 필요
    public class HighlightEffect : MonoBehaviour
    {
        private Renderer myRenderer;
        private Material outlineMaterial;
        private Material[] originalMaterials; 

        void Start()
        {
            myRenderer = GetComponentInChildren<Renderer>();
            outlineMaterial = Resources.Load<Material>("materials/Outline/Outline");
            if (outlineMaterial == null || myRenderer == null)
            {
                Debug.LogError("outline Material || myRenderer 이 null 이다.");
                return;
            }

            originalMaterials = myRenderer.sharedMaterials;

            outlineMaterial = new Material(Shader.Find("Custom/Outline_Shader"));
        }

        void OnMouseEnter()
        {
            if (myRenderer == null) return;

            List<Material> newMaterials = new List<Material>(originalMaterials);

            newMaterials.Add(outlineMaterial);

            myRenderer.materials = newMaterials.ToArray();
        }

        void OnMouseExit()
        {
            if (myRenderer == null) return;

            myRenderer.materials = originalMaterials;
        }

        void OnDestroy()
        {
            if (outlineMaterial != null)
                Destroy(outlineMaterial);
        }
    }
}
