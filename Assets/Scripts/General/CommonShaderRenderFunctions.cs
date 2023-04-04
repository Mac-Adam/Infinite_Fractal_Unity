using CommonFunctions;
using UnityEngine;
using System;
namespace CommonShaderRenderFunctions
{
    struct PixelizationData //shortcut to keep everything condensed
    {
        public int pixelsPerPixel;
        public int lastPixelsPerPixel;
        public int pixelCount;
        public int lastPixelCount;
        public int pixelizationBase;
        public int register;
        public PixelizationData(int pixelsPerPixel,int lastPixelsPerPixel, int pixelCount, int lastPixelCount, int pixelizationBase,int register)
        {
            this.pixelsPerPixel = pixelsPerPixel;
            this.lastPixelsPerPixel = lastPixelsPerPixel;
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
        double offset;
    }
    public struct FloatPixelPacket
    {
        float CurrentZX;
        float CurrentZY;
        uint iter;
        uint finished;
        float offset;
    }

    class PixelizedShaders
    {

        public static RenderTexture InitializePixelizedTexture(RenderTexture texture, int pixelizationBase, int pixelizationLevel, bool additionalCondition = false)
        {
            int reducedWidth = OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel);
            int reducedHeight = OtherFunctions.Reduce(Screen.height, pixelizationBase, pixelizationLevel);
            if (texture == null || texture.width != reducedWidth || texture.height != reducedHeight || additionalCondition)
            {
                
                if (texture != null)
                    texture.Release();

                texture = new RenderTexture(reducedWidth, reducedHeight, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
                {
                    enableRandomWrite = true
                };
                texture.Create();

            }
            return texture;
        }

        public static void Dispatch(ComputeShader RenderShader, ComputeShader DummyShader, RenderTexture targetTexture, RenderTexture dummyTexture, int pixelizationBase, int pixelizationLevel)
        {
            //TODO figure out how to fix the edge glithces when the size is not a multiple of 8
            int RenderThreadGrupsX = Mathf.CeilToInt((float)Screen.width / 8);
            int RenderThreadGrupsY = Mathf.CeilToInt((float)Screen.height / 8);
            int CalculatethreadGroupsX = Mathf.CeilToInt((float)OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel) / 8);
            int CalculatethreadGroupsY = Mathf.CeilToInt((float)OtherFunctions.Reduce(Screen.height, pixelizationBase, pixelizationLevel) / 8);
          
            DummyShader.SetTexture(0, "Result", dummyTexture);
            DummyShader.Dispatch(0, CalculatethreadGroupsX, CalculatethreadGroupsY, 1);


            RenderShader.SetTexture(0, "Result", targetTexture);
            RenderShader.Dispatch(0, RenderThreadGrupsX, RenderThreadGrupsY, 1);

        }

        public static void HandleZoomPixelization<T>(ComputeBuffer Buffer, int sizeofT, bool zoomIn, PixelizationData pixelizationData, Action<ComputeBuffer> setBuffers, int arrayCount = 1)
        {
           
            T[] oldData = new T[pixelizationData.lastPixelCount * arrayCount * 2];

          
            Buffer.GetData(oldData);
            Buffer.Dispose();

            Buffer = new ComputeBuffer(pixelizationData.pixelCount, sizeofT*arrayCount*2);
         
            T[] newData = new T[pixelizationData.pixelCount * arrayCount * 2];
            int oldDataWidth = Screen.width / pixelizationData.lastPixelsPerPixel;
            int oldDataHeight = Screen.height / pixelizationData.lastPixelsPerPixel;
            int newDataWidth = Screen.width / pixelizationData.pixelsPerPixel;
            int newDataHeight = Screen.height / pixelizationData.pixelsPerPixel;

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
        public static void BlitWitthAntialiasing(uint currentSample,bool frameFinished,RenderTexture destination,RenderTexture renderedTexture,Material addMaterial,Action NewFrameCallback)
        {
            if ((currentSample == 0 && !frameFinished) || Input.GetMouseButton(0))
            {
                Graphics.Blit(renderedTexture, destination);


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