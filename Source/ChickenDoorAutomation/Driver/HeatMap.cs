using System.Drawing;

namespace Driver
{
    public class HeatMap
    {
        private static double factor = 240d / 45;
        public static Color ColorFromTemperature(double temperature)
        {
            double t = 240 - (factor * temperature) % 240;

            return HSV.ColorFromHSV(t, 1, 1);
        }

        public static string Base64HeatMapFromTemperature(double[] temperatures)
        {
            var bitmap = BitmapFromTemperature(temperatures);
            return BitmapConverter.Convert(bitmap);
        }

        private static Bitmap BitmapFromTemperature(double[] temperatures)
        {
            var b = new Bitmap(8, 8);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var t = temperatures[8 * x + y];
                    var color = ColorFromTemperature(t);
                    b.SetPixel(x, y, color);
                }
            }

            return b;
        }
    }
}
