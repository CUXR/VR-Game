using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance; 
    
    public TextMeshProUGUI uiTextElement; 

    // maps text IDs to hint; each tutorial item has an id, text hint, and priority integer
    private Dictionary<string, TutorialItem> tutorialDict = new Dictionary<string, TutorialItem>();
    private TutorialItem currentItem = null;

    // clears any placeholder text in the ui text element
    void Awake()
    {
        if (Instance == null) Instance = this; 
        
        LoadFromJSON();
        if (uiTextElement!=null)
        {
            uiTextElement.text = "";
        }
    }

    // reads tutorial.json from the resources folder and parses into the dictionary
    void LoadFromJSON()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("tutorial");
        if (jsonText != null)
        {
            TutorialData db = JsonUtility.FromJson<TutorialData>(jsonText.text);
            foreach (var tutorial in db.tutorials)
            {
                tutorialDict.Add(tutorial.id, tutorial);
            }
        }
    }

    // checks the id inputted exists, and only displays it if it has an equal ot higher priority than the current hint
    public void ShowText(string itemId)
    {
        if (!tutorialDict.ContainsKey(itemId)) {
            Debug.Log("i can't find item with id " + itemId);
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

    // hides the hint with the id inputted (does nothing if the id inputted is not the current hint's id)
    public void HideText(string itemId)
    {
        if (currentItem != null && currentItem.id == itemId)
        {
            if (uiTextElement!=null)
            {
                uiTextElement.text = "";
            currentItem = null;
            }
        }
    }

}