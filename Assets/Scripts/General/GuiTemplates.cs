using System;
using System.Collections.Generic;
using UnityEngine;
namespace GuiTemplates
{

    struct ToggleTemplate
    {
        public string text;
        public bool startingValue;
        public Action<bool> callback;

        public ToggleTemplate(string text, bool startingValue, Action<bool> callback)
        {
            this.text = text;
            this.startingValue = startingValue;
            this.callback = callback;
        }
    }
    struct ButtonTemplae
    {
        public string text;
        public Action callback;

        public ButtonTemplae(string text, Action callback)
        {
            this.text = text;
            this.callback = callback;
        }


    }
    struct ProgressBarTemplate
    {
        public string text;
        public float startingValue;
        public ProgressBarTemplate(string text, float startingValue)
        {
            this.text = text;
            this.startingValue = startingValue;
        }
    }

    struct DropdownTemplate
    {
        public string text;
        public int startingValue;
        public List<string> options;
        public Action<int> callback;


        public DropdownTemplate(string text, int startingValue, List<string> options, Action<int> callback)
        {
            this.text = text;
            this.options = options;
            this.callback = callback;
            this.startingValue = startingValue;
        }
    }
    struct SliderTemplate
    {
        public string text;
        public float startingValue;
        public float min;
        public float max;
        public bool log;
        public Action<float> callback;

        public SliderTemplate(string text, float startingValue, float min, float max, bool log, Action<float> callback)
        {
            this.text = text;
            this.startingValue = startingValue;
            this.min = min;
            this.max = max;
            this.log = log;
            this.callback = callback;

        }
    }

    struct Sizes
    {
        public float width;
        public float margin;
        public Vector2 toggleSize;
        public Vector2 sliderSize;
        public Vector2 dropdownSize;
        public Vector2 progressBarSize;
        public Vector2 buttonSize;

        public Sizes(float width,float margin, Vector2 toggleSize, Vector2 sliderSize, Vector2 dropdownSize, Vector2 progressBarSize,Vector2 buttonSize)
        {
            this.width = width;
            this.margin = margin;
            this.toggleSize = toggleSize;
            this.sliderSize = sliderSize;
            this.dropdownSize = dropdownSize;
            this.progressBarSize = progressBarSize;
            this.buttonSize = buttonSize;
        }
    }

    struct UITemplate
    {
        public Sizes sizes;
        public List<ToggleTemplate> toggleTemplates;
        public List<SliderTemplate> sliderTemplates;
        public List<DropdownTemplate> dropdownTemplates;
        public List<ProgressBarTemplate> progressBarTemplates;
        public List<ButtonTemplae> buttonTemplaes;


        public UITemplate(Sizes sizes, List<ToggleTemplate> toggleTemplates, List<SliderTemplate> sliderTemplates, List<DropdownTemplate> dropdownTemplates, List<ProgressBarTemplate> progressBarTemplates, List<ButtonTemplae> buttonTemplaes)
        {
            this.sizes = sizes;
            this.toggleTemplates = toggleTemplates;
            this.sliderTemplates = sliderTemplates;
            this.dropdownTemplates = dropdownTemplates;
            this.progressBarTemplates = progressBarTemplates;
            this.buttonTemplaes = buttonTemplaes;   
        }

    }




}