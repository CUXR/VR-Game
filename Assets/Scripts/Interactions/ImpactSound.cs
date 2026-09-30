using UnityEngine;

public class ImpactSound : MonoBehaviour
{
    public AudioSource dropSound;

    void OnCollisionEnter(Collision collision)
    {
        if (dropSound != null && collision.relativeVelocity.magnitude > 1f && !dropSound.isPlaying)
        {
            dropSound.Play();
        }
    }
}