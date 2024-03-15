using System;
using UnityEngine;
using FixedPointNumberSystem;
using CommonFunctions;
namespace CommonShaderRenderFunctions
{

    public enum Precision { FLOAT = 0, DOUBLE = 1, INFINTE = 2 };
    struct PixelizationData //shortcut to keep everything condensed
    {
        public int reducedWidth;
        public int reducedHeight;
        public int lastReducedWidth;
        public int lastReducedHeight;
        public int pixelCount;
        public int lastPixelCount;
        public int pixelizationBase;
        public int register;
        public PixelizationData(int reducedWidth, int reducedHeight, int lastReducedWidth,int lastReducedHeight, int pixelCount, int lastPixelCount, int pixelizationBase, int register)
        {
            this.reducedWidth = reducedWidth;
            this.reducedHeight = reducedHeight;
            this.lastReducedWidth = lastReducedWidth;
            this.lastReducedHeight = lastReducedHeight;
            this.pixelCount = pixelCount;
            this.lastPixelCount = lastPixelCount;
            this.pixelizationBase = pixelizationBase;
            this.register = register;
        }

    }

    //In order to keep everythong condensed this stuct is used to keep tack of every value that describes the state of renderer
    // this takes into account only the values needed to describe equelibriun state (shiftX,etc. are excluded)
    public struct Settings
    {
        public bool upscaling;
        public int register;
        public bool zoomVideo;
        public int pixelizationBase;
        public int pixelizationLevel;
        public int lastPixelizationLevel;
        public uint currentSample;
        public int maxAntiAliasyncReruns;
        public Vector2[] antialiasLookupTable;
        public bool frankensteinRendering;
        public int frankensteinSteps;
        public int frankensteinX;
        public int frankensteinY;
        public Precision precision;
        public int precisionLevel;
        public int iterPerCycle;
        public bool doAntialasing;

        public Settings(
            bool upscaling,
            int register,
            bool zoomVideo,
            int pixelizationBase,
            int pixelizationLevel,
            int lastPixelizationLevel,
            uint currentSample,
            int maxAntiAliasyncReruns,
            Vector2[] antialiasLookupTable,
            bool frankensteinRendering,
            int frankensteinSteps,
            int frankensteinX,
            int frankensteinY,
            Precision precision,
            int precisionLevel,
            int iterPerCycle,
            bool doAntialasing
            )
        {
            this.upscaling = upscaling;
            this.register = register;
            this.zoomVideo = zoomVideo;
            this.pixelizationBase = pixelizationBase;
            this.pixelizationLevel = pixelizationLevel;
            this.lastPixelizationLevel = lastPixelizationLevel;
            this.currentSample = currentSample;
            this.maxAntiAliasyncReruns = maxAntiAliasyncReruns;
            this.antialiasLookupTable = antialiasLookupTable;
            this.frankensteinRendering = frankensteinRendering;
            this.frankensteinSteps = frankensteinSteps;
            this.frankensteinX = frankensteinX;
            this.frankensteinY = frankensteinY;
            this.precision = precision;
            this.precisionLevel = precisionLevel;
            this.iterPerCycle = iterPerCycle;
            this.doAntialasing = doAntialasing;
;
        }



        public int GetShaderPre()
        {
            return GPUCode.precisions[precisionLevel].precision;
        }
        public int GetShaderPixelSize()
        {
            return  2 * GetShaderPre() + 3;
        }


