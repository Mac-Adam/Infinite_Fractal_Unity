using System.Collections.Generic;
using UnityEngine;

namespace Colors
{
    public static class MyColoringSystem{
        static Vector4 HexTo01Color(string hex)
        {
            Dictionary<char, float> lookupTabe = new Dictionary<char, float>()
            {
                {'0', 0.0f},
                {'1', 1.0f},
                {'2', 2.0f},
                {'3', 3.0f},
                {'4', 4.0f},
                {'5', 5.0f},
                {'6', 6.0f},
                {'7', 7.0f},
                {'8', 8.0f},
                {'9', 9.0f},
                {'a', 10.0f},
                {'b', 11.0f},
                {'c', 12.0f},
                {'d', 13.0f},
                {'e', 14.0f},
                {'f', 15.0f},
                {'A', 10.0f},
                {'B', 11.0f},
                {'C', 12.0f},
                {'D', 13.0f},
                {'E', 14.0f},
                {'F', 15.0f}
            };
            return new Vector4(
                (lookupTabe[hex[1]] * 16.0f + lookupTabe[hex[2]]) / 256.0f,
                (lookupTabe[hex[3]] * 16.0f + lookupTabe[hex[4]]) / 256.0f,
                (lookupTabe[hex[5]] * 16.0f + lookupTabe[hex[6]]) / 256.0f,
                1.0f
           );
        }

        static public ColorPalette[] colorPalettes = new ColorPalette[] {
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#FFFFFF"),
                HexTo01Color("#155e80"),
                HexTo01Color("#91b8c4"),
                HexTo01Color("#d67b27"),
                HexTo01Color("#03074d")
            },11,"Pastel Normal"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#FFFFFF"),
                HexTo01Color("#155e80"),
                HexTo01Color("#91b8c4"),
                HexTo01Color("#d67b27"),
                HexTo01Color("#03074d")
            },12,"Pastel Normal 2"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#FFFFFF"),
            },13,"Normal map"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#155e80"),
                HexTo01Color("#91b8c4"),
                HexTo01Color("#d67b27"),
                HexTo01Color("#03074d")
            },13,"Rainbow normal"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#155e80"),
                HexTo01Color("#91b8c4"),
                HexTo01Color("#d67b27"),
                HexTo01Color("#03074d")
            },1,"Sunset"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#000764"),
                HexTo01Color("#206bcb"),
                HexTo01Color("#edffff"),
                HexTo01Color("#ffaa00"),
                HexTo01Color("#000200"),
            },1,"Wikipedia"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#060864"),
                HexTo01Color("#310c54"),
                HexTo01Color("#b4950a"),
            },1,"Blue Yellow"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#7F7FD5"),
                HexTo01Color("#91eae4"),
                HexTo01Color("#4de893"),
            },2,"Green Pastel"),
        new ColorPalette(
             new Vector4[] {
                HexTo01Color("#7F7FD5"),
                HexTo01Color("#91eae4")
            },3,"Pasetel"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#59C173"),
                HexTo01Color("#520645"),
                HexTo01Color("#5D26C1")
            },1,"Green Purple"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#40E0D0"),
                HexTo01Color("#FF8C00"),
                HexTo01Color("#5D26C1"),
            },1,"Bright Rainbow"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#155e80"),
                HexTo01Color("#91b8c4"),
                HexTo01Color("#d67b27"),
                HexTo01Color("#03074d")
            },2,"Dark Rainbow"),
        new ColorPalette(
            new Vector4[] {
                 HexTo01Color("#FFFFFF")
            },5,"Triangle Tiling",0),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#FFFFFF")
            },6,"1 2 rule them all",1),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#FFFFFF")
            },6,"Nebula",2),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#000000"),
                HexTo01Color("#FFFFFF")
            },7,"B/W distance"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#d67b27"),
                HexTo01Color("#03074d"),
                HexTo01Color("#155e80"),
            },7,"Distance Dark"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#7F7FD5"),
                HexTo01Color("#91eae4")
            },8,"Distance Bright"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#FFFFFF"),
                HexTo01Color("#155e80"),
                HexTo01Color("#91b8c4"),
                HexTo01Color("#d67b27"),
                HexTo01Color("#03074d")
            },9,"Light Distance"),
        new ColorPalette(
            new Vector4[] {
                HexTo01Color("#000000"),
                HexTo01Color("#40E0D0"),
                HexTo01Color("#FF8C00"),
                HexTo01Color("#5D26C1"),
            },10,"Dark Distance"),
        };

    }
    public struct ColorPalette
    {
        //Types:
        // 0 - In rgb color space
        // 1 - In lab color space
        // 2 - in lch color space, shortest
        // 3 - In lch one dir
        // 4 - In lch other
        // 5 Tilings
        // 6 Rings 
        // 7 distance estimation lab
        // 8 distance estimation lch
        // On the next two types the first color is the darkener and will not be used as a gradient
        // 9 lab with distance estimation as a darkener
        // 10 lch with distance estimation as a darkener
        // 11, 12 - less accurate normal map render with color light
        // 13 14 - more acurate normal map render with white light
        public Vector4[] colors;
        public int length;
        public int type;
        public string name;
        public int imageIdx;
        public ColorPalette(Vector4[] col, int t, string n,int img = 0)
        {
            colors = col;
            length = col.Length;
            type = t;
            name = n;
            imageIdx = img;
        }
    }
   
}