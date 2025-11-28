using UnityEngine;

public class Yuki_SkillAttack : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem ps;

    private void Awake()
    {
        // ������ ���� ��ƼŬ �ڵ� Ž��
        ps = GetComponentInChildren<ParticleSystem>();
        ps.Stop();
    }

    // �ܺ�(��ų �ڵ�/�ִϸ��̼�)���� ȣ���� �Լ�
    public void PlayEffect()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }
}
