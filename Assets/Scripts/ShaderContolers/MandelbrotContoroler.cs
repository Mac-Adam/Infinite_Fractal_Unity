using System;
using UnityEngine;
using FixedPointNumberSystem;
using Colors;
using CommonFunctions;
using CommonShaderRenderFunctions;

public class MandelbrotContoroler : MonoBehaviour
{
    //Shaders
    public ComputeShader InfiniteShader;
    public ComputeShader FloatShader;
    public ComputeShader RenderShader;
    public ComputeShader ResetShader;
    public ComputeShader ShiftShader;
    public Shader AddShader;

    RenderTexture targetTexture;

    RenderTexture dummyTexture;
    RenderTexture screenshotTexture;
    Material addMaterial;


    //the reason for the buffers of size 3 is that it is simpler this way to set it with the same code for both float and double
    //DoubleShader
    double[] doubleDataArray = new double[3];
    ComputeBuffer doubleDataBuffer;
    ComputeBuffer doubleMultiFrameRenderBuffer;

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

    ComputeBuffer ColorBuffer;




    //Not yet sure where this should be

    GuiController guiController;
    CameraController cameraController;
   
    //not yet sure if needed 
    int preUpscalePixLvl;


    //Anti-Alias


    static private Vector2[] antialiasLookupTableSmooth = {
        new Vector2(0,0),
        new Vector2(-2.0f/3,-2.0f/3),
        new Vector2(-2.0f/3,0),
        new Vector2(-2.0f/3,2.0f/3),
        new Vector2(0,2.0f/3),
        new Vector2(2.0f/3,2.0f/3),
        new Vector2(2.0f/3,0),
        new Vector2(2.0f/3,-2.0f/3),
        new Vector2(0,-2.0f/3),

    };
    static private Vector2[] antialiasLookupTableSharp = {
        new Vector2(0,0),
        new Vector2(-1.0f/3,-1.0f/3),
        new Vector2(-1.0f/3,0),
        new Vector2(-1.0f/3,1.0f/3),
        new Vector2(0,1.0f/3),
        new Vector2(1.0f/3,1.0f/3),
        new Vector2(1.0f/3,0),
        new Vector2(1.0f/3,-1.0f/3),
        new Vector2(0,-1.0f/3),

    };



   

    //Controls
    double length = 4.0f;
    double middleX = -1.0f;
    double middleY = 0.0f;

    //constanst are a starting point the other one is dynamicly set based on hardware capabilities
    int[] itersPerCycle = new int[] { 50, 10, 1 };
    const float minTargetFramerate = 60;
    const float maxTagretFramerate = 200;

    Settings settings = new(
        false,                      //upscaling
        0,                          //register
        false,                      //zoomVideo
        2,                          //pixelizationBase 
        0,                          //pixelizationLevel
        0,                          //lastPixelizationLevel
        0,                          //currentSample 
        9,                          //maxAntiAliasyncReruns
        antialiasLookupTableSharp,  //antialiasLookupTable.
        false,                      //frankensteinRendering;
        1,                          //frankensteinSteps;
        0,                          //frankensteinX
        0,                          //frankensteinY
        Precision.FLOAT,            //precision
        1,                          //precisionLevel
        50,                         //IterPerCycle
        false                       //doAntialasing
    );

    DynamicSettings dynamicSettings = new(
        0,                          //currIter
        false,                      //reset 
        false,                      //turboReset
        0,                          //shiftX 
        0,                          //shiftY 
        false,                      //renderFinished
        false,                      //frameFinished
        false,                      //frankensteinStepFinished
        0,                          //renderStatTime
        0                           //renderTimeElapsed
    );

    PixelizationData GetPixelizationData()
    {
        //TODO fix it probably won't work
        return new(settings.ReducedWidth(), settings.ReducedHeight(), settings.LastReducedWidth(), settings.LastReducedHeight(), settings.PixelCount(), settings.LastPixelCount(), settings.pixelizationBase, settings.register);
    }

    void ResetIterPerCycle()
    {
        settings.iterPerCycle = itersPerCycle[(int)settings.precision];
    }
    void SetSPrecision(int val)
    {
        val = Math.Clamp(val, 0, GPUCode.precisions.Length - 1);
        settings.precisionLevel = val;
        ResetPrecision();
        DisposeBuffers();
        InitializeBuffers();
        ResetParams();
    }
    void ResetPrecision()
    {
        TestPosiotnArray = new int[3 * settings.GetShaderPre()];
    }

