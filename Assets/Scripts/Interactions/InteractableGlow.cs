using UnityEngine;

[RequireComponent(typeof(Outline))]
public class InteractableGlow : MonoBehaviour
{
    public float viewAngle = 35f; 

    public Outline outline;
    public Color glowColor = Color.green;
    public float glowWidth = 4f;

    private Camera mainCamera; 
    private ObjectInteraction playerInteraction;

    private bool isGlowing = false;

    void Start()
    {
        outline = GetComponent<Outline>();
        outline.OutlineColor = glowColor;
        outline.OutlineWidth = glowWidth;
        outline.enabled = false; 

        mainCamera = Camera.main;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInteraction = player.GetComponent<ObjectInteraction>();
        }
    }

    private void SetGlow(bool state)
    {
        if (isGlowing == state) return; 

        isGlowing = state;
        outline.enabled = state;
    }

    void Update()
    {
        if (mainCamera == null || playerInteraction == null) return;

        float sqrDistance = (transform.position - mainCamera.transform.position).sqrMagnitude;
        float sqrRange = playerInteraction.range * playerInteraction.range;

        if (sqrDistance <= sqrRange)
        {
            Vector3 directionToObject = (transform.position - mainCamera.transform.position).normalized;
            float angle = Vector3.Angle(mainCamera.transform.forward, directionToObject);

            if (angle <= viewAngle)
            {
                SetGlow(true);
            }
            else
            {
                SetGlow(false);
            }
        }
        else
        {
            SetGlow(false);
        }
    }
}