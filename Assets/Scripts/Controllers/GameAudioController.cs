using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    private List<EnemyController> enemies;
    private float soundVolume;
    private float distance;
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
        enemies = GameController.Instance.enemies;
        foreach (EnemyController enemy in enemies)
        {
            if (Overlap(enemy.transform.position, enemy.hearing.GetRange(), sound.position, sound.radius))
            {
                soundVolume = sound.loudness * Mathf.Exp(sound.decayRate * distance);
                if (soundVolume >= enemy.hearing.)
            }
        }
    }

    private bool Overlap(Vector3 firstCenter, float firstRadius, Vector3 secondCenter, float secondRadius) {
        distance = Vector3.Distance(firstCenter, secondCenter);
        if (distance > (firstRadius + secondRadius))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
