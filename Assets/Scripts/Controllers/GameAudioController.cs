using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    [SerializeField] private LayerMask enemies;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SoundProduced(Sound sound)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sound.radius, enemies);
        foreach (Collider collider in hitColliders) {
            
        }
    }
}
