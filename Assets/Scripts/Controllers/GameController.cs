using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    public List<EnemyController> enemies = new List<EnemyController>();
    public float maxHearingRange = 15f;
    private LayerMask enemyLayer;
    private int environmentLayerInt;
    public bool isPaused;
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
        }
    }

    // Initializes audio settings, game starts unpaused with overlay hidden, locks and hides mouse cursor
    private void Start()
    {
        enemyLayer = LayerMask.GetMask("Enemy");
        environmentLayerInt = LayerMask.NameToLayer("Environment");
        AudioUtility.Initialize(enemyLayer, environmentLayerInt, maxHearingRange);

        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        isPaused = false;
        pauseOverlay.SetActive(isPaused);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Listens for the pause button to be pressed, pauses or unpauses depending on state
    void Update()
    {
        if (InputController.Instance.GetPauseDown()) {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            pauseOverlay.SetActive(isPaused);

            if (isPaused) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } 
            else {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    // Adds enemy to list of current enemies in scene
    public void AddEnemy(EnemyController enemy)
    {
        if (enemy != null && !enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    // Removes enemy from list of current enemies in scene
    public void RemoveEnemy(EnemyController enemy)
    {
        if (enemy != null && enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }
}
