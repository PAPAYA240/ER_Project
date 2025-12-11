using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UI_Follower : MonoBehaviour
{
    public Transform target;            // 따라갈 대상(플레이어)
    public Vector3 worldOffset;         // 머리 위 오프셋 (예: new Vector3(0, 2f, 0))
    public float followLerp = 20f;      // 덜덜임 줄이기용 보간 속도

    private Camera _cam;
    private RectTransform _rectTransform;
    private RectTransform _canvasRect;

    void Start()
    {
        _cam = Camera.main;
        _rectTransform = GetComponentInChildren<RectTransform>();
        _canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // 1) 월드 기준 머리 위 위치
        Vector3 worldPos = target.position + worldOffset;

        // 2) 스크린 좌표로 변환
        Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);

        // 화면 뒤에 있는 경우 숨기고 싶다면 이런 체크도 가능
        if (screenPos.z < 0)
        {
            _rectTransform.gameObject.SetActive(false);
            return;
        }
        else if (!_rectTransform.gameObject.activeSelf)
        {
            _rectTransform.gameObject.SetActive(true);
        }

        // 3) Canvas 로컬 좌표로 변환
        Vector2 localPoint;
        // Canvas가 Screen Space - Overlay면 카메라는 null
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, null, out localPoint);

        Vector3 targetLocalPos = localPoint;

        // 4) 바로 세팅 대신 보간해서 부드럽게 이동
        _rectTransform.localPosition = targetLocalPos;
        //_rectTransform.localPosition = Vector3.Lerp(
        //    _rectTransform.localPosition,
        //    targetLocalPos,
        //    followLerp * Time.unscaledDeltaTime);
    }
}
