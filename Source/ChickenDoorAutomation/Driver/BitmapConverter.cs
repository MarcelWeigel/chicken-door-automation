using System.Drawing;
using System.IO;

namespace Driver
{
    public class BitmapConverter
    {
        public static string Convert(Bitmap bitmap)
        {
            using MemoryStream m = new MemoryStream();

            bitmap.Save(m, bitmap.RawFormat);
            byte[] imageBytes = m.ToArray();

            string base64String = System.Convert.ToBase64String(imageBytes);
            return "data:image/png;base64," + base64String;
        }
    }
}
