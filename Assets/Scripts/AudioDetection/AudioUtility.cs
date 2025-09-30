using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioUtility
{
    private static LayerMask enemyLayer;
    private static int environmentLayerInt;
    private static float maxHearingRange;
    private static bool isInitialized = false;

    public static void Initialize(LayerMask enemy, int environment, float range)
    {
        if (isInitialized) return;

        enemyLayer = enemy;
        environmentLayerInt = environment;
        maxHearingRange = range;
        isInitialized = true;
    }

    public static void SoundProduced(Sound sound)
    {
        Collider[] colliders = Physics.OverlapSphere(sound.position, sound.radius + maxHearingRange, enemyLayer);
        foreach (var collider in colliders)
        {
            Debug.Log("Detected collider: " + collider.gameObject.name);
            if (collider.TryGetComponent<EnemyController>(out var enemy))
            {
                Debug.Log("Sound heard by: " + enemy.gameObject.name);
                float distance = Vector3.Distance(enemy.transform.position, sound.position);
                float enemyHearingRange = enemy.GetHearingRange();
                float muffling = 1f;
                if (distance <= sound.radius + enemyHearingRange)
                {
                    Vector3 direction = (enemy.transform.position - sound.position).normalized;
                    if (Physics.Raycast(sound.position, direction, out RaycastHit hit, distance))
                    {
                        if (hit.collider.gameObject.layer == environmentLayerInt)
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
}
