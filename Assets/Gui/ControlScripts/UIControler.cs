using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GuiTemplates;

public class UIControler : MonoBehaviour
{
    public GameObject BackgroundPrefab;
    public GameObject ButtonPrefab;
    public GameObject DropdownPrefab;
    public GameObject TogglePrefab;
    public GameObject SliderPrefab;
    public GameObject ProgressBarPrefab;
    public GameObject TextPrefab;
    public GameObject SlideVievPrefab;



    private GameObject backgroundTop;
    private GameObject backgroundBot;
    private GameObject slideViev;
    private List<GameObject> toggleList = new();
    private List<GameObject> sliderList = new();
    private List <GameObject> dropdownList = new();
    private List<GameObject> progressBarList = new();
    private List<GameObject> buttonList = new();
    private List<GameObject> textList = new();

    private float currentYTop = 0;
    private float currentYBottom = 30;


    public void UpdateUI(List<bool> toggleUpdates,List<float> sliderUpdates,List<int> dropdownUpdates,List<float> progressBarUpdates,List<string> textUpdates)
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
        for (int i = 0; i < progressBarUpdates.Count; i++)
        {
            progressBarList[i].GetComponent<ProgresBarContorler>().SetProgres(progressBarUpdates[i]);
        }
        for (int i = 0; i < textUpdates.Count; i++)
        {
            textList[i].GetComponent<TextControler>().SetText(textUpdates[i]);
        }
    }
    public void SetEnable(bool val)
    {
        gameObject.SetActive(val);
    }


    public void GenerateUI(UITemplate template)
    {

        backgroundBot = GenerateComponentFromPrefab(BackgroundPrefab, transform, new Vector2(template.sizes.width, 0), Vector2.zero, template.sizes.margin, true);
        backgroundTop = GenerateComponentFromPrefab(BackgroundPrefab, transform, new Vector2(template.sizes.width,0),Vector2.zero,template.sizes.margin,true);
        Image im = backgroundTop.GetComponent<Image>();
        im.enabled = false;

        slideViev = GenerateComponentFromPrefab(SlideVievPrefab, transform, new Vector2(template.sizes.width, 300), Vector2.zero, template.sizes.margin, true,false);
        foreach(ToggleTemplate toggleTemplate in template.toggleTemplates)
        {
            GameObject newToggle = GenerateComponentFromPrefab(TogglePrefab, backgroundTop.transform, template.sizes.toggleSize, new Vector2(0, currentYTop), template.sizes.margin, true);

            ToggleControler toggleConroler = newToggle.GetComponent<ToggleControler>();
            toggleConroler.SetText(toggleTemplate.text);
            toggleConroler.SetValue(toggleTemplate.startingValue);
            toggleConroler.AddListner(toggleTemplate.callback);

            toggleList.Add(newToggle);

        }
        foreach (SliderTemplate sliderTemplate in template.sliderTemplates)
        {
            GameObject newSlider = GenerateComponentFromPrefab(SliderPrefab, backgroundTop.transform, template.sizes.sliderSize, new Vector2(0, currentYTop), template.sizes.margin, true);

            SliderControler sliderControler = newSlider.GetComponent<SliderControler>();
            sliderControler.SetUp(sliderTemplate.min, sliderTemplate.max, sliderTemplate.log);
            sliderControler.SetText(sliderTemplate.text);
            sliderControler.SetValue(sliderTemplate.startingValue);
            sliderControler.AddListner(sliderTemplate.callback);

            sliderList.Add(newSlider);

        }
        foreach (DropdownTemplate dropdownTemplate in template.dropdownTemplates)
        {
            GameObject newDropdown = GenerateComponentFromPrefab(DropdownPrefab, backgroundTop.transform, template.sizes.dropdownSize, new Vector2(0, currentYTop), template.sizes.margin,true);

            DropdownControler dropdownControler = newDropdown.GetComponent<DropdownControler>();
            dropdownControler.SetOptions(dropdownTemplate.options);
            dropdownControler.SetText(dropdownTemplate.text);
            dropdownControler.SetValue(dropdownTemplate.startingValue);
            dropdownControler.AddListner(dropdownTemplate.callback);

            dropdownList.Add(newDropdown);

        }
        foreach (ProgressBarTemplate progressBarTemplate in template.progressBarTemplates)
        {
            GameObject newProgessBar = GenerateComponentFromPrefab(ProgressBarPrefab, backgroundTop.transform, template.sizes.progressBarSize, new Vector2(0, currentYTop), template.sizes.margin,true);
            ProgresBarContorler progressBarControler = newProgessBar.GetComponent<ProgresBarContorler>();

            progressBarControler.SetText(progressBarTemplate.text);
            progressBarControler.SetProgres(progressBarTemplate.startingValue);
           
            progressBarList.Add(newProgessBar);

        }
        foreach (ButtonTemplate buttonTemplate in template.buttonTemplaes)
        {
            GameObject newButton = GenerateComponentFromPrefab(ButtonPrefab, backgroundBot.transform, template.sizes.buttonSize, new Vector2(0, currentYBottom), template.sizes.margin, false);
            ButtonControler buttonControler = newButton.GetComponent<ButtonControler>();

            buttonControler.SetText(buttonTemplate.text);
            buttonControler.AddListner(buttonTemplate.callback);

            buttonList.Add(newButton);

        }
        foreach (TextTemplate textTemplate in template.textTemplates)
        {
            GameObject newText = GenerateComponentFromPrefab(TextPrefab, backgroundBot.transform, template.sizes.textSize, new Vector2(0, currentYBottom), template.sizes.margin, false);
            TextControler textControler = newText.GetComponent<TextControler>();

            textControler.SetText(textTemplate.text);
            
            textList.Add(newText);

        }


    }


    GameObject GenerateComponentFromPrefab(GameObject prefab,Transform parent,Vector2 size,Vector2 pos,float margin,bool top,bool accountForSize = true )
    {
        GameObject component = Instantiate(prefab);
        component.transform.SetParent(parent);
        RectTransform componentRectTransform = component.GetComponent<RectTransform>();
        componentRectTransform.anchoredPosition = pos;
        componentRectTransform.sizeDelta = size;
        if (accountForSize)
        {
            if (top)
            {
                currentYTop -= size.y + margin;
            }
            else
            {
                currentYBottom += size.y + margin;
            }
        }
    

        return component;



    }
}
