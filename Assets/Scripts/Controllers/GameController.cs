using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private LayerMask enemyLayer;
    private int environmentLayerInt;
    public float maxHearingRange = 15f;
    public static GameController Instance;
    public List<EnemyController> enemies = new List<EnemyController>();
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
