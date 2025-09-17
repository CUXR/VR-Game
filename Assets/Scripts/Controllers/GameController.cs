using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
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
    public void RemoveEnemy(EnemyController enemy)
    {
        enemies.Remove(enemy);
        AudioController.Instance.EnemyChanged();
    }

    public void AddEnemy(EnemyController enemy)
    {
        enemies.Add(enemy);
        AudioController.Instance.EnemyChanged();
    }

}
