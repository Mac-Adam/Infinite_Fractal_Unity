using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GuiTemplates;

public class UIControler : MonoBehaviour
{
    public GameObject backgroundPrefab;
    public GameObject buttonPrefab;
    public GameObject DropdownPrefab;
    public GameObject TogglePrefab;
    public GameObject SliderPrefab;

    private GameObject background;
    private List<GameObject> toggleList = new();
    private List<GameObject> sliderList = new();
    private List <GameObject> dropdownList = new();

    private float currentY = 0;

    void Start()
    {
        GenerateUI(new UITemplate(
            new Sizes(300,10, new Vector2(250, 40), new Vector2(250,50),new Vector2(250,70) ),
            new List<ToggleTemplate>()
            {
                new ToggleTemplate(
                "On", true, (bool b) => {
                    Debug.Log($"fist one is now {b}");

                }

                ), new ToggleTemplate(
                "Off", false, (bool b) => {
                    Debug.Log($"second one is now {b}");

                }

                )

            },
            new List<SliderTemplate>() {
                new SliderTemplate(
                    "Non log",
                    0.4f,
                    0,1,
                    false,
                    (float f) =>
                    {
                        Debug.Log($"Non log is now {f}");
                    }

                    ),
                new SliderTemplate(
                    "log",
                    100,
                    1,100000,
                    true,
                    (float f) =>
                    {
                        Debug.Log($"log is now {f}");
                    }

                    )
            },
            new List<DropdownTemplate>()
            {
                new DropdownTemplate("test",1,
                
                new List<string>(){"zero","raz", "dwa", "trzy" },
                
                (int i)=>{
                    Debug.Log($"you chose {i}");
                })
            }


            )); ;
    }

    public void UpdateUI(List<bool> toggleUpdates,List<float> sliderUpdates,List<int> dropdownUpdates)
    {
        for(int i = 0; i < toggleUpdates.Count; i++)
        {
            toggleList[i].GetComponent<ToggleControler>().SetValue(toggleUpdates[i]);
        }
        for (int i = 0; i < sliderUpdates.Count; i++)
        {
            sliderList[i].GetComponent<SliderControler>().SetValue(sliderUpdates[i]);
        }
        for (int i = 0; i < dropdownUpdates.Count; i++)
        {
            dropdownList[i].GetComponent<DropdownControler>().SetValue(dropdownUpdates[i]);
        }

    }
  


    void GenerateUI(UITemplate template)
    {
        background = GenerateComponentFromPrefab(backgroundPrefab, transform, new Vector2(template.sizes.width,0),Vector2.zero,template.sizes.margin);
        foreach(ToggleTemplate toggleTemplate in template.toggleTemplates)
        {
            GameObject newToggle = GenerateComponentFromPrefab(TogglePrefab, background.transform, template.sizes.toggleSize, new Vector2(0, currentY), template.sizes.margin);

            ToggleControler toggleConroler = newToggle.GetComponent<ToggleControler>();
            toggleConroler.SetText(toggleTemplate.text);
            toggleConroler.SetValue(toggleTemplate.startingValue);
            toggleConroler.AddListner(toggleTemplate.callback);

            toggleList.Add(newToggle);

        }
        foreach (SliderTemplate sliderTemplate in template.sliderTemplates)
        {
            GameObject newSlider = GenerateComponentFromPrefab(SliderPrefab, background.transform, template.sizes.sliderSize, new Vector2(0, currentY), template.sizes.margin);

            SliderControler sliderControler = newSlider.GetComponent<SliderControler>();
            sliderControler.SetUp(sliderTemplate.min, sliderTemplate.max, sliderTemplate.log);
            sliderControler.SetText(sliderTemplate.text);
            sliderControler.SetValue(sliderTemplate.startingValue);
            sliderControler.AddListner(sliderTemplate.callback);

            sliderList.Add(newSlider);

        }
        foreach (DropdownTemplate dropdownTemplate in template.dropdownTemplates)
        {
            GameObject newDropdown = GenerateComponentFromPrefab(DropdownPrefab, background.transform, template.sizes.dropdownSize, new Vector2(0, currentY), template.sizes.margin);

            DropdownControler dropdownControler = newDropdown.GetComponent<DropdownControler>();
            dropdownControler.SetOptions(dropdownTemplate.options);
            dropdownControler.SetText(dropdownTemplate.text);
            dropdownControler.SetValue(dropdownTemplate.startingValue);
            dropdownControler.AddListner(dropdownTemplate.callback);

            dropdownList.Add(newDropdown);

        }




    }


    GameObject GenerateComponentFromPrefab(GameObject prefab,Transform parent,Vector2 size,Vector2 pos,float margin )
    {
        GameObject component = Instantiate(prefab);
        component.transform.SetParent(parent);
        RectTransform componentRectTransform = component.GetComponent<RectTransform>();
        componentRectTransform.anchoredPosition = pos;
        componentRectTransform.sizeDelta = size;

        currentY -= size.y + margin;

        return component;



    }
}
