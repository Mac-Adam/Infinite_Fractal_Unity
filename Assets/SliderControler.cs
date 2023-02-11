using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderControler : MonoBehaviour
{
    Slider slider;
 
    public FractalMaster GameMasterComponent;
    public enum ValueToControl
    {
        ColorStrenght,
        MaxIter
    }
    public ValueToControl sliderVal;
    void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate
        {
            SliderValueChanged(slider);
        });
    }
    void SliderValueChanged(Slider change)
    {
        switch (sliderVal)
        {
            case ValueToControl.ColorStrenght:
                GameMasterComponent.SetColorStrenght(Mathf.Pow(10,change.value));
                break;
            case ValueToControl.MaxIter:
                GameMasterComponent.SetMaxIter((int)Mathf.Pow(10, change.value));
                break;

        }
        

    }

}
