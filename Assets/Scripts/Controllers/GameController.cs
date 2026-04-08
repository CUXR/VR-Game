using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    public List<EnemyController> enemies = new List<EnemyController>();
    public float maxHearingRange = 15f;
    private LayerMask enemyLayer;
    private int environmentLayerInt;
    private bool isPaused;
    public GameObject pauseOverlay;

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

    private void Start()
    {
        enemyLayer = LayerMask.GetMask("Enemy");
        environmentLayerInt = LayerMask.NameToLayer("Environment");
        AudioUtility.Initialize(enemyLayer, environmentLayerInt, maxHearingRange);

        Application.targetFrameRate = 60;
        isPaused = false;
        pauseOverlay.SetActive(isPaused);
    }

    // Update is called once per frame
    void Update()
    {
        if (InputController.Instance.GetPauseDown()) {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            pauseOverlay.SetActive(isPaused);
        }
    }

    public void AddEnemy(EnemyController enemy)
    {
        if (enemy != null && !enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        if (enemy != null && enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }
}
