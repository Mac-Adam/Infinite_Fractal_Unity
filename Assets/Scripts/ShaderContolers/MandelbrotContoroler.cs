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
    public ComputeShader ZoomShader;
    public Shader AddShader;


    // the render texture
    RenderTexture renderTexture;
    // texture the calculations are done on (can be different size than the render texture)
    RenderTexture dummyTexture;
    RenderTexture screenshotTexture;

    Material addMaterial;

    //used to transfer position and scale to the shader
    ComputeBuffer dataBuffer;
    //used by the gpu to store Data in between frames
    ComputeBuffer multiFrameRenderBuffer;


    //RenderShader
    ComputeBuffer IterBuffer;
    ComputeBuffer OldIterBuffer; //used for upscaling

    ComputeBuffer ColorBuffer;

    //Not yet sure where this should be

    GuiController guiController;
    CameraController cameraController;
   
    //not yet sure if needed 
    int preUpscalePixLvl;

    //Controls
    double length = 4.0f;
    double middleX = -1.0f;
    double middleY = 0.0f;

    //constanst are a starting point the other one is dynamicly set based on hardware capabilities
    int[] itersPerCycle = new int[] { 50, 10, 1 };
    const float minTargetFramerate = 60;
    const float maxTagretFramerate = 200;

    Settings settings = new(true);
    DynamicSettings dynamicSettings = new();

    void ResetIterPerCycle()
    {
        settings.iterPerCycle = Math.Clamp( itersPerCycle[(int)settings.precision]*OtherFunctions.IntPow(2,settings.frankensteinSteps),0,guiController.maxIter);
    }
    void SetSPrecision(int val)
    {
        val = Math.Clamp(val, 0, GPUCode.precisions.Length - 1);
        settings.precisionLevel = val;
        DisposeBuffers();
        InitializeBuffers();
        ResetParams();
    }


    void ResetAntialias()
    {
        settings.currentSample = 0;
        dynamicSettings.currIter = 0;
        SetFrameFinished(false);
        SetStepFinished(false);
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
        //Sonmething forced a rerender before it was finished
        if (val == false) {
            dynamicSettings.renderStatTime = Time.time;
        }
        //Nothing chagned
        if(dynamicSettings.renderFinished == val)
        {
            return;
        }

        dynamicSettings.renderFinished = val;
        //Since the render is finished the render time can be updated
        if(dynamicSettings.renderFinished == true)
        {
            dynamicSettings.renderTimeElapsed = Time.time - dynamicSettings.renderStatTime;
        }
       
    }
    void SetFrameFinished(bool val)
    {
        //Nothing changed
        if(dynamicSettings.frameFinished == val)
        {
            return;
        }

        dynamicSettings.currIter = 0;
        //Check if the whole render is finished
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
        //New frame requested needs to reset frankenstein steps
        if(dynamicSettings.frameFinished == false)
        {
            settings.frankensteinX = 0;
            settings.frankensteinY = 0;
        }
    }
    void SetStepFinished(bool val)
    {
        //If not doing frankenstein renderig those two are identical
        if (!settings.frankensteinRendering)
        {
            SetFrameFinished(val);

            dynamicSettings.stepFinished = val;
            return;
        }
        //Nothing changed
        if (dynamicSettings.stepFinished == val)
        {
            return;
        }

        //Step finished 
        if(val == true)
        {
            //Whole frame is done
            if(settings.frankensteinX == settings.frankensteinSteps - 1 && settings.frankensteinY == settings.frankensteinSteps - 1)
            {
                dynamicSettings.stepFinished = true;
                SetFrameFinished(true);
            }
            else //Need to do more work
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
            dynamicSettings.stepFinished = false;
        }
        
    }
    
    void SaveCurrentRenderTextureAsAPng()
    {
        RenderShader.SetBool("_RenderExact", true);
        screenshotTexture = PixelizedShaders.InitializePixelizedTexture(screenshotTexture, settings.ReducedWidth(false), settings.ReducedHeight(false),true);
        PixelizedShaders.Dispatch(RenderShader, screenshotTexture);
        OtherFunctions.SaveRenderTextureToFile(screenshotTexture, DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss-tt-fff"));
    }

    //Iter buffer can't be handled in this function since when upscaling it needs to be renewed without Dispose
    //(Due to both OldIterBuffer and IterBuffer pointing to the same buffer
    void RegenereateFractalComputeBuffers()
    {
        if (multiFrameRenderBuffer != null)
        {
            multiFrameRenderBuffer.Dispose();
        }
        if(dataBuffer != null)
        {
            dataBuffer.Dispose();
        }
        switch (settings.precision)
        {
            case Precision.INFINTE:
                multiFrameRenderBuffer = new ComputeBuffer(settings.PixelCount() * 2, sizeof(int) * settings.GetShaderPixelSize());
                dataBuffer = new ComputeBuffer(3 * settings.GetShaderPre(), sizeof(int));
                break;
            case Precision.DOUBLE:
                multiFrameRenderBuffer = new ComputeBuffer(settings.PixelCount() * 2, DoublePixelPacket.size);
                dataBuffer = new ComputeBuffer(3, sizeof(double));
                break;
            case Precision.FLOAT:
                multiFrameRenderBuffer = new ComputeBuffer(settings.PixelCount() * 2, FloatPixelPacket.size);
                dataBuffer = new ComputeBuffer(3, sizeof(float));
                break;

        }


    }
    public void InitializeBuffers()
    {
        OldIterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
        IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
        dataBuffer = new ComputeBuffer(3, sizeof(float));
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[guiController.currColorPalette].length, 4 * sizeof(float));
        RegenereateFractalComputeBuffers();
    }
 
    public void InitializeValues()
    {
        guiController = gameObject.AddComponent<GuiController>();
        cameraController = gameObject.AddComponent<CameraController>();
        //Fix just for now
        preUpscalePixLvl = settings.pixelizationLevel;

        cameraController.MiddleX.SetDouble(middleX);
        cameraController.MiddleY.SetDouble(middleY);
        cameraController.Scale.SetDouble( length / settings.ReducedWidth(false));

        ResetIterPerCycle();
        addMaterial = new Material(AddShader);
    }
   
    public void DisposeBuffers()
    {
        IterBuffer.Dispose();
        OldIterBuffer.Dispose();
        dataBuffer.Dispose();
      
        ColorBuffer.Dispose();

        multiFrameRenderBuffer.Dispose();
       

    }
    public void DestroyTextures()
    {
        Destroy(renderTexture);
        Destroy(dummyTexture);
        Destroy(screenshotTexture);
    }
    public void SetShadersParameters()
    {
        //calculate render place
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

        ColorBuffer.SetData(MyColoringSystem.colorPalettes[guiController.currColorPalette].colors);
        //If the screen moved the reset is a false alarm unless doing frankenstein rendering
        if((dynamicSettings.shiftX != 0|| dynamicSettings.shiftY !=0) &&!settings.frankensteinRendering)
        {
            dynamicSettings.reset = false;
        }
        ComputeShader shader = settings.precision == Precision.INFINTE ? InfiniteShader:FloatShader;
        GPUCode.ResetAllKeywords();
        //Set the render place wiht proper precision
        if (settings.precision == Precision.INFINTE)
        {
            int[] data = new int[3 * settings.GetShaderPre()];
            for (int i = 0; i < settings.GetShaderPre(); i++)
            {
                data[i] = MiddleXToSend.digits[i];
                data[settings.GetShaderPre() + i] = MiddleYToSend.digits[i];
                data[settings.GetShaderPre() * 2 + i] = cameraController.Scale.digits[i];

            }

            dataBuffer.SetData(data);

            Shader.EnableKeyword("INFINITE");     
            Shader.EnableKeyword(GPUCode.precisions[settings.precisionLevel].name);

            ResetShader.SetInt("_Precision", GPUCode.precisions[settings.precisionLevel].precision);

            ShiftShader.SetInt("_Precision", GPUCode.precisions[settings.precisionLevel].precision);
        }
        else if (settings.precision == Precision.DOUBLE || settings.precision == Precision.FLOAT)
        {
            if (settings.precision == Precision.DOUBLE)
            {
                double[] data = {
                    cameraController.Scale.ToDouble(),
                    MiddleXToSend.ToDouble(),
                    MiddleYToSend.ToDouble() 
                };
                dataBuffer.SetData(data);
                Shader.EnableKeyword("DOUBLE");
            }
            else
            {
                float[] data = { 
                    (float)cameraController.Scale.ToDouble(),
                    (float)MiddleXToSend.ToDouble(),
                    (float)MiddleYToSend.ToDouble()
                };
                dataBuffer.SetData(data);
                Shader.EnableKeyword("FLOAT");       
            }

        }
        ResetShader.SetBuffer(0, "_MultiFrameData", multiFrameRenderBuffer);
        ShiftShader.SetBuffer(0, "_MultiFrameData", multiFrameRenderBuffer);

        shader.SetBuffer(0, "_DataBuffer", dataBuffer);
        shader.SetBuffer(0, "_MultiFrameData", multiFrameRenderBuffer);
        shader.SetVector("_PixelOffset", settings.antialiasLookupTable[settings.currentSample % settings.antialiasLookupTable.Length]);
        shader.SetInt("_MaxIter", guiController.maxIter);
        shader.SetInt("_Register", settings.register);
        shader.SetInt("_IterPerCycle", settings.iterPerCycle);
        shader.SetBuffer(0, "_IterBuffer", IterBuffer);
        shader.SetInt("_BailoutRadius", guiController.bailoutRadius);
        shader.SetInt("_RenderWidth", settings.ReducedWidth(false));
        shader.SetInt("_FrankensteinOffsetX", settings.frankensteinX * settings.ReducedWidth());
        shader.SetInt("_FrankensteinOffsetY", settings.frankensteinY * settings.ReducedHeight());



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
        SetStepFinished(false);
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
        //While rendergin try to adjust the amout of iters per cycle to stay in the sweet spot between 
        //quick render and acceptable fps
        //In short at the beging of a frame the gpu needs to read a bunch of data ( quite slow)
        //Then it does (iterPerCycle) calculations
        //the more calculations per frame it needs to read the data less times
        //to much and the fps drops below acceptable level and the app is laggy
        //TODO: chceck if the fps has beed externaly limited (for example on a laptop working on batery)
        //In that case the iterPerCycle quickly drops to 1, and the frame takes ages to finish.
        if (!dynamicSettings.stepFinished && !dynamicSettings.renderFinished)
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
        //check how many zeros are on the begign of the scale variable
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
        //set the GPU precision acording to the needed anout
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
        //the values where the precision swithces are arbitrtary and probably coud be made tighter for a tiny increase in performance
        ////but this is not a priority and those numbers work allways 
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
    public void HandleFlags()
    {
        //This part of the code handles all the requests from the other controlers
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
            guiController.lastColorPalette = guiController.currColorPalette;
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
            if(settings.pixelizationLevel + guiController.requestedFrankensteinLevel <= settings.MaxPixelizationLevel())
            {
                //the buffers won't handle it
                guiController.changedFrankenstein = false;
            }
            else
            {
                settings.frankensteinSteps = OtherFunctions.IntPow(2, guiController.requestedFrankensteinLevel);
                settings.frankensteinRendering = settings.frankensteinSteps != 1;
                settings.frankensteinX = 0;
                settings.frankensteinY = 0;
                RegenereateFractalComputeBuffers();
                ResetParams();
                dynamicSettings.turboReset = true;
                guiController.changedFrankenstein = false;
            }
    
        }

        if (guiController.requestedUpscale)
        {
            //There is room for more pixels
            if (settings.MaxPixelizationLevel() < settings.pixelizationLevel)
            {
                settings.lastPixelizationLevel = settings.pixelizationLevel;
                preUpscalePixLvl = settings.pixelizationLevel;
                settings.pixelizationLevel -= 1;

                //Handle buffers
                OldIterBuffer.Dispose();
                OldIterBuffer = IterBuffer;
                RegenereateFractalComputeBuffers();
                IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);

                //make sure the scale is ok
                FixedPointNumber scaleFixer = new(CameraController.cpuPrecision);
                scaleFixer.SetDouble(settings.pixelizationLevel > settings.lastPixelizationLevel ? settings.pixelizationBase : 1.0 / settings.pixelizationBase);
                cameraController.Scale *= scaleFixer;

                //Make sure the render starts properly
                settings.upscaling = true;
                SetRenderFinished(false);
                SetStepFinished(false);
                SetFrameFinished(false);
                dynamicSettings.currIter = 0;
                OnMoveComand();
            }
            guiController.requestedUpscale = false;
        }

        if (guiController.requestedDownscale)
        {
            settings.lastPixelizationLevel = settings.pixelizationLevel;
            settings.pixelizationLevel += 1;

            //Handle buffers
            RegenereateFractalComputeBuffers();
            IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);

            //make sure the scale is ok
            FixedPointNumber scaleFixer = new(CameraController.cpuPrecision);
            scaleFixer.SetDouble(settings.pixelizationLevel > settings.lastPixelizationLevel ? settings.pixelizationBase : 1.0 / settings.pixelizationBase);
            cameraController.Scale *= scaleFixer;

            //Make sure the render starts properly
            SetRenderFinished(false);
            SetStepFinished(false);
            SetFrameFinished(false);
            dynamicSettings.currIter = 0;
            OnMoveComand();
          
            guiController.requestedDownscale = false;
        }

        if (guiController.pixelizationChange != 0)
        {
            if (!settings.frankensteinRendering && settings.MaxPixelizationLevel() <= settings.pixelizationLevel + guiController.pixelizationChange)
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
                //Dispose old buffer
                IterBuffer.Dispose();
                IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
                //setup Shader
                GPUCode.ResetAllKeywords();
                Shader.EnableKeyword(settings.lastPixelizationLevel < settings.pixelizationLevel ? "IN" : "OUT");
                ComputeBuffer temp;
                switch (settings.precision)
                {
                    case Precision.INFINTE:
                        ZoomShader.SetInt("_Precision", GPUCode.precisions[settings.precisionLevel].precision);
                        Shader.EnableKeyword("INFINITE");
                        temp = new ComputeBuffer(settings.PixelCount() * 2, sizeof(int) * settings.GetShaderPixelSize());
                        break;
                    case Precision.DOUBLE:
                        Shader.EnableKeyword("DOUBLE");
                        temp = new ComputeBuffer(settings.PixelCount() * 2, DoublePixelPacket.size);
                        break;
                    default: // idk why i need to do this thike that :/
                        Shader.EnableKeyword("FLOAT");
                        temp = new ComputeBuffer(settings.PixelCount() * 2, FloatPixelPacket.size);
                        break;
                }
                ZoomShader.SetBuffer(0, "_MultiFrameData", temp);
                ZoomShader.SetBuffer(0, "_OldMultiFrameData", multiFrameRenderBuffer);
                ZoomShader.SetInt("_Register", settings.register);
                ZoomShader.SetInt("_PixelizationBase", settings.pixelizationBase);
                if (settings.lastPixelizationLevel < settings.pixelizationLevel)
                {
                    dummyTexture = PixelizedShaders.InitializePixelizedTexture(
                        dummyTexture,
                        settings.ReducedWidth(),
                        settings.ReducedHeight());
                }
                else
                {
                    dummyTexture = PixelizedShaders.InitializePixelizedTexture(
                        dummyTexture,
                        settings.LastReducedWidth(),
                        settings.LastReducedHeight());

                }
                //Dispatch shader to do the work
                //the dummy texture should be the smaller size, so no work is wasted
                PixelizedShaders.Dispatch(ZoomShader, dummyTexture);
                multiFrameRenderBuffer.Dispose();
                multiFrameRenderBuffer = temp;

                settings.lastPixelizationLevel = settings.pixelizationLevel;
                SetFrameFinished(false);

            }
            guiController.pixelizationChange = 0;
        }

        if (cameraController.scrollMoved)
        {
            OnMoveComand();
            ResetParams();

            cameraController.scrollMoved = false;
        }

        if (cameraController.shiftX != 0 || cameraController.shiftY != 0)
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

        if (cameraController.screenSizeChanged)
        {

            if (IterBuffer != null)
            {
                IterBuffer.Dispose();
            }
            IterBuffer = new ComputeBuffer(settings.PixelCount(false), IterPixelPacket.size);
            RegenereateFractalComputeBuffers();
            dynamicSettings.reset = true;
            OnMoveComand();
        }

        guiController.settings = settings;
        guiController.dynamicSettings = dynamicSettings;

        cameraController.settings = settings;

        cameraController.deadZoneRight = guiController.guiOn ? (int)guiController.guiTemplate.sizes.width : 0;

    }
    //This function handles when the GPU has finished the frame
    public void HandleAntialias()
    {

        if (!dynamicSettings.stepFinished)
        {
            dynamicSettings.currIter += settings.iterPerCycle;
        }

        if (dynamicSettings.currIter > guiController.maxIter)
        {
            SetStepFinished(true);
        }

    }
    public void DispatchShaders()
    {
        if (dynamicSettings.reset || dynamicSettings.turboReset)
        {
            PixelizedShaders.Dispatch(ResetShader, dummyTexture);
            if (settings.frankensteinRendering)
            {
                ResetShader.SetInt("_Register", settings.register == 1? 0 : 1);
            }
        }
        if ((dynamicSettings.shiftX != 0 || dynamicSettings.shiftY != 0 )&& !settings.frankensteinRendering)
        {
            PixelizedShaders.Dispatch(ShiftShader, dummyTexture);
        }
        if (dynamicSettings.turboReset)
        {
            ResetShader.SetInt("_Register", 0);
            ResetShader.SetBuffer(0, "_MultiFrameData", IterBuffer);
            Shader.DisableKeyword("FLOAT");
            Shader.DisableKeyword("DOUBLE");
            Shader.DisableKeyword("INFINITE");
            Shader.EnableKeyword("ITER");
            //using screenshotTexture because the size of it is the same, so there won't be any conficts
            screenshotTexture = PixelizedShaders.InitializePixelizedTexture(screenshotTexture, settings.ReducedWidth(false), settings.ReducedHeight(false), true);
            PixelizedShaders.Dispatch(ResetShader, screenshotTexture);

        }
    
        Shader.DisableKeyword("ITER");
        switch (settings.precision)
        {
            case Precision.INFINTE:
                Shader.EnableKeyword("INFINITE");
                break;
            case Precision.DOUBLE:
                Shader.EnableKeyword("DOUBLE");
                break;
            case Precision.FLOAT:
                Shader.EnableKeyword("FLOAT");
                break;
        }

        Debug.Log($"{dummyTexture.width},{IterBuffer.count},{multiFrameRenderBuffer.count}");
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
        PixelizedShaders.Dispatch(RenderShader, renderTexture);
        dynamicSettings.reset = false;
        dynamicSettings.turboReset = false;
        dynamicSettings.shiftX = 0;
        dynamicSettings.shiftY = 0;
    }

    public void BlitTexture(RenderTexture destination)
    {
        //If not dooing antialiasting allways display live
        if (!settings.doAntialasing)
        {
            Graphics.Blit(renderTexture, destination);
            return;
        }

        //otherwise display live only if none renders were finished
        if (settings.currentSample == 0 && !dynamicSettings.frameFinished)
        {
            Graphics.Blit(renderTexture, destination);

        }//if the render is finished no need to do anything
        else if (dynamicSettings.renderFinished)
        {
            return;
        }// if the frame is finished continue to the next sample
        else if (dynamicSettings.frameFinished)
        {
            //this basicly combines the image with those already on the screen in a way that the 
            //result is an average of them
            addMaterial.SetFloat("_Sample", settings.currentSample);
            Graphics.Blit(renderTexture, destination, addMaterial);
            //sets up next render
            SetFrameFinished(false);
            SetStepFinished(false);
            settings.currentSample++;
            dynamicSettings.reset = true;
        }
    }


    private void Awake()
    {
        Application.targetFrameRate = -1;
        InitializeValues();
        InitializeBuffers();
        ResetParams();
    }
    //Some of the code executed here needs to be executer after the other modules have finishied their code
    void LateUpdate()
    {
        HandleFlags();
        HandleAntialias();
        AutomaticParametersChange();
    }
    private void OnDestroy()
    {
        DisposeBuffers();
        DestroyTextures();
    }

    private void InitializeRenderTextures()
    {
        renderTexture = PixelizedShaders.InitializePixelizedTexture(
            renderTexture,
            Screen.width,
            Screen.height,
            settings.lastPixelizationLevel != settings.pixelizationLevel,
            ()=>settings.currentSample = 0);

        dummyTexture = PixelizedShaders.InitializePixelizedTexture(
            dummyTexture, 
            settings.ReducedWidth(),
            settings.ReducedHeight());
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
