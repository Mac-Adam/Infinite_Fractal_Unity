using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FixedPointNumberSystem;
using Colors;
using CommonFunctions;
using CommonShaderRenderFunctions;
using GuiTemplates;

public class MandelbrotContoroler : ShadeContoler
{
    //Shaders
    public ComputeShader InfiniteShader;
    public ComputeShader FloatShader;
    public ComputeShader DoubleShader;
    public ComputeShader RenderShader;
    public Shader AddShader;

    RenderTexture dummyTexture;
    Material addMaterial;

    //DoubleShader
    double[] doubleDataArray = new double[3];
    ComputeBuffer doubleDataBuffer;
    ComputeBuffer MultiFrameRenderBuffer;

    //FloatShader
    float[] floatDataArray = new float[3];
    ComputeBuffer floatDataBuffer;
    ComputeBuffer floatMultiFrameRenderBuffer;

    //InfiShader
    ComputeBuffer FpMultiframeBuffer;
    ComputeBuffer PossionBuffer;
    int[] TestPosiotnArray;

    //RenderShader
    ComputeBuffer IterBuffer;
    ComputeBuffer OldIterBuffer; //used for upscaling
    float colorStrength = 5;
    const float ColorStrengthMax = 1000;
    const float ColorStrengthMin = 1;
    bool smoothGradient = true;
    ComputeBuffer ColorBuffer;
    bool upscaling;
    int currColorPalette = 0;


    //Shader control
    bool reset = false;
    int shiftX = 0;
    int shiftY = 0;
    int register = 0;



    //gui
    bool guiOn = true;
    bool renderFinished = false;
    public UIControler guiControler;
    UITemplate guiTemplate;
    string tooltip;
    const string DoubleTooltip = @"Controls:
I - Zoom In
O - Zoom Out
L - Change Gradient Type
C - Cycle Color Palette
A - Toggle Antialiasing
G - Toggle GUI";
    const string InfiniteTooltip = @"Controls:
I - Zoom In And Pixelize Image
O - Zoom Out Without Rerendering
U - Upscale Image
L - Change Gradient Type
C - Cycle Color Palette
A - Toggle Antialiasing
G - Toggle GUI";




    //shader settings
    public enum Precision { FLOAT = 0, DOUBLE = 1, INFINTE = 2};
    Precision precision = Precision.FLOAT;



    //Anti-Alias
    uint currentSample = 0;
    bool frameFinished = false;
    int currIter = 0;
    bool doAntialasing = false;
    int maxAntiAliasyncReruns = 9;
    private Vector2[] antialiasLookupTable = {
        new Vector2(0,0),
        new Vector2((float)-2/3,(float)-2/3),
        new Vector2((float)-2/3,0),
        new Vector2((float)-2/3,(float)2/3),
        new Vector2(0,(float)2/3),
        new Vector2((float)2/3,(float)2/3),
        new Vector2((float)2/3,0),
        new Vector2((float)2/3,(float)-2/3),
        new Vector2(0,(float)-2/3),

    };


    //Controls
    float scrollSlowness = 10.0f;
    double length = 4.0f;
    double middleX = -1.0f;
    double middleY = 0.0f;

    string pixelizationLevelUpControl = "i";
    string pixelizationLevelDownControl = "o";
    string resetControl = "r";
    string togleInterpolationTypeContorl = "l";
    string colorPaletteTogleContorl = "c";
    string antialiasTogleContorl = "a";
    string upscaleControl = "u";
    string guiToggleControl = "g";
    string scrennShotKey = "s";


    //Controlls Handleing
    int oldMouseTextureCoordinatesX;
    int oldMouseTextureCoordinatesY;
    int PrevScreenX;
    int PrevScreenY;
    static int cpuPrecision = GPUCode.precisions[^1].precision + 5;
    FixedPointNumber MiddleX = new(cpuPrecision);
    FixedPointNumber MiddleY = new(cpuPrecision);
    FixedPointNumber Scale = new(cpuPrecision);

    //precision
    int maxIter = 1000;
    //constanst are a starting point the other one is dynamicly set based on hardware capabilities
    int[] itersPerCycle = new int[] { 50, 10, 3 };
    int IterPerCycle;
    const float minTargetFramerate = 60;
    const float maxTagretFramerate = 200;


    int precisionLevel = 1;
    int shaderPre;
    int shaderPixelSize;


    //Pixelization
    int pixelizationBase = 2;
    int pixelizationLevel = 0;
    int lastPixelizationLevel;
    int preUpscalePixLvl;


