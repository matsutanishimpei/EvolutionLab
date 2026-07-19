using UnityEngine;

/// <summary>
/// 各身体パーツの障害物接触を親のSphereIndividualへ報告します。
/// </summary>
public class IndividualCollisionSensor : MonoBehaviour
{
    private SphereIndividual owner;

    public void Initialize(SphereIndividual individual)
    {
        owner = individual;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (owner != null && collision.gameObject.TryGetComponent<EvolutionObstacle>(out _))
        {
            owner.RegisterObstacleContact(Time.fixedDeltaTime);
        }
    }
}
