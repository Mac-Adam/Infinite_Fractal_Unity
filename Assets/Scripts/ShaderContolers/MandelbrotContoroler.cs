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
    bool upscaling;


    //Shader control
    bool reset = false;
    bool turboReset = false;
    int shiftX = 0;
    int shiftY = 0;
    int register = 0;


    //Not yet sure where this should be

    GuiController guiController;
    CameraController cameraController;
    int currIter = 0;
    bool zoomVideo = false;
    bool renderFinished = false;
    int preUpscalePixLvl;
    int pixelizationBase = 2;
    int pixelizationLevel = 0;
    int lastPixelizationLevel = 0;


    //Anti-Alias
    private uint currentSample = 0;
    bool frameFinished = false;
    int maxAntiAliasyncReruns = 9;
    private Vector2[] antialiasLookupTable;
    private Vector2[] antialiasLookupTableSmooth = {
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
    private Vector2[] antialiasLookupTableSharp = {
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


    //gui


    //Frankenstein rendering (this probably has some fancy technical term)
    //Rendering frame by wornikng only on a small portion of it a time in order to decreese memory usage
    bool frankensteinRendering = false;
    int frankensteinSteps = 1; 
    int frankensteinX = 0;
    int frankensteinY = 0;
    bool frankensteinStepFinished = false;


    //shader settings
    Precision precision = Precision.FLOAT;



   

    //Controls
    double length = 4.0f;
    double middleX = -1.0f;
    double middleY = 0.0f;

    //constanst are a starting point the other one is dynamicly set based on hardware capabilities
    int[] itersPerCycle = new int[] { 50, 10, 1 };
    int IterPerCycle;
    const float minTargetFramerate = 60;
    const float maxTagretFramerate = 200;


    int precisionLevel = 1;
    int shaderPre;
    int shaderPixelSize;




    float renderStatTime;
    float renderTimeElapsed = 0;

    int FrankensteinCorrection()
    {
        if (frankensteinRendering)
        {
            return frankensteinSteps;
        }
        return 1;
             
    }
    int PixelCount(bool frankenstein = true)
    {
        return ReducedHeight(frankenstein) * ReducedWidth(frankenstein);

    }
    int LastPixelCount(bool frankenstein = true)
    {
        return LastReducedHeight(frankenstein) * LastReducedWidth(frankenstein);
    }
    int ReducedWidth(bool frankenstein = true)
    {
        if (frankenstein)
        {
            return OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel) / FrankensteinCorrection();
        }
        return OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel);

    }
    int ReducedHeight(bool frankenstein = true)
    {
        if (frankenstein)
        {
            return OtherFunctions.Reduce(Screen.height, pixelizationBase,pixelizationLevel) / FrankensteinCorrection();
        }
        return OtherFunctions.Reduce(Screen.height, pixelizationBase,pixelizationLevel);
    }
    int LastReducedWidth(bool frankenstein = true)
    {
        if (frankenstein)
        {
            return OtherFunctions.Reduce(Screen.width, pixelizationBase, lastPixelizationLevel) / FrankensteinCorrection();
        }
        return OtherFunctions.Reduce(Screen.width, pixelizationBase, lastPixelizationLevel);
    }
    int LastReducedHeight(bool frankenstein = true)
    {
        if (frankenstein)
        {
            return OtherFunctions.Reduce(Screen.height, pixelizationBase, lastPixelizationLevel) / FrankensteinCorrection();
        }
        return OtherFunctions.Reduce(Screen.height, pixelizationBase, lastPixelizationLevel);
    }

    public void SetFrankensteinLevel(int level)
    {
        int steps = OtherFunctions.IntPow(2, level);
        if (steps == frankensteinSteps)
        {
            return;
        }
        frankensteinRendering = steps != 1;
        frankensteinSteps = steps;
        frankensteinX = 0;
        frankensteinY = 0;
        ResetParams();
        turboReset = true;
    }

    int MaxPixelizationLevel()
    {
        int max = 6; //This will allways be a valid level
        long pixelCount;
        long bufferSize = 0;
        long iterSize;
        do
        {
            max--;
            pixelCount = OtherFunctions.Reduce(Screen.width, pixelizationBase, max) * OtherFunctions.Reduce(Screen.height, pixelizationBase, max);
            iterSize = pixelCount * 3 * sizeof(int);
            switch (precision)
            {
                case Precision.FLOAT:
                    bufferSize = 2 * pixelCount * FloatPixelPacket.size;
                    break;
                case Precision.DOUBLE:
                    bufferSize = 2 * pixelCount * DoublePixelPacket.size;
                    break;
                case Precision.INFINTE:
                    bufferSize = 2 * pixelCount * sizeof(int) * shaderPixelSize;
                    break;
            }

        } while (bufferSize <= PixelizedShaders.MAXBYTESPERBUFFER * FrankensteinCorrection() && iterSize <= PixelizedShaders.MAXBYTESPERBUFFER); ;
        return max + 1;
    }
    PixelizationData GetPixelizationData()
    {
        //TODO fix it probably won't work
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
        SetFrameFinished(false);
        SetFrankensteinFinished(false);
        SetRenderFinished(false);
        if (frankensteinRendering)
        {
            ResetParams();
            turboReset = true;
        }
    }

    void OnMoveComand()
    {
        zoomVideo = false;
        if (frankensteinRendering)
        {
            ResetParams();
            turboReset = true;
        }
    }
    public void SetPrecision(Precision val)
    {
        if(val == precision)
        {
            return;
        }
        precision = val;
        if (pixelizationLevel < MaxPixelizationLevel())
        {
           pixelizationLevel = MaxPixelizationLevel();
        }
        RegenereateFractalComputeBuffers();
        ResetParams();
        ResetAntialias();
    }
   
    void SetRenderFinished(bool val)
    {
        if (val == false) {
            renderStatTime = Time.time;
        }

        if(renderFinished == val)
        {
            return;
        }

        renderFinished = val;
        if(renderFinished == true)
        {
            renderTimeElapsed = Time.time - renderStatTime;
        }
       
    }
    void SetFrameFinished(bool val)
    {
        if(frameFinished == val)
        {
            return;
        }
    
        currIter = 0;
        if (guiController.doAntialasing)
        {
            if (currentSample >= maxAntiAliasyncReruns)
            {
                SetRenderFinished(true);
            }
        }
        else
        {
            SetRenderFinished(true);
        }
        
        frameFinished = val;
        if(frameFinished == false)
        {
            frankensteinX = 0;
            frankensteinY = 0;

        }
    }
    void SetFrankensteinFinished(bool val)
    {
        if (!frankensteinRendering)
        {
            SetFrameFinished(val);
          
            frankensteinStepFinished = val;
            return;
        }
        if (frankensteinStepFinished == val)
        {
            return;
        }
        if(val == true)
        {
            if(frankensteinX == frankensteinSteps - 1 && frankensteinY == frankensteinSteps - 1)
            {
                frankensteinStepFinished = true;
                Debug.Log("2");
                SetFrameFinished(true);
            }
            else
            {           
                ResetIterPerCycle();
                reset = true;
                currIter = 0;
                frankensteinX++;
                if (frankensteinX >= frankensteinSteps)
                {
                    frankensteinX = 0;
                    frankensteinY++;
                }

            }
        }
        else
        {
            frankensteinStepFinished = false;
        }
        
    }
    
    void SaveCurrentRenderTextureAsAPng()
    {
        RenderShader.SetBool("_RenderExact", true);
        screenshotTexture = PixelizedShaders.InitializePixelizedTexture(screenshotTexture, ReducedWidth(false), ReducedHeight(false),true);
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

        
        
        switch (precision)
        {
            case Precision.INFINTE:
                FpMultiframeBuffer = new ComputeBuffer(PixelCount() * 2, sizeof(int) * shaderPixelSize);
                break;
            case Precision.DOUBLE:
                doubleMultiFrameRenderBuffer = new ComputeBuffer(PixelCount() * 2, DoublePixelPacket.size);
                break;
            case Precision.FLOAT:
                floatMultiFrameRenderBuffer = new ComputeBuffer(PixelCount() * 2, FloatPixelPacket.size);
                break;

        }


    }
    public void InitializeBuffers()
    {
        IterBuffer = new ComputeBuffer(PixelCount(false), IterPixelPacket.size);
        OldIterBuffer = new ComputeBuffer(PixelCount(false), IterPixelPacket.size);
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        floatDataBuffer = new ComputeBuffer(3, sizeof(float));
        PossionBuffer = new ComputeBuffer(3 * shaderPre, sizeof(int));
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[guiController.currColorPalette].length, 4 * sizeof(float));
        RegenereateFractalComputeBuffers();
    }
 
    public void InitializeValues()
    {
        guiController = gameObject.AddComponent<GuiController>();
        cameraController = gameObject.AddComponent<CameraController>();
        //Fix just for now
        preUpscalePixLvl =pixelizationLevel;
        
        ResetPrecision();


        cameraController.MiddleX.SetDouble(middleX);
        cameraController.MiddleY.SetDouble(middleY);
        cameraController.Scale.SetDouble( length / ReducedWidth(false));

        ResetIterPerCycle();
        addMaterial = new Material(AddShader);
        antialiasLookupTable = antialiasLookupTableSharp;
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
        if (cameraController.screenSizeChanged || lastPixelizationLevel !=pixelizationLevel)
        {
            IterBuffer.Dispose();
            IterBuffer = new ComputeBuffer(PixelCount(false), IterPixelPacket.size);
            if (lastPixelizationLevel !=pixelizationLevel && !upscaling)
            {
                switch (precision)
                {
                    case Precision.INFINTE:
                        PixelizedShaders.HandleZoomPixelization<int>(FpMultiframeBuffer, sizeof(int), lastPixelizationLevel <pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { FpMultiframeBuffer = buffer; }, shaderPixelSize);
                        break;
                    case Precision.DOUBLE:
                        PixelizedShaders.HandleZoomPixelization<DoublePixelPacket>(doubleMultiFrameRenderBuffer, DoublePixelPacket.size, lastPixelizationLevel <pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { doubleMultiFrameRenderBuffer = buffer; });
                        break;
                    case Precision.FLOAT:
                        PixelizedShaders.HandleZoomPixelization<FloatPixelPacket>(floatMultiFrameRenderBuffer, FloatPixelPacket.size, lastPixelizationLevel <pixelizationLevel, GetPixelizationData(), (ComputeBuffer buffer) => { floatMultiFrameRenderBuffer = buffer; });
                        break;

                }
                lastPixelizationLevel = pixelizationLevel;
                SetFrameFinished(false);
            }
            else
            {
                RegenereateFractalComputeBuffers();
                reset = true;
                OnMoveComand();
            }


            





        }

    }
    
    public void SetShadersParameters()
    {

        FixedPointNumber MiddleXToSend = new(cameraController.MiddleX);
        FixedPointNumber MiddleYToSend = new(cameraController.MiddleY);
        if (frankensteinRendering)
        {
            int frankensteinOffsetX = ReducedWidth() * frankensteinX - (ReducedWidth() * (frankensteinSteps - 1)) / 2;
            int frankensteinOffsetY = ReducedHeight() * frankensteinY - (ReducedHeight() * (frankensteinSteps - 1)) / 2;
            FixedPointNumber temp = new(CameraController.cpuPrecision);
            temp.SetDouble(frankensteinOffsetX);
            temp *= cameraController.Scale;
            MiddleXToSend += temp;
            temp.SetDouble(frankensteinOffsetY);
            temp *= cameraController.Scale;
            MiddleYToSend += temp;
        }
        


        for (int i = 0; i < shaderPre; i++)
        {
            TestPosiotnArray[i] = MiddleXToSend.digits[i];
            TestPosiotnArray[shaderPre + i] = MiddleYToSend.digits[i];
            TestPosiotnArray[shaderPre * 2 + i] = cameraController.Scale.digits[i];

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
       
        if(shiftX != 0|| shiftY !=0)
        {
            reset = false;
        }
        Shader.DisableKeyword("FLOAT");
        Shader.DisableKeyword("DOUBLE");
        Shader.DisableKeyword("INFINITE");
        if (precision == Precision.INFINTE)
        {
            Shader.EnableKeyword("INFINITE");
            GPUCode.ResetAllKeywords();
            Shader.EnableKeyword(GPUCode.precisions[precisionLevel].name);

            InfiniteShader.SetBuffer(0, "_FpMultiframeBuffer", FpMultiframeBuffer);
            InfiniteShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
            InfiniteShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
            InfiniteShader.SetInt("_MaxIter", guiController.maxIter);
            InfiniteShader.SetBool("_reset", reset || turboReset);
            InfiniteShader.SetInt("_pixelizationBase", pixelizationBase);
            InfiniteShader.SetInt("_Register", register);
            InfiniteShader.SetInt("_IterPerCycle", IterPerCycle);
            InfiniteShader.SetBuffer(0, "_IterBuffer", IterBuffer);
            InfiniteShader.SetInt("_BailoutRadius", guiController.bailoutRadius);
            InfiniteShader.SetInt("_RenderWidth", ReducedWidth(false));
            InfiniteShader.SetInt("_FrankensteinOffsetX", frankensteinX * ReducedWidth());
            InfiniteShader.SetInt("_FrankensteinOffsetY", frankensteinY * ReducedHeight());

            ResetShader.SetBuffer(0, "_MultiFrameData", FpMultiframeBuffer);
            ResetShader.SetInt("_Precision", GPUCode.precisions[precisionLevel].precision);

            ShiftShader.SetBuffer(0, "_MultiFrameData", FpMultiframeBuffer);
            ShiftShader.SetInt("_Precision", GPUCode.precisions[precisionLevel].precision);
        }
        else if (precision == Precision.DOUBLE || precision == Precision.FLOAT)
        {
            if (precision == Precision.DOUBLE)
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

            FloatShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
            FloatShader.SetInt("_MaxIter", guiController.maxIter);
            FloatShader.SetInt("_Register", register);
            FloatShader.SetInt("_IterPerCycle", IterPerCycle);
            FloatShader.SetBuffer(0, "_IterBuffer", IterBuffer);
            FloatShader.SetInt("_BailoutRadius", guiController.bailoutRadius);
            FloatShader.SetInt("_RenderWidth", ReducedWidth(false));
            FloatShader.SetInt("_FrankensteinOffsetX", frankensteinX * ReducedWidth());
            FloatShader.SetInt("_FrankensteinOffsetY", frankensteinY * ReducedHeight());
       
        }


        ShiftShader.SetInt("_Register", register);
        ShiftShader.SetInt("_ShiftX", shiftX);
        ShiftShader.SetInt("_ShiftY", shiftY);

        ResetShader.SetInt("_Register", register);


        RenderShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        RenderShader.SetBuffer(0, "_OldIterBuffer", OldIterBuffer);
        RenderShader.SetInt("_MaxIter", guiController.maxIter);
        RenderShader.SetFloat("_ColorStrength", guiController.colorStrength);
        RenderShader.SetBool("_Smooth", guiController.smoothGradient);
        RenderShader.SetBool("_Upscaling", upscaling);
        RenderShader.SetBool("_Reset", turboReset);
        RenderShader.SetInt("_Type", MyColoringSystem.colorPalettes[guiController.currColorPalette].gradientType);
        RenderShader.SetInt("_ReduceAmount", OtherFunctions.IntPow(pixelizationBase,Math.Abs(pixelizationLevel)));
        RenderShader.SetBool("_Superresolution",pixelizationLevel < 0);
        RenderShader.SetBool("_RenderExact", false);
        RenderShader.SetInt("_OldPixelWidth", OtherFunctions.IntPow(pixelizationBase, Math.Abs(preUpscalePixLvl)));
        RenderShader.SetBuffer(0, "_Colors", ColorBuffer);
        RenderShader.SetInt("_ColorArrayLength", MyColoringSystem.colorPalettes[guiController.currColorPalette].length);

    }

    public void ResetParams()
    {
        reset = true;
        currentSample = 0;
        currIter = 0;
        upscaling = false;
        SetFrameFinished(false);
        SetFrankensteinFinished(false);
        SetRenderFinished(false);
        ResetIterPerCycle();
        if (frankensteinRendering)
        {
            frankensteinX = 0;
            frankensteinY = 0;
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
            zoomVideo = !zoomVideo;
            guiController.requestedZoomVid = false;
        }
        if (guiController.changedAntialias)
        {
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
            turboReset = true;
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
        if (guiController.RequestedUpscale)
        {
            if (MaxPixelizationLevel() < pixelizationLevel)
            {

                int[] arr = new int[PixelCount(false) * 3];
                IterBuffer.GetData(arr);
                OldIterBuffer.Dispose();
                OldIterBuffer = new ComputeBuffer(PixelCount(false), IterPixelPacket.size);
                OldIterBuffer.SetData(arr);
                IterBuffer.Dispose();


                preUpscalePixLvl =pixelizationLevel;
                pixelizationLevel -= 1;

                IterBuffer = new ComputeBuffer(PixelCount(false), IterPixelPacket.size);
                FixedPointNumber scaleFixer = new(CameraController.cpuPrecision);
                scaleFixer.SetDouble(pixelizationLevel > lastPixelizationLevel ? pixelizationBase : 1.0 / pixelizationBase);
                cameraController.Scale *= scaleFixer;
                upscaling = true;
                SetRenderFinished(false);
                SetFrankensteinFinished(false);
                SetFrameFinished(false);
                currIter = 0;
                OnMoveComand();
            }
            guiController.RequestedUpscale = false;
        }
        if (guiController.pixelizationChange != 0)
        {
            if(MaxPixelizationLevel() < pixelizationLevel + guiController.pixelizationChange)
            {
                if (guiController.pixelizationChange > 0)
                {
                    preUpscalePixLvl = pixelizationLevel;
                }
                lastPixelizationLevel = pixelizationLevel;
                pixelizationLevel += guiController.pixelizationChange;

                upscaling = false;
                currIter = 0;
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
            shiftX = cameraController.shiftX;
            shiftY = cameraController.shiftY;

            ResetAntialias();
            register = (register + 1) % 2;
            OnMoveComand();
            ResetParams();

            cameraController.shiftX = 0;
            cameraController.shiftY = 0;
        }
        guiController.renderWidth= ReducedWidth(false);
        guiController.renderHeight= ReducedHeight(false);
        guiController.currIter= currIter;
        guiController.maxAntiAliasyncReruns= maxAntiAliasyncReruns;
        guiController.currentSample = currentSample;
        guiController.frankensteinX= frankensteinX;
        guiController.frankensteinY= frankensteinY;
        guiController.renderTimeElapsed= renderTimeElapsed;
        guiController.precision = precision;
        guiController.precisionLevel= precisionLevel;
        guiController.renderFinished = renderFinished;

        cameraController.pixelizationBase = pixelizationBase;
        cameraController.pixelizationLevel = pixelizationLevel;


        cameraController.deadZoneRight = guiController.guiOn ? (int)guiController.guiTemplate.sizes.width : 0;

        // end of the temp code



        if (!frankensteinStepFinished && !renderFinished)
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
        if (zoomVideo)
        {
            if (renderFinished)
            {
                SaveCurrentRenderTextureAsAPng();
              
                FixedPointNumber mul = new(CameraController.cpuPrecision);
                mul.SetDouble(pixelizationBase);
                cameraController.Scale *= mul;
                ResetParams();
                if (cameraController.Scale.ToDouble() >= 0.003)
                {
                    zoomVideo = false;
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
        if (!frankensteinStepFinished)
        {
            currIter += IterPerCycle;
        }

        if (currIter > guiController.maxIter)
        {
            SetFrankensteinFinished(true);
        }

    }

    public void AddiitionalTextureRegenerationHandeling()
    {
        currentSample = 0;
  
    }
    public bool ShouldRegerateTexture()
    {
        return lastPixelizationLevel !=pixelizationLevel;
    }
    public void InitializeOtherTextures()
    {
        dummyTexture = PixelizedShaders.InitializePixelizedTexture(dummyTexture, ReducedWidth(), ReducedHeight());
    }
    public void DispatchShaders()
    {
        if (reset || turboReset)
        {
            PixelizedShaders.Dispatch(ResetShader, dummyTexture);
        }
        if (shiftX != 0 || shiftY != 0)
        {
            PixelizedShaders.Dispatch(ShiftShader, dummyTexture);
        }
        switch (precision)
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
        reset = false;
        turboReset = false;
        shiftX = 0;
        shiftY = 0;
    }

    public void BlitTexture(RenderTexture destination)
    {
        
        Antialiasing.BlitWitthAntialiasing(currentSample, frameFinished, renderFinished,
            Input.GetMouseButton(0) && Input.mousePosition.x < Screen.width - guiController.guiTemplate.sizes.width
            , destination, targetTexture, addMaterial,
            () =>
            {
                SetFrameFinished(false);
                SetFrankensteinFinished(false);
                currentSample++;
                reset = true;
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
