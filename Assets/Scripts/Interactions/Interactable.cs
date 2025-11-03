public interface Interactable
{

    // This function is called by the player being in range of an interactable object and pressing the
    // "e" key (applied only if the player is not currently holding an object). The effects of the
    // interaction depend on the characteristics of the object. Takes in a collider (player collider)
    // as well as three floats that determine the offset in the player's camera view.
    public void Interact();
}

