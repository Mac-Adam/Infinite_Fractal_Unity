using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownControler : MonoBehaviour
{
    TMPro.TMP_Dropdown dropdown;
    TMPro.TMP_Text text;

    void Awake()
    {
        dropdown = GetComponentInChildren<TMPro.TMP_Dropdown>();
        text = GetComponentInChildren<TMPro.TMP_Text>();
    }
    public void SetOptions(List<string> options) 
    {
        foreach(string option in options)
        {
            dropdown.options.Add(new TMPro.TMP_Dropdown.OptionData() { text = option });
        }
    }
    public void SetText(string newText)
    {
        text.text = newText;
    }
    public void SetValue(int newValue)
    {
        dropdown.value = newValue;
    }
    public void AddListner(Action<int> callback)
    {
        dropdown.onValueChanged.AddListener(delegate
        {
            callback(dropdown.value);
        });
    }
}
