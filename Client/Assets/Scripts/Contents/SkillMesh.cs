using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.UIElements;
using static UI_PlayerInterface;
using static UnityEngine.Rendering.DebugUI;

public class SkillMesh : MonoBehaviour
{
    SkillHitbox _hitbox = new SkillHitbox();
    Transform _playerTransform = null;

    public float ChargeRatio { get; set; } = 1;

    private GameObject visualObject;

    public void Init(SkillHitbox hitbox, Transform playerTransform, int team = 0, float chargeRatio = 1) // 투사체일 경우엔 투사체의 transform을 넣어줘야함
    {
        _hitbox = hitbox;
        _playerTransform = playerTransform;
        ChargeRatio = chargeRatio;

        Enum.TryParse<SkillShape>(_hitbox.Shape, out SkillShape shape);
        CreateVisual(shape, team);

        if (_hitbox.Duration > 0f)
            StartCoroutine(AutoDestroy(_hitbox.Duration));
    }

    private void CreateVisual(SkillShape shape, int team)
    {
        if (visualObject != null)
            Destroy(visualObject);

        visualObject = new GameObject(shape.ToString() + "Visual");
        visualObject.transform.SetParent(transform);

        var lr = visualObject.AddComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = 0.05f;
        Material originalMat = Resources.Load<Material>("Prefabs/Debug/hitbox");
        lr.material = Instantiate(originalMat);

        UnityEngine.Color color;
        switch (team)
        {
            case 0:
                color = new UnityEngine.Color(1, 0.92f, 0.016f, 1);
                break;
            case 1:
                color = new UnityEngine.Color(1, 0, 1, 1);
                break;
            case 2:
                color = new UnityEngine.Color(0, 0, 1, 1);
                break;
            default:
                color = new UnityEngine.Color(0, 1, 0, 1);
                break;
        }
        lr.startColor = lr.endColor = color;
        lr.useWorldSpace = false;

        Enum.TryParse<SkillType>(_hitbox.Type, out SkillType type);
        if (type == SkillType.SkillTrack || type == SkillType.SkillProjectile) 
            transform.SetParent(_playerTransform);
        else
        {
            visualObject.transform.position = _playerTransform.position;
            visualObject.transform.rotation = _playerTransform.rotation;
        }

        switch (shape)
        {
            case SkillShape.Circle:
                visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
                DrawCircle(lr, _hitbox.Radius, 36);
                break;

            case SkillShape.Rectangle:                
                visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
                DrawRectangle(lr, _hitbox.Width, _hitbox.Height); 
                break;

            case SkillShape.Ray:
                float range = Mathf.Lerp(_hitbox.MinRange, _hitbox.MaxRange, ChargeRatio);
                visualObject.transform.localPosition = new Vector3(0, 0, range * 0.5f); 
                DrawRectangle(lr, _hitbox.Width, range);
                break;

            case SkillShape.Sector:
                visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
                DrawSector(lr, _hitbox.Radius, _hitbox.Angle);
                break;

            case SkillShape.ShapeNone:
            default:
                Destroy(visualObject);
                visualObject = null;
                break;
        }
    }

    private void DrawCircle(LineRenderer lr, float radius, int segments)
    {
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
        }
    }

    private void DrawRectangle(LineRenderer lr, float width, float height)
    {
        lr.positionCount = 5;
        Vector3[] points = new Vector3[5]
        {
        new Vector3(-width/2f, 0, -height/2f), // X, Y=0, Z
        new Vector3(-width/2f, 0, height/2f),
        new Vector3(width/2f, 0, height/2f),
        new Vector3(width/2f, 0, -height/2f),
        new Vector3(-width/2f, 0, -height/2f)
        };
        lr.SetPositions(points);
    }

    private void DrawSector(LineRenderer lr, float radius, float angleDegrees)
    {
        int segments = 20;
        lr.positionCount = segments + 2;
        lr.SetPosition(0, Vector3.zero); // 중심
        float startAngle = -angleDegrees / 2f;
        float step = angleDegrees / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + step * i) * Mathf.Deg2Rad;
            lr.SetPosition(i + 1, new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
        }
    }

    private IEnumerator AutoDestroy(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (visualObject != null)
            Destroy(visualObject);

        Destroy(this.gameObject);
    }
}