    int PixelCount()
    {
        return ReducedHeight()*ReducedWidth();
    }
    int LastPixelCount()
    {
        return LastReducedHeight()*LastReducedWidth();
    }
    int ReducedWidth()
    {
        return OtherFunctions.Reduce(Screen.width,pixelizationBase,pixelizationLevel);
    }
    int ReducedHeight()
    {
        return OtherFunctions.Reduce(Screen.height, pixelizationBase, pixelizationLevel);
    }
    int LastReducedWidth()
    {
        return OtherFunctions.Reduce(Screen.width, pixelizationBase, lastPixelizationLevel);
    }
    int LastReducedHeight()
    {
        return OtherFunctions.Reduce(Screen.height, pixelizationBase, lastPixelizationLevel);
    }

    int maxPixelizationLevel()
    {
        int max = 6; //This will allways be a valid level
        long buffersize;
        do
        {
            max--;
            buffersize = OtherFunctions.Reduce(Screen.width, pixelizationBase, max) * OtherFunctions.Reduce(Screen.height, pixelizationBase, max);
            switch (precision)
            {
                case Precision.FLOAT:
                    buffersize *= sizeof(float) * 2 + sizeof(int) * 2 + sizeof(float);
                    break;
                case Precision.DOUBLE:
                    buffersize *= sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2;
                    break;
                case Precision.INFINTE:
                    buffersize *= sizeof(int)*shaderPixelSize;
                    break;
            }

        } while (buffersize <= PixelizedShaders.MAXBYTESPERBUFFER);
        return max;
    }
    PixelizationData GetPixelizationData()
    {
        return new(ReducedWidth(),ReducedHeight(),LastReducedWidth(),LastReducedHeight(), PixelCount(), LastPixelCount(), pixelizationBase,register);
    }

    void ResetIterPerCycle()
    {
        IterPerCycle = itersPerCycle[(int)precision];
    }
    void SetSPrecision(int val)
    {
        val = Math.Clamp(val, 0, GPUCode.precisions.Length - 1);
        precisionLevel = val;
        ResetPrecision();
        DisposeBuffers();
        InitializeBuffers();
        ResetParams();
    }
    void ResetPrecision()
    {

        shaderPre = GPUCode.precisions[precisionLevel].precision;
        shaderPixelSize = 2 * shaderPre + 3;
        TestPosiotnArray = new int[3 * shaderPre];

    }

