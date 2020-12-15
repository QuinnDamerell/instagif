using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Instagif
{
    class Clippy
    {
        const string s_copyTempFileName = "instagif_temp.gif";
        const string s_gifSuffix = ".gif"
;
        public static bool SetClipboardContent(GiphyDotNet.Model.GiphyImage.Data data)
        {
            try
            {
                string actaulGifUrl = data.Images.Original.Url;
                int gifPos = actaulGifUrl.ToLower().IndexOf(s_gifSuffix);
                if (gifPos != -1)
                {
                    gifPos += s_gifSuffix.Length;
                    actaulGifUrl = actaulGifUrl.Substring(0, gifPos);
                }

                // Get a temp path to the file
                string tempFolder = Path.GetTempPath();
                string tempPath = tempFolder + s_copyTempFileName;

                // Download it
                using (var client = new WebClient())
                {
                    client.DownloadFile(actaulGifUrl, tempPath);
                }

                // Open it as a bitmap
                BitmapSource img = new BitmapImage(new Uri(tempPath));

                // Setup the drop file locations
                List<string> dropFiles = new List<string>();
                dropFiles.Add(tempPath);
                StringCollection dropFileCollection = new StringCollection();
                dropFileCollection.AddRange(dropFiles.ToArray());

                // Setup the move effect buffer (this is part of the drop files
                byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
                MemoryStream dropEffect = new MemoryStream();
                dropEffect.Write(moveEffect, 0, moveEffect.Length);

                // Setup the HTML format - this is what most apps use
                string htmlFormat = CreateClipboardHtmlFormat(actaulGifUrl);

                DataObject clipData = new DataObject();

                // These files allow the program to access the local gif.
                clipData.SetFileDropList(dropFileCollection);
                clipData.SetData("Preferred DropEffect", dropEffect);

                // This is the heavy lifter, most apps use this to actually get the gif.
                clipData.SetData("HTML Format", htmlFormat);
                clipData.SetText(actaulGifUrl);

                // Most apps don't use the image, but it must be here for them to want to use the gif html right.
                clipData.SetImage(img);

                // Clear the current clipboard and set our data.
                Clipboard.Clear();
                Clipboard.SetDataObject(clipData, true);
            }
            catch
            {
                return false;
            }
            return true;   
        }

        static private string CreateClipboardHtmlFormat(string gifUrl)
        {
            // The html format is really strange, you can find it here
            // https://docs.microsoft.com/en-us/windows/win32/dataxchg/html-clipboard-format

            // This is the actual html we will use.
            string html = $"<html><body>\r\n<!--StartFragment--><img src=\"{gifUrl}\"><!--EndFragment-->\r\n</body>\r\n</html>";

            return HtmlClipboardHelper.GetHtmlDataString(html);
        } 

        public static void Test()
        {

            var format = Clipboard.GetDataObject();
            var list = format.GetFormats();
            Console.WriteLine(list);

            var aDataDropEffect = Clipboard.GetData("Preferred DropEffect");
            if (aDataDropEffect != null)
            {
                MemoryStream aDropEffect = (MemoryStream)aDataDropEffect;
                byte[] aMoveEffect = new byte[4];
                aDropEffect.Read(aMoveEffect, 0, aMoveEffect.Length);
                var aDragDropEffects = (DragDropEffects)BitConverter.ToInt32(aMoveEffect, 0);
                var aMove = aDragDropEffects.HasFlag(DragDropEffects.Move);
            }

            foreach (string f in list)
            {
                if(f == "application/x-moz-nativeimage")
                    {
                    continue;
                }
                var data = format.GetData(f);
                var str = "";
                if(data is MemoryStream)
                {
                    var ms = (MemoryStream)data;
                    string teststr = Encoding.Unicode.GetString(ms.ToArray());
                    FileStream files = File.Create($"C:\\Users\\quinn\\Desktop\\copy\\file-{f.Replace("/", "-")}.gif");
                    ms.WriteTo(files);
                    files.Close();
                }
                if(data is Bitmap)
                {
                    Bitmap bb = (Bitmap)data;
                    bb.Save($"C:\\Users\\quinn\\Desktop\\copy\\file-{f.Replace("/", "-")}.gif");
                }
                if(data is string)
                {
                    str = (string)data;
                }
                if(data is List<string>)
                {

                }
            }

            //// Clipboard.SetText(url);
            ////Clipboard.Clear();
            //List<string> file = new List<string>();
            //file.Add("C:\\Users\\quinn\\AppData\\Local\\Temp\\safe_image.gif");
            ////Clipboard.SetData(DataFormats.FileDrop, "C:\\Users\\quinn\\Desktop\\safe_image.php.gif", true);
            //StringCollection collection = new StringCollection();
            //collection.AddRange(file.ToArray());
            ////Clipboard.SetFileDropList(collection);



            ////MemoryStream mss = new MemoryStream();

            ////Int32 test = (Int32)DragDropEffects.Move;
            ////byte[] buf = BitConverter.GetBytes(test);
            ////mss.Write(buf);
            ////Clipboard.SetData("Preferred DropEffect", mss);

            //string texthtml = "<img class=\" _52mr _1byr _5pf5 img\" alt=\"\" src=\"https://external-sea1-1.xx.fbcdn.net/safe_image.php?d=AQDCpUNwEUCXoOSl&amp;url=https%3A%2F%2Fmedia3.giphy.com%2Fmedia%2Fl3JDmUAPidmdgkkF2%2Fgiphy.gif&amp;ext=gif&amp;_nc_cb=1&amp;_nc_hash=AQDh_KCk7XXvpfgP\" style=\"max-width: 100%; width: 100%;\">\0";

            //byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
            //MemoryStream dropEffect = new MemoryStream();
            //dropEffect.Write(moveEffect, 0, moveEffect.Length);

            //MemoryStream html = new MemoryStream();
            //byte[] b = Encoding.Unicode.GetBytes(texthtml);
            //html.Write(b, 0, b.Length);

            //DataObject datad = new DataObject();
            //datad.SetFileDropList(collection);
            //datad.SetData("Preferred DropEffect", dropEffect);
            //datad.SetData("text/html", b);
            //datad.SetData("HTML Format", "Version:0.9\r\nStartHTML:00000097\r\nEndHTML:00000459\r\nStartFragment:00000131\r\nEndFragment:00000423\r\n<html><body>\r\n<!--StartFragment--><img class=\" _52mr _1byr _5pf5 img\" alt=\"\" src=\"https://external-sea1-1.xx.fbcdn.net/safe_image.php?d=AQDCpUNwEUCXoOSl&amp;url=https%3A%2F%2Fmedia3.giphy.com%2Fmedia%2Fl3JDmUAPidmdgkkF2%2Fgiphy.gif&amp;ext=gif&amp;_nc_cb=1&amp;_nc_hash=AQDh_KCk7XXvpfgP\" style=\"max-width: 100%; width: 100%;\"><!--EndFragment-->\r\n</body>\r\n</html>");

            //BitmapSource img = new BitmapImage();
            //datad.SetImage(img);




            //Clipboard.Clear();
            //Clipboard.SetDataObject(datad, true);

        }
    }
}
