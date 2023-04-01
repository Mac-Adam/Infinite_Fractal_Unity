using UnityEngine;
using System.IO;
namespace CommonFunctions
{
    class MathFunctions
    {
        public static int IntPow(int baseNum, int exponent)
        {
            int res;
            if (exponent == 0)
            {
                res = 1;
            }
            else
            {
                res = baseNum;
            }
            for (int i = 1; i < exponent; i++)
            {
                res *= baseNum;
            }
            return res;
        }
    }

    public class OtherFunctions
    {
        //Not my code :/
        //note to self:
        //this function grabs the texture from the gpu converts it to Texture2D and then simply saves it

        static public void SaveRenderTextureToFile(RenderTexture renderTexture,string fileName)
        {
            Texture2D tex;
            tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);
            var oldRt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = oldRt;
           
            if (!Directory.Exists("./renders"))
            {
                Directory.CreateDirectory("./renders");

            }
            File.WriteAllBytes("./renders/" + fileName + ".png", tex.EncodeToPNG());
            if (Application.isPlaying)
                Object.Destroy(tex);
            else
                Object.DestroyImmediate(tex);

        }

    }

}