    void ResetAntialias()
    {
        settings.currentSample = 0;
        dynamicSettings.currIter = 0;
        SetFrameFinished(false);
        SetFrankensteinFinished(false);
        SetRenderFinished(false);
        if (settings.frankensteinRendering)
        {
            ResetParams();
            dynamicSettings.turboReset = true;
        }
    }

    void OnMoveComand()
    {
        settings.zoomVideo = false;
        if (settings.frankensteinRendering)
        {
            ResetParams();
            dynamicSettings.turboReset = true;
        }
    }
    public void SetPrecision(Precision val)
    {
        if(val == settings.precision)
        {
            return;
        }
        settings.precision = val;
        if (settings.pixelizationLevel < settings.MaxPixelizationLevel())
        {
            settings.pixelizationLevel = settings.MaxPixelizationLevel();
        }
        RegenereateFractalComputeBuffers();
        ResetParams();
        ResetAntialias();
    }
   
    void SetRenderFinished(bool val)
    {
        if (val == false) {
            dynamicSettings.renderStatTime = Time.time;
        }

        if(dynamicSettings.renderFinished == val)
        {
            return;
        }

        dynamicSettings.renderFinished = val;
        if(dynamicSettings.renderFinished == true)
        {
            dynamicSettings.renderTimeElapsed = Time.time - dynamicSettings.renderStatTime;
        }
       
    }
    void SetFrameFinished(bool val)
    {
        if(dynamicSettings.frameFinished == val)
        {
            return;
        }

        dynamicSettings.currIter = 0;
        if (settings.doAntialasing)
        {
            if (settings.currentSample >= settings.maxAntiAliasyncReruns)
            {
                SetRenderFinished(true);
            }
        }
        else
        {
            SetRenderFinished(true);
        }

        dynamicSettings.frameFinished = val;
        if(dynamicSettings.frameFinished == false)
        {
            settings.frankensteinX = 0;
            settings.frankensteinY = 0;

        }
    }
    void SetFrankensteinFinished(bool val)
    {
        if (!settings.frankensteinRendering)
        {
            SetFrameFinished(val);

            dynamicSettings.frankensteinStepFinished = val;
            return;
        }
        if (dynamicSettings.frankensteinStepFinished == val)
        {
            return;
        }
        if(val == true)
        {
            if(settings.frankensteinX == settings.frankensteinSteps - 1 && settings.frankensteinY == settings.frankensteinSteps - 1)
            {
                dynamicSettings.frankensteinStepFinished = true;
                Debug.Log("2");
                SetFrameFinished(true);
            }
            else
            {           
                ResetIterPerCycle();
                dynamicSettings.reset = true;
                dynamicSettings.currIter = 0;
                settings.frankensteinX++;
                if (settings.frankensteinX >= settings.frankensteinSteps)
                {
                    settings.frankensteinX = 0;
                    settings.frankensteinY++;
                }

            }
        }
        else
        {
            dynamicSettings.frankensteinStepFinished = false;
        }
        
    }
    
    void SaveCurrentRenderTextureAsAPng()
    {
        RenderShader.SetBool("_RenderExact", true);
        screenshotTexture = PixelizedShaders.InitializePixelizedTexture(screenshotTexture, settings.ReducedWidth(false), settings.ReducedHeight(false),true);
        PixelizedShaders.Dispatch(RenderShader, screenshotTexture);
        OtherFunctions.SaveRenderTextureToFile(screenshotTexture, DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss-tt-fff"));
    }


    void RegenereateFractalComputeBuffers()
    {
        if (doubleMultiFrameRenderBuffer != null)
        {
            doubleMultiFrameRenderBuffer.Dispose();
        }
        if (floatMultiFrameRenderBuffer != null)
        {
            floatMultiFrameRenderBuffer.Dispose();
        }
        if (FpMultiframeBuffer != null)
        {
            FpMultiframeBuffer.Dispose();
        }

        
        
        switch (settings.precision)
        {
            case Precision.INFINTE:
                FpMultiframeBuffer = new ComputeBuffer(settings.PixelCount() * 2, sizeof(int) * settings.GetShaderPixelSize());
                break;
            case Precision.DOUBLE:
                doubleMultiFrameRenderBuffer = new ComputeBuffer(settings.PixelCount() * 2, DoublePixelPacket.size);
                break;
            case Precision.FLOAT:
                floatMultiFrameRenderBuffer = new ComputeBuffer(settings.PixelCount() * 2, FloatPixelPacket.size);
                break;

        }


    }
    public void InitializeBuffers()
    {
        IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
        OldIterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        floatDataBuffer = new ComputeBuffer(3, sizeof(float));
        PossionBuffer = new ComputeBuffer(3 * settings.GetShaderPre(), sizeof(int));
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[guiController.currColorPalette].length, 4 * sizeof(float));
        RegenereateFractalComputeBuffers();
    }
 
