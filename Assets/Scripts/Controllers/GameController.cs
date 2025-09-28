using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private LayerMask enemyLayer;
    private int environmentLayerInt;
    // Universal maximum hearing range for all enemies in the game
    public float maxHearingRange = 12f;
    public static GameController Instance;
    public List<EnemyController> enemies = new List<EnemyController>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        enemyLayer = LayerMask.GetMask("Enemy");
        environmentLayerInt = LayerMask.NameToLayer("Environment");
        AudioUtility.Initialize(enemyLayer, environmentLayerInt, maxHearingRange);
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        enemies.Remove(enemy);
    }

    public void AddEnemy(EnemyController enemy)
    {
        enemies.Add(enemy);
    }

}
