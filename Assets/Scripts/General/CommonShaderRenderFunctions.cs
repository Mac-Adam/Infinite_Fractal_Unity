using CommonFunctions;
using UnityEngine;
using System;
namespace CommonShaderRenderFunctions
{
    class PixelizedShaders
    {

        public static RenderTexture InitializePixelizedTexture(RenderTexture texture, int pixelizationBase, int pixelizationLevel, bool additionalCondition = false)
        {

            if (texture == null || texture.width != Screen.width / MathFunctions.IntPow(pixelizationBase, pixelizationLevel) || texture.height != Screen.height / MathFunctions.IntPow(pixelizationBase, pixelizationLevel) || additionalCondition)
            {

                if (texture != null)
                    texture.Release();

                texture = new RenderTexture(Screen.width / MathFunctions.IntPow(pixelizationBase, pixelizationLevel), Screen.height / MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 0,
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
            int RenderThreadGrupsX = Mathf.CeilToInt(Screen.width / 8);
            int RenderThreadGrupsY = Mathf.CeilToInt(Screen.height / 8);
            int CalculatethreadGroupsX = Mathf.CeilToInt(Screen.width / (8 * MathFunctions.IntPow(pixelizationBase, pixelizationLevel)));
            int CalculatethreadGroupsY = Mathf.CeilToInt(Screen.height / (8 * MathFunctions.IntPow(pixelizationBase, pixelizationLevel)));


            DummyShader.SetTexture(0, "Result", dummyTexture);
            DummyShader.Dispatch(0, CalculatethreadGroupsX, CalculatethreadGroupsY, 1);


            RenderShader.SetTexture(0, "Result", targetTexture);
            RenderShader.Dispatch(0, RenderThreadGrupsX, RenderThreadGrupsY, 1);

        }

        public static void HandleZoomPixelization<T>(ComputeBuffer oldBuffer,ComputeBuffer newBuffer,int sizeofT,bool zoomIn, int pixelsPerPixel,int lastPixelsPerPixel,int lastPixelCount, int pixelCount, int pixelizationBase, int register,Action<bool> resetSetCallback,Action<ComputeBuffer,ComputeBuffer> setBuffers)
        {
            T[] oldArr = new T[lastPixelCount * 2];
            T[] newArr;
            int dataWidth;
            int dataHeigth;
            int otherWidth;
            int otherHeigth;
            if (zoomIn)//zoom in
            {
                newArr = new T[pixelCount]; //Not yet sure but I think it should be *2
                dataWidth = Screen.width / pixelsPerPixel;
                dataHeigth = Screen.height / pixelsPerPixel;

                otherWidth = Screen.width / lastPixelsPerPixel;
                otherHeigth = Screen.height / lastPixelsPerPixel;

            }
            else //zoom out
            {

                newArr = new T[pixelCount * 2];


                otherWidth = Screen.width / pixelsPerPixel;
                otherHeigth = Screen.height / pixelsPerPixel;


                dataWidth = Screen.width / lastPixelsPerPixel;
                dataHeigth = Screen.height / lastPixelsPerPixel;
            }

            oldBuffer.GetData(oldArr);

            int cornerX = dataWidth * (pixelizationBase - 1) / 2;
            int cornerY = dataHeigth * (pixelizationBase - 1) / 2;
            for (int x = 0; x < dataWidth; x++)
            {
                for (int y = 0; y < dataHeigth; y++)
                {

                    int smallId = x + y * dataWidth;
                    int bigId = x + cornerX + (y + cornerY) * otherWidth;
                    bigId += otherWidth * otherHeigth * register;
                    if (!zoomIn)
                    {
                        smallId += dataWidth * dataHeigth * register;
                    }


                  
                    if (zoomIn)
                    {
                        newArr[bigId] = oldArr[smallId];
                    }
                    else
                    {
                        newArr[smallId] = oldArr[bigId];
                    }
                    
                }
            }
            oldBuffer.Dispose();
            oldBuffer = new ComputeBuffer(pixelCount * 2, sizeofT);

            if (!zoomIn)
            {
                resetSetCallback(false);
                oldBuffer.SetData(newArr);
            }
            else
            {
                newBuffer.SetData(newArr);
            }
            setBuffers(oldBuffer, newBuffer);
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