using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleControler : MonoBehaviour
{
    Toggle toggle;
    Text text;
    

    void Awake()
    {  
        toggle = GetComponent<Toggle>();
        text = GetComponentInChildren<Text>();
    }
   

    public void SetText(string newText)
    {
        text.text = newText;
    }
    public void SetValue(bool newValue)
    {
        toggle.isOn = newValue;
    }
    public void AddListner(Action<bool> callback)
    {
        toggle.onValueChanged.AddListener(delegate
        {
            callback(toggle.isOn);
        });
    }


}
