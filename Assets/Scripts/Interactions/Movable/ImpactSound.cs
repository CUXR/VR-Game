using UnityEngine;

public class ImpactSound : MonoBehaviour
{
    [Header("References")]
    public AudioSource audioSource;
    
    [Header("Impact Settings")]
    public float minImpactForce = 0.5f; 
    public float maxImpactForce = 8.0f; 
    
    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;

    void OnCollisionEnter(Collision collision)
    {
        if (audioSource == null || audioSource.clip == null) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > minImpactForce)
        {
            float volume = Mathf.InverseLerp(minImpactForce, maxImpactForce, impactForce);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            
            audioSource.PlayOneShot(audioSource.clip, volume);
        }
    }
}