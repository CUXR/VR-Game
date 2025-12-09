using UnityEngine;

public class Key : Collectible
{
    //how are we expected to use actions enum?
    //what is CopyCollectibleData for?
    public bool collected = false; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
