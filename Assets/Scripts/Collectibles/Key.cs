using UnityEngine;

public class Key : Collectible
{
    //how are we expected to use actions enum?
    //what is CopyCollectibleData for?
    public bool collected = false; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    // public override GameObject ToUIObject()
    // {
    //     GameObject uiObject = base.ToUIObject();
    //     Key key = uiObject.AddComponent<Key>();

    //     uiObject.name = itemName;
    //     CopyCollectibleData(key);

    //     return uiObject;
    // }
    
    void Start()
    {
        // itemName = "Key";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // public override void Use()
    // {
    //     collected = true;
    // }
}
