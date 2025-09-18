using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    private List<EnemyController> enemies;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
    }

    public void SoundProduced(Sound sound)
    {
        if (enemies != null)
        {
            foreach (EnemyController enemy in enemies)
            {
                float muffling = 1f;
                float distance = Vector3.Distance(enemy.transform.position, sound.position);
                if (Overlap(enemy.hearing.GetRange(), sound.radius, distance))
                {
                    Vector3 direction = (enemy.transform.position - sound.position).normalized;
                    if (Physics.Raycast(sound.position, direction, out RaycastHit hit, distance))
                    {
                        if (hit.collider.gameObject.layer == 8)
                        {
                            // Index 8 refers to the "Environment" layer, change the index if the corresponding layer
                            // is moved to a different index
                            muffling = 0.5f;
                        }
                    }
                    float soundVolume = sound.loudness * muffling * Mathf.Exp(-sound.decayRate * distance);
                    if (soundVolume >= enemy.hearing.GetThreshold())
                    {
                        enemy.hearing.HeardSound(sound.position);
                    }
                }
            }
        }
    }

    private bool Overlap(float firstRadius, float secondRadius, float distance)
    {
        return distance <= (firstRadius + secondRadius);
    }

    public void EnemyChanged()
    {
        enemies = GameController.Instance.enemies;
    }
}
