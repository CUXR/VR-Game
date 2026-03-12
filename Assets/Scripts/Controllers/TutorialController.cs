using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance; 
    
    public TextMeshProUGUI uiTextElement; 

    private Dictionary<string, TutorialItem> tutorialDict = new Dictionary<string, TutorialItem>();
    private TutorialItem currentItem = null;

    void Awake()
    {
        if (Instance == null) Instance = this; 
        
        LoadFromJSON();
        uiTextElement.text = "";
    }

    void LoadFromJSON()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("tutorial");
    }

    public void ShowItem(string itemId)
    {
        if (!tutorialDict.ContainsKey(itemId)) {
            return;
        }

        TutorialItem newItem = tutorialDict[itemId];

        if (currentItem != null && newItem.priority < currentItem.priority)
        {
            return; // current item takes precedence until new item has higher priority
        }

        currentItem = newItem;
        uiTextElement.text = newItem.text;
    }

}