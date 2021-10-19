using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ePaperTeamsPresence.Desktop
{
    public static class BitmapUtil
    {
        /// <summary>
        /// Render the passed element to a bitmap and only allow the specified color to show up.
        /// </summary>
        public static BitmapSource ColorMask(FrameworkElement element, Color passThroughColor)
        {
            // scale up 
            int scaleFactor = 8;

            RenderTargetBitmap bmp = new RenderTargetBitmap(
                 (int)element.Width * scaleFactor,
                 (int)element.Height * scaleFactor,
                 96 * scaleFactor,
                 96 * scaleFactor,
                 PixelFormats.Pbgra32
                 );

            // it just helps a bit...
            element.SnapsToDevicePixels = true;

            TextOptions.SetTextRenderingMode(element, TextRenderingMode.Aliased);
            TextOptions.SetTextHintingMode(element, TextHintingMode.Fixed);
            TextOptions.SetTextFormattingMode(element, TextFormattingMode.Display);

            TextOptions.SetTextRenderingMode(bmp, TextRenderingMode.Aliased);
            TextOptions.SetTextHintingMode(bmp, TextHintingMode.Fixed);
            TextOptions.SetTextFormattingMode(bmp, TextFormattingMode.Display);

            bmp.Render(element);

            WriteableBitmap writeableBitmap = new WriteableBitmap(bmp);

            using (writeableBitmap.GetBitmapContext())
            {
                // color masking
                writeableBitmap.ForEach((x, y, color) =>
                {
                    color.A = 0xFF;
                    return color == passThroughColor ? Colors.Black : Colors.White;
                });
            }

            return writeableBitmap.Resize((int)element.Width, (int)element.Height, WriteableBitmapExtensions.Interpolation.Bilinear);
        }

        public static byte[] ToBytes(BitmapSource bitmap)
        {
            var bmpEncoder = new BmpBitmapEncoder();
            bmpEncoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var mem = new MemoryStream())
            {
                bmpEncoder.Save(mem);

                mem.Flush();

                return mem.GetBuffer();
            }
        }
    }
}
