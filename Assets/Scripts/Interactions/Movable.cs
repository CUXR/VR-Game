using UnityEngine;

public class Movable : MonoBehaviour, InteractableInterface
{
    private float radiusToDraw;
    private float soundTimer = 0f;
    private float soundRadiusDuration = 2f;

    [Header("Sound Variables")]
    private float objectVolumeRadius = 7f;
    private float objectVolumeDecay = 0.4f;
    private float objectLoudness = 0.3f;

    public void Interact()
    {
       
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radiusToDraw);
    }

}
