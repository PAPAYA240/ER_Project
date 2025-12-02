using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class YukiSkillRange : MonoBehaviour, IEffect
{
    private PlayerController _player;

    private Coroutine _co;
    public Image _backgroundImage;
    public Image _foregroundImage;

    void Start()
    {
        _player = GetComponentInParent<PlayerController>();

        transform.localPosition = Vector3.zero;

        _backgroundImage.type = Image.Type.Filled;
        _backgroundImage.fillMethod = Image.FillMethod.Radial360; // ����
        _backgroundImage.fillOrigin = 3; // 0: ��
        _backgroundImage.fillClockwise = true; // �ð����
        _backgroundImage.fillAmount = 0f; // ������ ����ְ�

        _foregroundImage.type = Image.Type.Filled;
        _foregroundImage.fillMethod = Image.FillMethod.Vertical;
        _foregroundImage.fillOrigin = 1; // 0: ����, 1: ������
        _foregroundImage.fillAmount = 0.5f; // 50%�� �׸���

        gameObject.SetActive(false);
    }

    public void Play()
    {
        gameObject.SetActive(true);

        if (_co != null)
            StopCoroutine(_co);

        _co = StartCoroutine(FillAndHide(_player, 1f));
    }

    private IEnumerator FillAndHide(PlayerController player, float duration)
    {
        float timer = 0f;
        _backgroundImage.fillAmount = 0.0f;

        // 0~0.5 ������ 1�� ���� ä���
        while (timer < duration)
        {
            timer += Time.deltaTime;
            _backgroundImage.fillAmount = Mathf.Clamp01(timer / duration * 0.5f);
            yield return null;
        }

        player.YukiEffects.PlayEffect(SkillEffectType.RShadow);
        player.YukiEffects.PlayEffect(SkillEffectType.RAttack);

        _backgroundImage.fillAmount = 0.5f; // Ȯ���ϰ� �ݸ� ä��

        // ��� �����ְ� �����
        yield return new WaitForSeconds(0.1f);

        gameObject.SetActive(false);
        _co = null;
    }

    public void Stop()
    {
        throw new System.NotImplementedException();
    }
}