    public void InitializeValues()
    {
        guiController = gameObject.AddComponent<GuiController>();
        cameraController = gameObject.AddComponent<CameraController>();
        //Fix just for now
        preUpscalePixLvl = settings.pixelizationLevel;
        
        ResetPrecision();


        cameraController.MiddleX.SetDouble(middleX);
        cameraController.MiddleY.SetDouble(middleY);
        cameraController.Scale.SetDouble( length / settings.ReducedWidth(false));

        ResetIterPerCycle();
        addMaterial = new Material(AddShader);
        settings.antialiasLookupTable = antialiasLookupTableSharp;
    }
    public void HandleLastValues()
    {
        //lastPixelizationLevel = pixelizationLevel;
    }
   
    public void DisposeBuffers()
    {
        IterBuffer.Dispose();
        OldIterBuffer.Dispose();
        doubleDataBuffer.Dispose();
        floatDataBuffer.Dispose();
      
        ColorBuffer.Dispose();
       
        PossionBuffer.Dispose();
        if (doubleMultiFrameRenderBuffer != null)
        {
            doubleMultiFrameRenderBuffer.Dispose();
        }
        if (floatMultiFrameRenderBuffer != null)
        {
            floatMultiFrameRenderBuffer.Dispose();
        }
        if (FpMultiframeBuffer != null)
        {
            FpMultiframeBuffer.Dispose();
        }

    }
    public void AdditionalCleanup()
    {
        Destroy(dummyTexture);
        Destroy(screenshotTexture);
    }

    public void HandleScreenSizeChange()
    {
        if (cameraController.screenSizeChanged || settings.lastPixelizationLevel != settings.pixelizationLevel)
        {
            IterBuffer.Dispose();
            IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
            if (settings.lastPixelizationLevel != settings.pixelizationLevel && !settings.upscaling)
            {
                switch (settings.precision)
                {
                    case Precision.INFINTE:
                        PixelizedShaders.HandleZoomPixelization<int>(FpMultiframeBuffer, sizeof(int), settings.lastPixelizationLevel < settings.pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { FpMultiframeBuffer = buffer; }, settings.GetShaderPixelSize());
                        break;
                    case Precision.DOUBLE:
                        PixelizedShaders.HandleZoomPixelization<DoublePixelPacket>(doubleMultiFrameRenderBuffer, DoublePixelPacket.size, settings.lastPixelizationLevel < settings.pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { doubleMultiFrameRenderBuffer = buffer; });
                        break;
                    case Precision.FLOAT:
                        PixelizedShaders.HandleZoomPixelization<FloatPixelPacket>(floatMultiFrameRenderBuffer, FloatPixelPacket.size, settings.lastPixelizationLevel < settings.pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { floatMultiFrameRenderBuffer = buffer; });
                        break;

                }
                settings.lastPixelizationLevel = settings.pixelizationLevel;
                SetFrameFinished(false);
            }
            else
            {
                RegenereateFractalComputeBuffers();
                dynamicSettings.reset = true;
                OnMoveComand();
            }


            





        }

    }
    
