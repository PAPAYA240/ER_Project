using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Highlight
{
    public class HighlightEffect : MonoBehaviour
    {
        private Renderer myRenderer;
        private Material outlineMaterial;
        private Material[] originalMaterials; // 원래 재질 배열을 저장할 변수

        void Start()
        {
            myRenderer = GetComponentInChildren<Renderer>();
            outlineMaterial = Resources.Load<Material>("Material/Outline/Outline");
            if (outlineMaterial == null)
            {
                Debug.LogError("아웃라인 머터리얼 ㄹ못찾음.");
                return;
            }

            if (myRenderer == null)
            {
                Debug.LogError("Renderer 추가하셈.");
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

            // 3. 렌더러에 새로운 재질 배열을 할당하여 하이라이트를 적용합니다.
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
            {
                Destroy(outlineMaterial);
            }
        }
    }
}
