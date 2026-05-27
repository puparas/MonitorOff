using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class CreateIcon
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: CreateIcon <input.png> <output.ico>");
            return 1;
        }

        using (var source = new Bitmap(args[0]))
        {
            SaveAsIcon(source, args[1]);
        }

        Console.WriteLine("Created " + args[1]);
        return 0;
    }

    private static void SaveAsIcon(Bitmap source, string path)
    {
        int[] sizes = { 16, 32, 48, 256 };
        var pngImages = new List<byte[]>();

        foreach (int size in sizes)
        {
            using (var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, 0, 0, size, size);

                using (var memory = new MemoryStream())
                {
                    resized.Save(memory, ImageFormat.Png);
                    pngImages.Add(memory.ToArray());
                }
            }
        }

        using (var output = File.Create(path))
        using (var writer = new BinaryWriter(output))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)pngImages.Count);

            int offset = 6 + 16 * pngImages.Count;
            for (int i = 0; i < sizes.Length; i++)
            {
                writer.Write((byte)sizes[i]);
                writer.Write((byte)sizes[i]);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(pngImages[i].Length);
                writer.Write(offset);
                offset += pngImages[i].Length;
            }

            foreach (byte[] png in pngImages)
                writer.Write(png);
        }
    }
}