    public void SetShadersParameters()
    {

        FixedPointNumber MiddleXToSend = new(cameraController.MiddleX);
        FixedPointNumber MiddleYToSend = new(cameraController.MiddleY);
        if (settings.frankensteinRendering)
        {
            int frankensteinOffsetX = settings.ReducedWidth() * settings.frankensteinX - (settings.ReducedWidth() * (settings.frankensteinSteps - 1)) / 2;
            int frankensteinOffsetY = settings.ReducedHeight() * settings.frankensteinY - (settings.ReducedHeight() * (settings.frankensteinSteps - 1)) / 2;
            FixedPointNumber temp = new(CameraController.cpuPrecision);
            temp.SetDouble(frankensteinOffsetX);
            temp *= cameraController.Scale;
            MiddleXToSend += temp;
            temp.SetDouble(frankensteinOffsetY);
            temp *= cameraController.Scale;
            MiddleYToSend += temp;
        }
        


        for (int i = 0; i < settings.GetShaderPre(); i++)
        {
            TestPosiotnArray[i] = MiddleXToSend.digits[i];
            TestPosiotnArray[settings.GetShaderPre() + i] = MiddleYToSend.digits[i];
            TestPosiotnArray[settings.GetShaderPre() * 2 + i] = cameraController.Scale.digits[i];

        }

        PossionBuffer.SetData(TestPosiotnArray);
        doubleDataArray[0] = cameraController.Scale.ToDouble();
        doubleDataArray[1] = MiddleXToSend.ToDouble();
        doubleDataArray[2] = MiddleYToSend.ToDouble();
        floatDataArray[0] = (float)doubleDataArray[0];
        floatDataArray[1] = (float)doubleDataArray[1];
        floatDataArray[2] = (float)doubleDataArray[2];
        floatDataBuffer.SetData(floatDataArray);
        doubleDataBuffer.SetData(doubleDataArray);
        ColorBuffer.SetData(MyColoringSystem.colorPalettes[guiController.currColorPalette].colors);
       
        if(dynamicSettings.shiftX != 0|| dynamicSettings.shiftY !=0)
        {
            dynamicSettings.reset = false;
        }
        Shader.DisableKeyword("FLOAT");
        Shader.DisableKeyword("DOUBLE");
        Shader.DisableKeyword("INFINITE");
        if (settings.precision == Precision.INFINTE)
        {
            Shader.EnableKeyword("INFINITE");
            GPUCode.ResetAllKeywords();
            Shader.EnableKeyword(GPUCode.precisions[settings.precisionLevel].name);

            InfiniteShader.SetBuffer(0, "_FpMultiframeBuffer", FpMultiframeBuffer);
            InfiniteShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
            InfiniteShader.SetVector("_PixelOffset", settings.antialiasLookupTable[settings.currentSample % settings.antialiasLookupTable.Length]);
            InfiniteShader.SetInt("_MaxIter", guiController.maxIter);
            InfiniteShader.SetBool("_reset", dynamicSettings.reset || dynamicSettings.turboReset);
            InfiniteShader.SetInt("_pixelizationBase", settings.pixelizationBase);
            InfiniteShader.SetInt("_Register", settings.register);
            InfiniteShader.SetInt("_IterPerCycle", settings.iterPerCycle);
            InfiniteShader.SetBuffer(0, "_IterBuffer", IterBuffer);
            InfiniteShader.SetInt("_BailoutRadius", guiController.bailoutRadius);
            InfiniteShader.SetInt("_RenderWidth", settings.ReducedWidth(false));
            InfiniteShader.SetInt("_FrankensteinOffsetX", settings.frankensteinX * settings.ReducedWidth());
            InfiniteShader.SetInt("_FrankensteinOffsetY", settings.frankensteinY * settings.ReducedHeight());

            ResetShader.SetBuffer(0, "_MultiFrameData", FpMultiframeBuffer);
            ResetShader.SetInt("_Precision", GPUCode.precisions[settings.precisionLevel].precision);

            ShiftShader.SetBuffer(0, "_MultiFrameData", FpMultiframeBuffer);
            ShiftShader.SetInt("_Precision", GPUCode.precisions[settings.precisionLevel].precision);
        }
        else if (settings.precision == Precision.DOUBLE || settings.precision == Precision.FLOAT)
        {
            if (settings.precision == Precision.DOUBLE)
            {
                Shader.EnableKeyword("DOUBLE");
                FloatShader.SetBuffer(0, "_DataBuffer", doubleDataBuffer);
                FloatShader.SetBuffer(0, "_MultiFrameData", doubleMultiFrameRenderBuffer);


                ResetShader.SetBuffer(0, "_MultiFrameData", doubleMultiFrameRenderBuffer);
                ShiftShader.SetBuffer(0, "_MultiFrameData", doubleMultiFrameRenderBuffer);

            }
            else
            {
                Shader.EnableKeyword("FLOAT");
                FloatShader.SetBuffer(0, "_DataBuffer", floatDataBuffer);
                FloatShader.SetBuffer(0, "_MultiFrameData", floatMultiFrameRenderBuffer);

                ResetShader.SetBuffer(0, "_MultiFrameData", floatMultiFrameRenderBuffer);
                ShiftShader.SetBuffer(0, "_MultiFrameData", floatMultiFrameRenderBuffer);
            }

            FloatShader.SetVector("_PixelOffset", settings.antialiasLookupTable[settings.currentSample % settings.antialiasLookupTable.Length]);
            FloatShader.SetInt("_MaxIter", guiController.maxIter);
            FloatShader.SetInt("_Register", settings.register);
            FloatShader.SetInt("_IterPerCycle", settings.iterPerCycle);
            FloatShader.SetBuffer(0, "_IterBuffer", IterBuffer);
            FloatShader.SetInt("_BailoutRadius", guiController.bailoutRadius);
            FloatShader.SetInt("_RenderWidth", settings.ReducedWidth(false));
            FloatShader.SetInt("_FrankensteinOffsetX", settings.frankensteinX * settings.ReducedWidth());
            FloatShader.SetInt("_FrankensteinOffsetY", settings.frankensteinY * settings.ReducedHeight());
       
        }


        ShiftShader.SetInt("_Register", settings.register);
        ShiftShader.SetInt("_ShiftX", dynamicSettings.shiftX);
        ShiftShader.SetInt("_ShiftY", dynamicSettings.shiftY);

        ResetShader.SetInt("_Register", settings.register);


        RenderShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        RenderShader.SetBuffer(0, "_OldIterBuffer", OldIterBuffer);
        RenderShader.SetInt("_MaxIter", guiController.maxIter);
        RenderShader.SetFloat("_ColorStrength", guiController.colorStrength);
        RenderShader.SetBool("_Smooth", guiController.smoothGradient);
        RenderShader.SetBool("_Upscaling", settings.upscaling);
        RenderShader.SetBool("_Reset", dynamicSettings.turboReset);
        RenderShader.SetInt("_Type", MyColoringSystem.colorPalettes[guiController.currColorPalette].gradientType);
        RenderShader.SetInt("_ReduceAmount", OtherFunctions.IntPow(settings.pixelizationBase,Math.Abs(settings.pixelizationLevel)));
        RenderShader.SetBool("_Superresolution", settings.pixelizationLevel < 0);
        RenderShader.SetBool("_RenderExact", false);
        RenderShader.SetInt("_OldPixelWidth", OtherFunctions.IntPow(settings.pixelizationBase, Math.Abs(preUpscalePixLvl)));
        RenderShader.SetBuffer(0, "_Colors", ColorBuffer);
        RenderShader.SetInt("_ColorArrayLength", MyColoringSystem.colorPalettes[guiController.currColorPalette].length);

    }

