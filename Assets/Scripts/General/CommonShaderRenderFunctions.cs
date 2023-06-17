using CommonFunctions;
using UnityEngine;
using System;
namespace CommonShaderRenderFunctions
{
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
                Debug.Log("live");

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