        public int FrankensteinCorrection()
        {
            if (frankensteinRendering)
            {
                return frankensteinSteps;
            }
            return 1;

        }
        public int PixelCount(bool frankenstein = true)
        {
            return ReducedHeight(frankenstein) * ReducedWidth(frankenstein);

        }
        public int ReducedWidth(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel);

        }
        public int ReducedHeight(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.height, pixelizationBase, pixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.height, pixelizationBase, pixelizationLevel);
        }

        public int LastPixelCount( bool frankenstein = true)
        {
            return LastReducedHeight( frankenstein) * LastReducedWidth( frankenstein);
        }
        public  int LastReducedWidth(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.width, pixelizationBase, lastPixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.width, pixelizationBase, lastPixelizationLevel);
        }
        public  int LastReducedHeight(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.height, pixelizationBase, lastPixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.height, pixelizationBase, lastPixelizationLevel);
        }


        public int MaxPixelizationLevel()
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
                        bufferSize = 2 * pixelCount * sizeof(int) * GetShaderPixelSize();
                        break;
                }

            } while (bufferSize <= PixelizedShaders.MAXBYTESPERBUFFER * FrankensteinCorrection() && iterSize <= PixelizedShaders.MAXBYTESPERBUFFER); ;
            return max + 1;
        }

    }
    //values keept in this struct change frequently
    //Every value is prone to changes so the devision is arbitrary
    public struct DynamicSettings 
    {
        public int currIter;
        public bool reset;
        public bool turboReset;
        public int shiftX;
        public int shiftY;
        public bool renderFinished;
        public bool frameFinished;
        public bool frankensteinStepFinished;
        public float renderStatTime;
        public float renderTimeElapsed;

        public DynamicSettings(
            int currIter,
            bool reset,
            bool turboReset,
            int shiftX,
            int shiftY,
            bool renderFinished,
            bool frameFinished,
            bool frankensteinStepFinished,
            float renderStatTime,
            float renderTimeElapsed
            )
        {
            this.currIter = currIter;
            this.reset = reset;
            this.turboReset = turboReset;
            this.shiftX = shiftX;
            this.shiftY = shiftY;
            this.renderFinished = renderFinished;
            this.frameFinished = frameFinished;
            this.frankensteinStepFinished = frankensteinStepFinished;
            this.renderStatTime = renderStatTime;
            this.renderTimeElapsed = renderTimeElapsed;
        }
    }

    public struct DoublePixelPacket
    {
        double CurrentZX;
        double CurrentZY;
        uint iter;
        uint finished;
        float offset;
        //for reasosns I don't fully understand sizeof(float) must be mutiplied by 2, otherwise it doesn't work
        public static int size = sizeof(double) * 2 + sizeof(uint) * 2 + sizeof(float) * 2;
    }
    public struct FloatPixelPacket
    {
        float CurrentZX;
        float CurrentZY;
        uint iter;
        uint finished;
        float offset;
        public static int size = sizeof(float) * 3 + sizeof(uint) * 2;
    }
    public struct IterPixelPacket
    {
        int iter;
        int finished;
        float rest;
        public static int size = sizeof(int) * 2 + sizeof(float);
    }

    class PixelizedShaders
    {
        public const uint MAXBYTESPERBUFFER = 2147483648;
        public static RenderTexture InitializePixelizedTexture(RenderTexture texture, int reducedWidth, int reducedHeight, bool smallSize = false)
        {
            if (texture == null || texture.width != reducedWidth || texture.height != reducedHeight)
            {
                
                if (texture != null)
                    texture.Release();

                texture = new RenderTexture(reducedWidth, reducedHeight, 0,
                   smallSize ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGBFloat
                   , RenderTextureReadWrite.Linear)
                {
                    enableRandomWrite = true
                };
                texture.Create();

            }
            return texture;
        }

        public static void Dispatch(ComputeShader shader, RenderTexture texture)
        {
            //TODO figure out how to fix the edge glithces when the size is not a multiple of 8
            int ThreadGrupsX = Mathf.CeilToInt((float)texture.width / 8);
            int ThreadGrupsY = Mathf.CeilToInt((float)texture.height / 8);
            shader.SetTexture(0, "Result", texture);
            shader.Dispatch(0, ThreadGrupsX, ThreadGrupsY, 1);

        }

        public static void HandleZoomPixelization<T>(ComputeBuffer Buffer, int sizeofT, bool zoomIn, PixelizationData pixelizationData, Action<ComputeBuffer> setBuffers, int arrayCount = 1)
        {
           
            T[] oldData = new T[pixelizationData.lastPixelCount * arrayCount * 2];
            Debug.Log($"Last: {pixelizationData.lastPixelCount}, This: {pixelizationData.pixelCount}");
          
            Buffer.GetData(oldData);
            Buffer.Dispose();

            Buffer = new ComputeBuffer(pixelizationData.pixelCount, sizeofT*arrayCount*2);
         
            T[] newData = new T[pixelizationData.pixelCount * arrayCount * 2];
            int oldDataWidth = pixelizationData.lastReducedWidth;
            int oldDataHeight = pixelizationData.lastReducedHeight;
            int newDataWidth = pixelizationData.reducedWidth;
            int newDataHeight = pixelizationData.reducedHeight;

            int cornerX;
            int cornerY;
            int oldIdx;
            int newIdx;
            int yLoops;
            int xLoops;

            if (zoomIn)
            {
                cornerX = (oldDataWidth - oldDataWidth / pixelizationData.pixelizationBase) / 2;
                cornerY = (oldDataHeight - oldDataHeight / pixelizationData.pixelizationBase) / 2;

                oldIdx = cornerY * oldDataWidth + cornerX;
                newIdx = 0;
              

                yLoops = newDataHeight;
                xLoops = newDataWidth;

            }
            else
            {
                cornerX = (newDataWidth - newDataWidth / pixelizationData.pixelizationBase) / 2;
                cornerY = (newDataHeight - newDataHeight / pixelizationData.pixelizationBase) / 2;
                
                oldIdx = 0;
                newIdx = cornerY * newDataWidth + cornerX;
           
                yLoops = oldDataHeight;
                xLoops = oldDataWidth;

            }
            oldIdx += oldDataWidth * oldDataHeight * pixelizationData.register;
            newIdx += newDataWidth * newDataHeight * pixelizationData.register;
            oldIdx *= arrayCount;
            newIdx *= arrayCount;
            for (int y = 0; y < yLoops; y++)
            {
                for (int x = 0; x < xLoops; x++)
                {

                    for (int i = 0; i < arrayCount; i++)
                    {
                        newData[newIdx+i] = oldData[oldIdx+i];
                    }

               
                    newIdx += arrayCount;
                    oldIdx += arrayCount;
                }

                if (zoomIn)
                {
                    oldIdx += (oldDataWidth - newDataWidth) * arrayCount;
                }
                else
                {
                    newIdx += (newDataWidth - oldDataWidth) * arrayCount;
                }
                        
            }
                
           
           
            Buffer.SetData(newData);
            setBuffers(Buffer);

        }

    }
    class Antialiasing
    {
        public static void BlitWitthAntialiasing(uint currentSample,bool frameFinished, bool renderFinished, bool liveOverride,RenderTexture destination,RenderTexture renderedTexture,Material addMaterial,Action NewFrameCallback)
        {
            
            if ((currentSample == 0 && !frameFinished) || liveOverride)
            {
                Graphics.Blit(renderedTexture, destination);

            }else if (renderFinished)
            {
                return;
            }
            else if (frameFinished)
            {

                addMaterial.SetFloat("_Sample", currentSample);
                Graphics.Blit(renderedTexture, destination, addMaterial);
                NewFrameCallback.Invoke();
               
            }

        }


    }





}