    public void ResetParams()
    {
        dynamicSettings.reset = true;
        settings.currentSample = 0;
        dynamicSettings.currIter = 0;
        settings.upscaling = false;
        SetFrameFinished(false);
        SetFrankensteinFinished(false);
        SetRenderFinished(false);
        ResetIterPerCycle();
        if (settings.frankensteinRendering)
        {
            settings.frankensteinX = 0;
            settings.frankensteinY = 0;
        }
    }

    public void AutomaticParametersChange()
    {
        //This code will be somewhere else in the long run
        if (guiController.requestingSS)
        {
            SaveCurrentRenderTextureAsAPng();
            guiController.requestingSS = false;
        }
        if (guiController.requestedZoomVid)
        {
            //this may need some more code
            settings.zoomVideo = !settings.zoomVideo;
            guiController.requestedZoomVid = false;
        }
        if (guiController.changedAntialias)
        {
            settings.doAntialasing = !settings.doAntialasing;
            ResetAntialias();
            OnMoveComand();
            guiController.changedAntialias = false;
        }
        if (guiController.currColorPalette != guiController.lastColorPalette)
        {
            ColorBuffer.Dispose();
            ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[guiController.currColorPalette].length, 4 * sizeof(float));
            OnMoveComand();
        }
        if (guiController.resetRequested)
        {
            dynamicSettings.turboReset = true;
            ResetParams();
            ResetAntialias();
            guiController.resetRequested = false;
        }
        if (guiController.changedSmoothGradient)
        {
            OnMoveComand();
            guiController.changedSmoothGradient = false;
        }
        if (guiController.changedMaxIter)
        {
            ResetParams();
            ResetAntialias();
            guiController.changedMaxIter = false;
        }
        if (guiController.changedBailoutRadius)
        {
            ResetParams();
            ResetAntialias();
            guiController.changedBailoutRadius = false;
        }
        if (guiController.changedFrankenstein)
        {
            settings.frankensteinSteps = OtherFunctions.IntPow(2, guiController.requestedFrankensteinLevel);
            settings.frankensteinX = 0;
            settings.frankensteinY = 0;
            ResetParams();
            dynamicSettings.turboReset = true;
            guiController.changedFrankenstein = false;
        }

