using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class IconBuilder
{
    static void Main()
    {
        string srcJpg = @"C:\Users\yu896367449\.gemini\antigravity\brain\b686dd58-7148-46bd-a805-478a03bfa99d\cat_paw_app_icon_1786789324782.jpg";
        string outPng = @"D:\yu896367449\Antigravity Chat\App develope\CatPawPlayer.WinUI\Assets\AppIcon.png";
        string outIco = @"D:\yu896367449\Antigravity Chat\App develope\CatPawPlayer.WinUI\Assets\AppIcon.ico";

        using (Image srcImg = Image.FromFile(srcJpg))
        {
            // 1. Save true PNG 256x256
            using (Bitmap bmp256 = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp256))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(srcImg, 0, 0, 256, 256);
                }
                bmp256.Save(outPng, ImageFormat.Png);
            }

            // 2. Build multi-resolution ICO (256, 128, 64, 48, 32, 16)
            int[] sizes = new int[] { 256, 128, 64, 48, 32, 16 };
            List<byte[]> pngStreams = new List<byte[]>();

            for (int s = 0; s < sizes.Length; s++)
            {
                int sz = sizes[s];
                using (Bitmap resized = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(srcImg, 0, 0, sz, sz);
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        resized.Save(ms, ImageFormat.Png);
                        pngStreams.Add(ms.ToArray());
                    }
                }
            }

            using (FileStream fs = new FileStream(outIco, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR
                bw.Write((short)0); // reserved
                bw.Write((short)1); // icon type
                bw.Write((short)sizes.Length); // number of images

                int offset = 6 + (16 * sizes.Length);

                // ICONDIRENTRY list
                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    byte bWidth = sz >= 256 ? (byte)0 : (byte)sz;
                    byte bHeight = sz >= 256 ? (byte)0 : (byte)sz;

                    bw.Write(bWidth);
                    bw.Write(bHeight);
                    bw.Write((byte)0); // colors
                    bw.Write((byte)0); // reserved
                    bw.Write((short)1); // color planes
                    bw.Write((short)32); // bpp
                    bw.Write(pngStreams[i].Length); // size in bytes
                    bw.Write(offset); // offset

                    offset += pngStreams[i].Length;
                }

                // Image payloads
                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngStreams[i]);
                }

                bw.Flush();
            }
        }

        Console.WriteLine("Done building ICO: " + outIco);
    }
}
