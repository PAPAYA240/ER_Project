using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Highlight
{
    // 이거 쓰려면 스키닝 메시 + 아웃라인 머터리얼 + 충돌 캡슐 필요
    public class HighlightEffect : MonoBehaviour
    {
        private Renderer[] myRenderers;
        private Material outlineMaterial;
        private Material[][] originalMaterials;

        // 커서
        private Texture2D _cursorDefault;
        private Texture2D _cursorEnemy;

        void Start()
        {
            myRenderers = GetComponentsInChildren<Renderer>();
            _cursorDefault = Managers.Resource.Load<Texture2D>("Cursor/Cursor_01");
            _cursorEnemy = Managers.Resource.Load<Texture2D>("Cursor/Cursor_05");
            outlineMaterial = Resources.Load<Material>("materials/Outline/Outline");

            if (outlineMaterial == null || myRenderers == null || myRenderers.Length == 0)
            {
                Debug.LogError("아웃라인 머티리얼 또는 렌더러가 없습니다.");
                return;
            }

            originalMaterials = new Material[myRenderers.Length][];
            for (int i = 0; i < myRenderers.Length; i++)
                originalMaterials[i] = myRenderers[i].sharedMaterials;
            outlineMaterial = new Material(Shader.Find("Custom/Outline_Shader"));
        }

        void OnMouseEnter()
        {
            if (myRenderers == null) return;

            foreach (var renderer in myRenderers)
            {
                if (renderer == null) continue;
                Material[] newMaterials = renderer.materials;
                renderer.materials = newMaterials.Append(outlineMaterial).ToArray();
            }
            List<Material> newMaterials = new List<Material>(originalMaterials);

            newMaterials.Add(outlineMaterial);
            Cursor.SetCursor(_cursorEnemy, Vector2.zero, CursorMode.Auto);

            myRenderer.materials = newMaterials.ToArray();
        }

        void OnMouseExit()
        {
            if (myRenderers == null) return;

            foreach (var renderer in myRenderers)
            {
                if (renderer == null) continue;
                Material[] materialsToKeep = renderer.materials.Where(m => m != outlineMaterial).ToArray();
                for (int i = 0; i < myRenderers.Length; i++)
                {
                    if (myRenderers[i] != null)
                        myRenderers[i].materials = originalMaterials[i];
                }
            }
            myRenderer.materials = originalMaterials;
            Cursor.SetCursor(_cursorDefault, Vector2.zero, CursorMode.Auto);
        }
        void OnDestroy()
        {
            if (outlineMaterial != null)
                Destroy(outlineMaterial);
        }
    }
}