        if (guiController.RequestedUpscale)
        {
            if (settings.MaxPixelizationLevel() < settings.pixelizationLevel)
            {

                int[] arr = new int[settings.PixelCount(false) * 3];
                IterBuffer.GetData(arr);
                OldIterBuffer.Dispose();
                OldIterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
                OldIterBuffer.SetData(arr);
                IterBuffer.Dispose();


                preUpscalePixLvl = settings.pixelizationLevel;
                settings.pixelizationLevel -= 1;

                IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
                FixedPointNumber scaleFixer = new(CameraController.cpuPrecision);
                scaleFixer.SetDouble(settings.pixelizationLevel > settings.lastPixelizationLevel ? settings.pixelizationBase : 1.0 / settings.pixelizationBase);
                cameraController.Scale *= scaleFixer;
                settings.upscaling = true;
                SetRenderFinished(false);
                SetFrankensteinFinished(false);
                SetFrameFinished(false);
                dynamicSettings.currIter = 0;
                OnMoveComand();
            }
            guiController.RequestedUpscale = false;
        }
        if (guiController.pixelizationChange != 0)
        {
            if(settings.MaxPixelizationLevel() < settings.pixelizationLevel + guiController.pixelizationChange)
            {
                if (guiController.pixelizationChange > 0)
                {
                    preUpscalePixLvl = settings.pixelizationLevel;
                }
                settings.lastPixelizationLevel = settings.pixelizationLevel;
                settings.pixelizationLevel += guiController.pixelizationChange;

                settings.upscaling = false;
                dynamicSettings.currIter = 0;
                OnMoveComand();
            }
            guiController.pixelizationChange = 0;
        }
        if (cameraController.scrollMoved)
        {
            OnMoveComand();
            ResetParams();

            cameraController.scrollMoved = false;
        }
        if(cameraController.shiftX !=0 || cameraController.shiftY!= 0)
        {
            dynamicSettings.shiftX = cameraController.shiftX;
            dynamicSettings.shiftY = cameraController.shiftY;

            ResetAntialias();
            settings.register = (settings.register + 1) % 2;
            OnMoveComand();
            ResetParams();

            cameraController.shiftX = 0;
            cameraController.shiftY = 0;
        }
        guiController.settings = settings;
        guiController.dynamicSettings = dynamicSettings;

        cameraController.settings = settings;

        cameraController.deadZoneRight = guiController.guiOn ? (int)guiController.guiTemplate.sizes.width : 0;

        // end of the temp code



        if (!dynamicSettings.frankensteinStepFinished && !dynamicSettings.renderFinished)
        {
            if (1 / Time.deltaTime > maxTagretFramerate)
            {
                settings.iterPerCycle++;
            }
            else if (1 / Time.deltaTime < minTargetFramerate)
            {
                if (settings.iterPerCycle > 1)
                {
                    settings.iterPerCycle--;
                }

            }
        }
        if (settings.zoomVideo)
        {
            if (dynamicSettings.renderFinished)
            {
                SaveCurrentRenderTextureAsAPng();
              
                FixedPointNumber mul = new(CameraController.cpuPrecision);
                mul.SetDouble(settings.pixelizationBase);
                cameraController.Scale *= mul;
                ResetParams();
                if (cameraController.Scale.ToDouble() >= 0.003)
                {
                    settings.zoomVideo = false;
                }

            }
        }

