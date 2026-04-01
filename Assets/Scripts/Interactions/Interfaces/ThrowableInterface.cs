public interface ThrowableInterface : InteractableInterface
{

    // This function is called when the player is holding an object and presses the "e" key. The
    // object is effectively released from the player's influence.
    public void Release();

    // This function is called when the player is holding an object and left clicks the mouse. It
    // sends the object towards the direction the player is facing with a certain amount of force.
    public void Throw();
}

