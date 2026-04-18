// data containers

using System;

// each tutorial is defined by its id, text, and priority value
[Serializable]
public class TutorialItem {
    public string id;
    public string text;
    public int priority;
}

// tutorialdata is a list of all the tutorials
// this is needed because JsonUtility can't parse a top-level JSON array
[Serializable]
public class TutorialData {
    public TutorialItem[] tutorials;
}