        int tagretPrecison = 0;
        foreach(int digit in cameraController.Scale.digits)
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
        if (tagretPrecison + 1 >= GPUCode.precisions[settings.precisionLevel].precision)
        {
            SetSPrecision(settings.precisionLevel + 1);

        }
        else if (settings.precisionLevel != 0)
        {
            if (tagretPrecison + 1 < GPUCode.precisions[settings.precisionLevel - 1].precision)
            {
                SetSPrecision(settings.precisionLevel - 1);
            }
        }
        if (cameraController.Scale.ToDouble() > 1E-6)
        {
            SetPrecision(Precision.FLOAT);
        }
        else if(cameraController.Scale.ToDouble() > 1E-14)
        {
            SetPrecision(Precision.DOUBLE);
        }
        else
        {
            SetPrecision(Precision.INFINTE);
          
        }

   
    }

    public void HandleAntialias()
    {
        if (!dynamicSettings.frankensteinStepFinished)
        {
            dynamicSettings.currIter += settings.iterPerCycle;
        }

        if (dynamicSettings.currIter > guiController.maxIter)
        {
            SetFrankensteinFinished(true);
        }

    }

    public void AddiitionalTextureRegenerationHandeling()
    {
        settings.currentSample = 0;
  
    }
    public bool ShouldRegerateTexture()
    {
        return settings.lastPixelizationLevel != settings.pixelizationLevel;
    }
    public void InitializeOtherTextures()
    {
        dummyTexture = PixelizedShaders.InitializePixelizedTexture(dummyTexture, settings.ReducedWidth(), settings.ReducedHeight());
    }
    public void DispatchShaders()
    {
        if (dynamicSettings.reset || dynamicSettings.turboReset)
        {
            PixelizedShaders.Dispatch(ResetShader, dummyTexture);
        }
        if (dynamicSettings.shiftX != 0 || dynamicSettings.shiftY != 0)
        {
            PixelizedShaders.Dispatch(ShiftShader, dummyTexture);
        }
        switch (settings.precision)
        {
            case Precision.INFINTE:
                PixelizedShaders.Dispatch(InfiniteShader, dummyTexture);
                break;
            case Precision.DOUBLE:
            case Precision.FLOAT:
              
                PixelizedShaders.Dispatch(FloatShader, dummyTexture);
                break;
        }
        PixelizedShaders.Dispatch(RenderShader, targetTexture);
        dynamicSettings.reset = false;
        dynamicSettings.turboReset = false;
        dynamicSettings.shiftX = 0;
        dynamicSettings.shiftY = 0;
    }

    public void BlitTexture(RenderTexture destination)
    {
        
        Antialiasing.BlitWitthAntialiasing(settings.currentSample, dynamicSettings.frameFinished, dynamicSettings.renderFinished,
            Input.GetMouseButton(0) && Input.mousePosition.x < Screen.width - guiController.guiTemplate.sizes.width
            , destination, targetTexture, addMaterial,
            () =>
            {
                SetFrameFinished(false);
                SetFrankensteinFinished(false);
                settings.currentSample++;
                dynamicSettings.reset = true;
            });

    }


    private void Awake()
    {
        Application.targetFrameRate = -1;
        InitializeValues();
        InitializeBuffers();
        HandleLastValues();
        ResetParams();

    }
    //Some of the code executed here needs to be executer after the other modules have finishied their code
    void LateUpdate()
    {
        HandleScreenSizeChange();
        HandleAntialias();
        AutomaticParametersChange();

        HandleLastValues();
    }
    private void OnDestroy()
    {
        Destroy(targetTexture);
        DisposeBuffers();
        AdditionalCleanup();
    }

    private void InitializeRenderTextures()
    {
        if (targetTexture == null || targetTexture.width != Screen.width || targetTexture.height != Screen.height || ShouldRegerateTexture())
        {
            if (targetTexture != null)
            {
                targetTexture.Release();
            }

            targetTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            targetTexture.Create();
            AddiitionalTextureRegenerationHandeling();

        }
        InitializeOtherTextures();
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitializeRenderTextures();


        DispatchShaders();

        BlitTexture(destination);


    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShadersParameters();
        Render(destination);
    }


}
