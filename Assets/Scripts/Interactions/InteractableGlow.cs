using UnityEngine;

[RequireComponent(typeof(Outline))]
public class InteractableGlow : MonoBehaviour
{
    public float viewAngle = 35f; // angle that the player has to be looking for glow to activate

    public Outline outline; // outline settings
    public Color glowColor = Color.green;
    public float glowWidth = 4f;

    private Camera mainCamera; // camera and player interaction necessary to only activate glow when player is near
    private ObjectInteraction playerInteraction;

    private bool isGlowing = false;

    // initializes glow, glow is initially off
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

    // updates glow if state changes
    private void SetGlow(bool state)
    {
        if (isGlowing == state) return; 

        isGlowing = state;
        outline.enabled = state;
    }

    // calculates distance to object and viewing angle. if both are within range, turns on glow.
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