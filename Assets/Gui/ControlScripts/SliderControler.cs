using System;
using UnityEngine;
using UnityEngine.UI;

public class SliderControler : MonoBehaviour
{
    Slider slider;

    TMPro.TMP_Text text;

    private bool logScale = false;


    void Awake()
    {
        slider =  GetComponentInChildren<Slider>();
        text = GetComponentInChildren<TMPro.TMP_Text>();
    }
    public void SetUp(float min,float max, bool log)
    {
        logScale = log;
        if (logScale)
        {
            slider.minValue = Mathf.Log10(min);
            slider.maxValue = Mathf.Log10(max);
        }
        else
        {
            slider.minValue = min;
            slider.maxValue = max;

        }


    }


    public void SetText(string newText)
    {
        text.text = newText;
    }
    public void SetValue(float newValue)
    {
        if (logScale)
        {
            slider.value = Mathf.Log10(newValue);
        }
        else
        {
            slider.value = newValue;
        }
        
    }
    public void AddListner(Action<float> callback)
    {
        if (logScale)
        {
            slider.onValueChanged.AddListener(delegate
            {
                callback(Mathf.Pow(10, slider.value));
            });
        }
        else
        {
            slider.onValueChanged.AddListener(delegate
            {
                callback(slider.value);
            });
        }

    }

}
