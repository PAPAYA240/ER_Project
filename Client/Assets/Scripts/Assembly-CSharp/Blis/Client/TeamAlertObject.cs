using UnityEngine;

namespace Blis.Client
{
	public class TeamAlertObject : MonoBehaviour
	{
		[SerializeField]
		private int teamNumber;

		[SerializeField]
		private string emissionColorKey;

		[SerializeField]
		private Color allyEmissionColor;

		[SerializeField]
		private float allyEmissionIntensity;

		[SerializeField]
		private Color enemyEmissionColor;

		[SerializeField]
		private float enemyEmissionIntensity;

		private MeshRenderer[] meshRenderer;

		private SkinnedMeshRenderer[] skinnedMeshRenderer;

		private int EmissionColor;

		private (Color, float) emissionColorEnemy;

		private (Color, float) emissionColorAlly;

		private readonly (Color, float) emissionDeActiveColor;

		public void Awake()
		{
		}

		public void ActiveObject(int playerTeamNumber)
		{
		}

		public void DeActiveObject()
		{
		}

		private void SetColor((Color, float) colorValue)
		{
		}

		public void SetTeamNumber(int playerTeamNumber)
		{
		}
	}
}
