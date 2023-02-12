using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleControler : MonoBehaviour
{
    Toggle toggle;
    public enum ValueToToggle
    {
        Precision,
        Antialiasing,
        SmoothGradient
    }
    public ValueToToggle valToToggle;
    public FractalMaster GameMasterComponent;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(delegate
        {
            ToggleValueChanged(toggle);
        });
    }
    void ToggleValueChanged(Toggle change)
    {
        switch (valToToggle)
        {
            case ValueToToggle.Precision:
                GameMasterComponent.SetPrecision(change.isOn);
                break;
            case ValueToToggle.Antialiasing:
                GameMasterComponent.SetAnitialiasing(change.isOn);
                break;
            case ValueToToggle.SmoothGradient:
                GameMasterComponent.SetSmoothGradient(change.isOn);
                break;

        }
        
    }

}