    void ResetAntialias()
    {
        currentSample = 0;
        currIter = 0;
        frameFinished = false;
        renderFinished = false;
    }
    public void SetPrecision(Precision val)
    {
        if(val == precision)
        {
            return;
        }
        precision = val;
        tooltip = precision==Precision.INFINTE ? InfiniteTooltip : DoubleTooltip;
        ResetParams();
        ResetAntialias();
    }
    public void SetSmoothGradient(bool val)
    {
        smoothGradient = val;
    }
    public void SetAnitialiasing(bool val)
    {
        doAntialasing = val;
        ResetAntialias();
    }
    public void SetColorPalette(int val)
    {
        currColorPalette = val % MyColoringSystem.colorPalettes.Length;
        ColorBuffer.Dispose();
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[currColorPalette].length, 4 * sizeof(float));

    }
    public void SetMaxIter(int iter)
    {
        maxIter = iter;
        ResetParams();
        ResetAntialias();
    }

    public void SetGuiActive(bool val)
    {
        guiOn = val;
        guiControler.SetEnable(val);
    }
    public void SetColorStrenght(float val)
    {
        colorStrength = Mathf.Clamp(val, ColorStrengthMin, ColorStrengthMax);
    }

    void SaveCurrentRenderTextureAsAPng()
    {
        RenderShader.SetBool("_RenderExact", true);
        PixelizedShaders.Dispatch(RenderShader, dummyTexture);
        OtherFunctions.SaveRenderTextureToFile(dummyTexture, DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss-tt"));
    }



    public override void InitializeBuffers()
    {
        IterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
        OldIterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        floatDataBuffer = new ComputeBuffer(3, sizeof(float));
        MultiFrameRenderBuffer = new ComputeBuffer(PixelCount()*2, sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2);
        floatMultiFrameRenderBuffer = new ComputeBuffer(PixelCount()*2, sizeof(float) * 2 + sizeof(int) * 2 + sizeof(float));
        FpMultiframeBuffer = new ComputeBuffer(PixelCount()*2, sizeof(int) * shaderPixelSize);
        PossionBuffer = new ComputeBuffer(3 * shaderPre, sizeof(int));
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[currColorPalette].length, 4 * sizeof(float));

    }
    public override void InitializeValues()
    {
        ResetPrecision();

        MiddleX.SetDouble(middleX);
        MiddleY.SetDouble(middleY);
        Scale.SetDouble( length / ReducedWidth());

        ResetIterPerCycle();
        addMaterial = new Material(AddShader);
    }
    public override void HandleLastValues()
    {
        lastPixelizationLevel = pixelizationLevel;
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
    }
   
    public override void InitializeGui()
    {
        tooltip = precision == Precision.INFINTE ? InfiniteTooltip : DoubleTooltip;
        guiTemplate = new UITemplate(
        DefaultTemlates.sizes,
        new List<ToggleTemplate>(){
            new ToggleTemplate(
                "Antialiasing",
                doAntialasing,
                (bool b) => SetAnitialiasing(b)
                ),
            new ToggleTemplate(
                "Smooth Gradient",
                smoothGradient,
                (bool b) => SetSmoothGradient(b)
                )
            },
        new List<SliderTemplate>()
        {
            new SliderTemplate(
                "Color Strenght",
                colorStrength,
                1,
                10000,
                true,
                (float f)=> SetColorStrenght(f)
                ),
            new SliderTemplate(
                "Max Iterations",
                maxIter,
                1,
                1000000,
                true,
                (float f)=> SetMaxIter(Mathf.FloorToInt(f))
                )
        },
        new List<DropdownTemplate>() {
            new DropdownTemplate(
                "Color Palette",
                currColorPalette,
                MyColoringSystem.colorPalettes.Select(palette => palette.name).ToList(),
                (int i)=> SetColorPalette(i)
                )
        },
        new List<ProgressBarTemplate>()
        {
            new ProgressBarTemplate(
                "Frame progress",
                0
                ),
            new ProgressBarTemplate(
                "Render progress",
                0
                )
        },
        new List<ButtonTemplate>()
        {
            new ButtonTemplate(
                "Hide GUI",
                ()=>SetGuiActive(false)
                ),
            new ButtonTemplate(
                "Exit",
                ()=>Exit()
                )
        },
        new List<TextTemplate>()
        {
            new TextTemplate(tooltip)
        }

        );
        guiControler.GenerateUI(guiTemplate);
        SetGuiActive(guiOn);

    }
    public override void DisposeBuffers()
    {
        IterBuffer.Dispose();
        OldIterBuffer.Dispose();
        doubleDataBuffer.Dispose();
        floatDataBuffer.Dispose();
        MultiFrameRenderBuffer.Dispose();
        floatMultiFrameRenderBuffer.Dispose();
        ColorBuffer.Dispose();
        FpMultiframeBuffer.Dispose();
        PossionBuffer.Dispose();
    }
    public override void AdditionalCleanup()
    {
        Destroy(dummyTexture);
    }

    public override void HandleKeyInput()
    {
        if (Input.GetKeyDown(scrennShotKey))
        {

            SaveCurrentRenderTextureAsAPng();
        }


        if (Input.GetKeyDown(guiToggleControl))
        {
            SetGuiActive(!guiOn);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Exit();
        }
       
        if (Input.GetKeyDown(antialiasTogleContorl))
        {
            SetAnitialiasing(!doAntialasing);
        }
      
        if (Input.GetKeyDown(pixelizationLevelUpControl))
        {
            preUpscalePixLvl = pixelizationLevel;
            pixelizationLevel += 1;
            upscaling = false;
        }
        if (Input.GetKeyDown(pixelizationLevelDownControl))
        {
            if (pixelizationLevel > 0)
            {
                lastPixelizationLevel = pixelizationLevel;
                pixelizationLevel -= 1;
                upscaling = false;
            }
           
        }
        
        if (Input.GetKeyDown(colorPaletteTogleContorl))
        {
            SetColorPalette(currColorPalette + 1);
        }
        if (Input.GetKeyDown(resetControl))
        {
            ResetParams();
            ResetAntialias();
        }
        if (Input.GetKeyDown(togleInterpolationTypeContorl))
        {
            SetSmoothGradient(!smoothGradient);
        }
        if (Input.GetKeyDown(upscaleControl))
        {
           
            int[] arr = new int[PixelCount() * 3];
            IterBuffer.GetData(arr);
            OldIterBuffer.Dispose();
            OldIterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
            OldIterBuffer.SetData(arr);
            IterBuffer.Dispose();


            preUpscalePixLvl = pixelizationLevel;
            pixelizationLevel -= 1;

            IterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
            FixedPointNumber scaleFixer = new(cpuPrecision);
            scaleFixer.SetDouble(pixelizationLevel > lastPixelizationLevel ? pixelizationBase : 1.0 / pixelizationBase);
            Scale *= scaleFixer;
            upscaling = true;
            renderFinished = false;
            currIter = 0;

            



        }
    }
    public override void HandleScreenSizeChange()
    {
        if (PrevScreenX != Screen.width || PrevScreenY != Screen.height || lastPixelizationLevel != pixelizationLevel)
        {
            IterBuffer.Dispose();
            IterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
            if (lastPixelizationLevel != pixelizationLevel && !upscaling)
            {
                switch (precision)
                {
                    case Precision.INFINTE:
                        PixelizedShaders.HandleZoomPixelization<int>(FpMultiframeBuffer, sizeof(int), lastPixelizationLevel < pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { FpMultiframeBuffer = buffer; }, shaderPixelSize);
                        break;
                    case Precision.DOUBLE:
                        PixelizedShaders.HandleZoomPixelization<DoublePixelPacket>(MultiFrameRenderBuffer, sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2, lastPixelizationLevel < pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { MultiFrameRenderBuffer = buffer; });
                        break;
                    case Precision.FLOAT:
                        PixelizedShaders.HandleZoomPixelization<FloatPixelPacket>(floatMultiFrameRenderBuffer, sizeof(float) * 2 + sizeof(int) * 2 + sizeof(float), lastPixelizationLevel < pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { floatMultiFrameRenderBuffer = buffer; });
                        break;

                }
             
            }
            else
            {
                MultiFrameRenderBuffer.Dispose();
                MultiFrameRenderBuffer = new ComputeBuffer(PixelCount() * 2, sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2);
                floatMultiFrameRenderBuffer.Dispose();
                floatMultiFrameRenderBuffer = new ComputeBuffer(PixelCount() * 2, sizeof(float) * 2 + sizeof(int) * 2 + sizeof(float));
                FpMultiframeBuffer.Dispose();
                FpMultiframeBuffer = new ComputeBuffer(PixelCount() * 2, sizeof(int) * shaderPixelSize);
                reset = true;
            }

           
            
           
           



        }

    }
    public override void HandleMouseInput()
    {
        if(guiOn &&Input.mousePosition.x > Screen.width - guiTemplate.sizes.width)
        {
            return;
        }
        if(Screen.width != PrevScreenX || Screen.height != PrevScreenY)
        {
            return;
        }


        Vector2 mousePosPix = Input.mousePosition;
        int mouseTextureCoordinatesX = OtherFunctions.Reduce((int)mousePosPix.x, pixelizationBase, pixelizationLevel);
        int mouseTextureCoordinatesY = OtherFunctions.Reduce((int)mousePosPix.y, pixelizationBase, pixelizationLevel);


        FixedPointNumber mousePosRealX = new(cpuPrecision);

        mousePosRealX.SetDouble(mouseTextureCoordinatesX - ReducedHeight()/2);
        mousePosRealX = mousePosRealX * Scale + MiddleX;
        FixedPointNumber mousePosRealY = new(cpuPrecision);

        mousePosRealY.SetDouble(mouseTextureCoordinatesY - ReducedHeight()/2);
        mousePosRealY = mousePosRealY * Scale + MiddleY;
        FixedPointNumber multiplyer = new(cpuPrecision);
        if (Input.mouseScrollDelta.y != 0)
        {
           
            double scaleDifference = 1 - Input.mouseScrollDelta.y / scrollSlowness;
            multiplyer.SetDouble(scaleDifference);
            Scale *= multiplyer;

            FixedPointNumber differenceX = mousePosRealX - MiddleX;
            FixedPointNumber differenceY = mousePosRealY - MiddleY;
            multiplyer.SetDouble(1.0 - scaleDifference);
            MiddleX += differenceX * multiplyer;
            MiddleY += differenceY * multiplyer;
            ResetParams();
            

        }
        if (mouseTextureCoordinatesX != oldMouseTextureCoordinatesX || mouseTextureCoordinatesY != oldMouseTextureCoordinatesY)
        {
            if (Input.GetMouseButton(0))
            {
                ResetAntialias();

                shiftX = mouseTextureCoordinatesX - oldMouseTextureCoordinatesX;
                shiftY = mouseTextureCoordinatesY - oldMouseTextureCoordinatesY;

                register = (register + 1) % 2;
                
                multiplyer.SetDouble(mouseTextureCoordinatesX - oldMouseTextureCoordinatesX);
                MiddleX -= multiplyer * Scale;
                multiplyer.SetDouble(mouseTextureCoordinatesY - oldMouseTextureCoordinatesY);
                MiddleY -= multiplyer * Scale;
                ResetParams();

            }

        }
        oldMouseTextureCoordinatesX = mouseTextureCoordinatesX;
        oldMouseTextureCoordinatesY = mouseTextureCoordinatesY;


    }
    public override void HandleGuiUpdates()
    {
        guiControler.UpdateUI(
            new List<bool>() {
                doAntialasing,
                smoothGradient
            },
            new List<float>()
            {
                colorStrength,
                maxIter
            },
            new List<int>()
            {
                currColorPalette
            },
            new List<float>()
            {
                renderFinished ? 1 : currIter/(float)maxIter,
                renderFinished ? 1 : currentSample/(float)maxAntiAliasyncReruns
            },
            new List<string>()
            {
                tooltip
            }
         );



    }
    public override void SetShadersParameters()
    {

        for (int i = 0; i < shaderPre; i++)
        {
            TestPosiotnArray[i] = MiddleX.digits[i];
            TestPosiotnArray[shaderPre + i] = MiddleY.digits[i];
            TestPosiotnArray[shaderPre * 2 + i] = Scale.digits[i];

        }

        PossionBuffer.SetData(TestPosiotnArray);
        doubleDataArray[0] = Scale.ToDouble() * ReducedWidth();
        doubleDataArray[1] = MiddleX.ToDouble();
        doubleDataArray[2] = MiddleY.ToDouble();
        floatDataArray[0] = (float)doubleDataArray[0];
        floatDataArray[1] = (float)doubleDataArray[1];
        floatDataArray[2] = (float)doubleDataArray[2];
        floatDataBuffer.SetData(floatDataArray);
        doubleDataBuffer.SetData(doubleDataArray);
        ColorBuffer.SetData(MyColoringSystem.colorPalettes[currColorPalette].colors);
        switch (precision)
        {
            case Precision.INFINTE:
                GPUCode.ResetAllKeywords();
                Shader.EnableKeyword(GPUCode.precisions[precisionLevel].name);

                InfiniteShader.SetBuffer(0, "_FpMultiframeBuffer", FpMultiframeBuffer);
                InfiniteShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
                InfiniteShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
                InfiniteShader.SetInt("_MaxIter", maxIter);
                InfiniteShader.SetBool("_reset", reset);
                InfiniteShader.SetInt("_pixelizationBase", pixelizationBase);
                InfiniteShader.SetInt("_ShiftX", shiftX);
                InfiniteShader.SetInt("_ShiftY", shiftY);
                InfiniteShader.SetInt("_Register", register);
                InfiniteShader.SetInt("_IterPerCycle", IterPerCycle);
                InfiniteShader.SetBuffer(0, "_IterBuffer", IterBuffer);
                break;
            case Precision.DOUBLE:
                DoubleShader.SetBuffer(0, "_DoubleDataBuffer", doubleDataBuffer);
                DoubleShader.SetBuffer(0, "_MultiFrameData", MultiFrameRenderBuffer);
                DoubleShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
                DoubleShader.SetInt("_MaxIter", maxIter);
                DoubleShader.SetBool("_reset", reset);
                DoubleShader.SetInt("_ShiftX", shiftX);
                DoubleShader.SetInt("_ShiftY", shiftY);
                DoubleShader.SetInt("_Register", register);
                DoubleShader.SetInt("_IterPerCycle", IterPerCycle);
                DoubleShader.SetBuffer(0, "_IterBuffer", IterBuffer);
                break;
            case Precision.FLOAT:
                FloatShader.SetBuffer(0, "_FloatDataBuffer", floatDataBuffer);
                FloatShader.SetBuffer(0, "_MultiFrameData", floatMultiFrameRenderBuffer);
                FloatShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
                FloatShader.SetInt("_MaxIter", maxIter);
                FloatShader.SetBool("_reset", reset);
                FloatShader.SetInt("_ShiftX", shiftX);
                FloatShader.SetInt("_ShiftY", shiftY);
                FloatShader.SetInt("_Register", register);
                FloatShader.SetInt("_IterPerCycle", IterPerCycle);
                FloatShader.SetBuffer(0, "_IterBuffer", IterBuffer);
                break;
        }
            
       
        RenderShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        RenderShader.SetBuffer(0, "_OldIterBuffer", OldIterBuffer);
        RenderShader.SetInt("_MaxIter", maxIter);
        RenderShader.SetFloat("_ColorStrength", colorStrength);
        RenderShader.SetBool("_Smooth", smoothGradient);
        RenderShader.SetBool("_Upscaling", upscaling);
        RenderShader.SetInt("_Type", MyColoringSystem.colorPalettes[currColorPalette].gradientType);
        RenderShader.SetInt("_ReduceAmount", OtherFunctions.IntPow(pixelizationBase,Math.Abs(pixelizationLevel)));
        RenderShader.SetBool("_Superresolution", pixelizationLevel < 0);
        RenderShader.SetBool("_RenderExact", false);
        RenderShader.SetInt("_OldPixelWidth", OtherFunctions.IntPow(pixelizationBase, Math.Abs(preUpscalePixLvl)));
        RenderShader.SetBuffer(0, "_Colors", ColorBuffer);
        RenderShader.SetInt("_ColorArrayLength", MyColoringSystem.colorPalettes[currColorPalette].length);
        reset = false;
        shiftX = 0;
        shiftY = 0;

    }

    public override void ResetParams()
    {
        reset = true;
        currentSample = 0;
        currIter = 0;
        upscaling = false;
        renderFinished = false;
        renderFinished = false;
        ResetIterPerCycle();
    }

    public override void AutomaticParametersChange()
    {
        if (!frameFinished && !renderFinished)
        {
            if (1 / Time.deltaTime > maxTagretFramerate)
            {
                IterPerCycle++;
            }
            else if (1 / Time.deltaTime < minTargetFramerate)
            {
                if (IterPerCycle > 1)
                {
                    IterPerCycle--;
                }

            }
        }
        int tagretPrecison = 0;
        foreach(int digit in Scale.digits)
        {
            if (digit == 0)
            {
                tagretPrecison++;
            }
            else
            {
                break;
            }
        }
        if (tagretPrecison + 1 >= GPUCode.precisions[precisionLevel].precision)
        {
            SetSPrecision(precisionLevel + 1);

        }
        else if (precisionLevel != 0)
        {
            if (tagretPrecison + 1 < GPUCode.precisions[precisionLevel - 1].precision)
            {
                SetSPrecision(precisionLevel - 1);
            }
        }
        if (Scale.ToDouble() > 1E-6)
        {
            SetPrecision(Precision.FLOAT);
        }
        else if(Scale.ToDouble() > 1E-14)
        {
            SetPrecision(Precision.DOUBLE);
        }
        else
        {
            SetPrecision(Precision.INFINTE);
          
        }

   
    }

    public override void HandleAntialias()
    {

        if (!renderFinished)
        {
            currIter += IterPerCycle;
        }
        

        if (doAntialasing)
        {
            if (currentSample < maxAntiAliasyncReruns)
            {
                if (currIter > maxIter)
                {

                    frameFinished = true;
                    currIter = 0;
                }

            }
            else
            {
                renderFinished = true;
            }
        }
        else if (currIter > maxIter)
        {
            renderFinished = true;
        }

    }

    public override void AddiitionalTextureRegenerationHandeling()
    {
        currentSample = 0;
  
    }
    public override bool ShouldRegerateTexture()
    {
        return lastPixelizationLevel != pixelizationLevel;
    }
    public override void InitializeOtherTextures()
    {
        dummyTexture = PixelizedShaders.InitializePixelizedTexture(dummyTexture, pixelizationBase, pixelizationLevel, ShouldRegerateTexture());
    }
    public override void DispatchShaders()
    {
      
        switch (precision)
        {
            case Precision.INFINTE:
                PixelizedShaders.Dispatch(InfiniteShader, dummyTexture);
                break;
            case Precision.DOUBLE:
                PixelizedShaders.Dispatch(DoubleShader, dummyTexture);
                break;
            case Precision.FLOAT:
                PixelizedShaders.Dispatch(FloatShader, dummyTexture);
                break;
        }
        PixelizedShaders.Dispatch(RenderShader, targetTexture);

    }

    public override void BlitTexture(RenderTexture destination)
    {
        Antialiasing.BlitWitthAntialiasing(currentSample, frameFinished, destination, targetTexture, addMaterial,
            () =>
            {
                frameFinished = false;
                currentSample++;
                reset = true;
            });

    }

}
