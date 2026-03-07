using UnityEngine;
using UnityEngine.UIElements;

public class DoorAnimator : MonoBehaviour
{
    public Animator anim;

    void Start()
    {
        // anim = gameObject.GetComponent(Animator);
    }
    
    public void toggleOpen()
    {
        Debug.Log("entered toggleOpen");
        if (anim.GetBool("open"))
        {
            Debug.Log("open set false");
            anim.SetBool("open", false);
        }
        else
        {
            anim.SetBool("open", true);
        }
    }
}
