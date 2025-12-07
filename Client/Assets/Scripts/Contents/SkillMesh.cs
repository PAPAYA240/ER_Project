using System;
using System.Collections;
using Google.Protobuf.Protocol;
using UnityEngine;

public class SkillMesh : MonoBehaviour
{
    //public SkillHitbox _hitbox = new SkillHitbox();
    //public GameObject visualObject;
    //private Transform _playerTransform = null;
    //public int segments = 32;

    //// +추가
    //public float ChargeRatio { get; set; } = 1;

    //private Vector3 _mousePos = new Vector3();

    //public void Init(SkillHitbox hitbox, Transform playerTransform, int team = 0, float chargeRatio = 1, Vector3 mousePos = new Vector3()) // 투사체일 경우엔 투사체의 transform을 넣어줘야함
    //{
    //    _hitbox = hitbox;
    //    _playerTransform = playerTransform;
    //    ChargeRatio = chargeRatio;
    //    _mousePos = mousePos;

    //    float startTime = (float)_hitbox.StartFrame / _hitbox.Fps;
    //    float endTime = (float)_hitbox.EndFrame / _hitbox.Fps;

    //    StartCoroutine(AutoDestroy(startTime, endTime, team));
    //}

    //private void CreateVisual(SkillShape shape, int team)
    //{
    //    if (visualObject != null)
    //        Destroy(visualObject);

    //    visualObject = new GameObject(shape.ToString() + "Visual");
    //    visualObject.transform.SetParent(transform);

    //    var lr = visualObject.AddComponent<LineRenderer>();
    //    lr.startWidth = lr.endWidth = 0.05f;
    //    Material originalMat = Resources.Load<Material>("Prefabs/Debug/hitbox");
    //    lr.material = Instantiate(originalMat);

    //    UnityEngine.Color color;
    //    switch (team)
    //    {
    //        case 0:
    //            color = new UnityEngine.Color(1, 0.92f, 0.016f, 1);
    //            break;
    //        case 1:
    //            color = new UnityEngine.Color(1, 0, 1, 1);
    //            break;
    //        case 2:
    //            color = new UnityEngine.Color(0, 0, 1, 1);
    //            break;
    //        default:
    //            color = new UnityEngine.Color(0, 1, 0, 1);
    //            break;
    //    }
    //    lr.startColor = lr.endColor = color;
    //    lr.useWorldSpace = false;

    //    Enum.TryParse<SkillType>(_hitbox.Type, out SkillType type);
    //    if (type == SkillType.SkillTrack || type == SkillType.SkillProjectile)
    //    {
    //        transform.SetParent(_playerTransform);
    //        visualObject.transform.localPosition = Vector3.zero;
    //        visualObject.transform.localRotation = Quaternion.identity;
    //    }
    //    else if (type == SkillType.SkillPoint)
    //    {
    //        transform.SetParent(null);
    //        transform.position = _mousePos;
    //        transform.rotation = _playerTransform.rotation;

    //        visualObject.transform.localPosition = Vector3.zero;
    //        visualObject.transform.localRotation = Quaternion.identity;
    //    }
    //    else
    //    {
    //        visualObject.transform.position = _playerTransform.position;
    //        visualObject.transform.rotation = _playerTransform.rotation;
    //    }

    //    switch (shape)
    //    {
    //        case SkillShape.Circle:
    //            visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
    //            DrawCircle(lr, _hitbox.Radius, 36);
    //            break;

    //        case SkillShape.Point:
    //            visualObject.transform.localPosition = Vector3.zero;
    //            DrawRectangle(lr, _hitbox.Width, _hitbox.Height);
    //            break;

    //        case SkillShape.Rectangle:
    //            visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
    //            DrawRectangle(lr, _hitbox.Width, _hitbox.Height);
    //            break;

    //        case SkillShape.Ray:
    //            float range = Mathf.Lerp(_hitbox.MinRange, _hitbox.MaxRange, ChargeRatio);
    //            visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, range * 0.5f + _hitbox.LookOffset);
    //            DrawRectangle(lr, _hitbox.Width, range);
    //            break;

    //        case SkillShape.Sector:
    //            visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
    //            DrawSector(lr, _hitbox.Radius, _hitbox.Angle);
    //            break;
    //        case SkillShape.ShapeNone:
    //        default:
    //            Destroy(visualObject);
    //            visualObject = null;
    //            break;
    //    }
    //}

    //private void DrawCircle(LineRenderer lr, float radius, int segments)
    //{
    //    lr.positionCount = segments + 1;
    //    for (int i = 0; i <= segments; i++)
    //    {
    //        float angle = i * 2f * Mathf.PI / segments;
    //        lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
    //    }
    //}

    //private void DrawRectangle(LineRenderer lr, float width, float height)
    //{
    //    lr.positionCount = 5;
    //    Vector3[] points = new Vector3[5]
    //    {
    //    new Vector3(-width/2f, 0, -height/2f), // X, Y=0, Z
    //    new Vector3(-width/2f, 0, height/2f),
    //    new Vector3(width/2f, 0, height/2f),
    //    new Vector3(width/2f, 0, -height/2f),
    //    new Vector3(-width/2f, 0, -height/2f)
    //    };
    //    lr.SetPositions(points);
    //}

    //private void DrawSector(LineRenderer lr, float radius, float angleDegrees)
    //{
    //    int segments = 20;
    //    lr.positionCount = segments + 3;

    //    lr.SetPosition(0, Vector3.zero); // 중심 (로컬)

    //    float startAngle = -angleDegrees / 2f;
    //    float step = angleDegrees / segments;

    //    for (int i = 0; i <= segments; i++)
    //    {
    //        float angle = (startAngle + step * i) * Mathf.Deg2Rad;

    //        // Z축(앞) 기준으로 반원 그리기
    //        Vector3 localPos = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);

    //        lr.SetPosition(i + 1, localPos);
    //    }

    //    lr.SetPosition(segments + 2, Vector3.zero);
    //}

    //private IEnumerator AutoDestroy(float startTime, float endTime, int team)
    //{
    //    yield return new WaitForSeconds(startTime);

    //    if (Enum.TryParse<SkillShape>(_hitbox.Shape, out SkillShape shape))
    //        CreateVisual(shape, team);

    //    float duration = endTime - startTime;
    //    if (duration > 0f)
    //        yield return new WaitForSeconds(duration);

    //    if (visualObject != null)
    //        Destroy(visualObject);

    //    Destroy(this.gameObject);
    //}
}

