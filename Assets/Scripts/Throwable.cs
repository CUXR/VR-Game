using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwable : MonoBehaviour
{

    public GameObject player;
    //follows the camera instead of the player when being held
    public Transform holdPos;
    public float throwForce;
    private GameObject heldObj;
    private Rigidbody heldObjRb;
    private int layerNumber; //layer index for hold layer

    // Start is called before the first frame update
    void Start()
    {
        layerNumber = LayerMask.NameToLayer("Throwable");
        heldObj = this.gameObject;
        heldObjRb = GetComponent<Rigidbody>();
        throwForce = 15.0f;
    }

    // Update is called once per frame
    void Update()
    {
        //While the hold key is being pressed the object is being held
        if (Input.GetKeyDown(KeyCode.E))
        {
            PickUpObject();
        }
        //If the hold key is let go then the object drops
        if (Input.GetKeyUp(KeyCode.E))
        {
            DropObject();
        }
        //When you're holding the object, you can throw it
         if (Input.GetKeyDown(KeyCode.Mouse0) && heldObjRb.isKinematic)
        {
            ThrowObject();
        }
    
    }
    //Placeholder functions for picking up and dropping 
    //because object can clip through while being held
    void PickUpObject()
    {
        heldObjRb.isKinematic = true;
        heldObjRb.transform.parent = holdPos.transform;
        heldObj.layer = layerNumber;
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        
    }
    void DropObject()
    {
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), false);
        heldObj.layer = 0;
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;
    }

    void ThrowObject()
    {
        DropObject();
        heldObjRb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);
    }
}
