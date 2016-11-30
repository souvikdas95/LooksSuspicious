using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App5
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("394c0875c8a54d37970289f2dde56808");

        private Guid myFaceId = Guid.Empty;

        private async Task<FaceRectangle[]> UploadAndDetectFaces1(StorageFile file)
        {
            try
            {
                using (Stream imageFileStream = await file.OpenStreamForReadAsync())
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    var myFaces = faces.Select(face => face.FaceId).ToArray();
                    if(myFaces.Length > 0)
                        myFaceId = myFaces[0];
                    return faceRects.ToArray();
                }
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }

        private async Task<Face[]> UploadAndDetectFaces2(StorageFile file)
        {
            try
            {
                using (Stream imageFileStream = await file.OpenStreamForReadAsync())
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    return faces;
                }
            }
            catch (Exception)
            {
                return new Face[0];
            }
        }

        private async void button_OnClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                //OutputTextBlock.Text = "Picked photo: " + file.Name;
            }
            else
            {
                //OutputTextBlock.Text = "Operation cancelled.";
                return;
            }

            string filePath = file.Path;
            //Uri fileUri = new Uri(filePath);
            //await new MessageDialog(fileUri.AbsoluteUri).ShowAsync();

            //Stream bmpStream = System.IO.File.Open(filePath, System.IO.FileMode.Open);
            //Stream stream = System.IO.File.Open(filePath, System.IO.FileMode.Open);
            //var memStream = new MemoryStream();
            //stream.CopyTo(memStream);
            //memStream.Position = 0;

            IRandomAccessStream astream = await file.OpenReadAsync();
            System.IO.Stream ostream = await file.OpenStreamForReadAsync();
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(astream);
            WriteableBitmap writeableBmp = await BitmapFactory.New(1, 1).FromStream(ostream);
            facephoto.Source = writeableBmp;

            //bitmapSource.UriSource = fileUri;

            new MessageDialog("Detecting...").ShowAsync();
            FaceRectangle[] faceRects = null;
            myFaceId = Guid.Empty;
            try
            {
                using (Stream imageFileStream = await file.OpenStreamForReadAsync())
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    var faceRectsraw = faces.Select(face => face.FaceRectangle);
                    var myFaces = faces.Select(face => face.FaceId).ToArray();
                    if (myFaces.Length > 0)
                        myFaceId = myFaces[0];
                    faceRects = faceRectsraw.ToArray();
                }
            }
            catch (Exception)
            {
                faceRects = new FaceRectangle[0];
            }

            await new MessageDialog(String.Format("Detection Finished. {0} face(s) detected", faceRects.Length)).ShowAsync();

            if (faceRects.Length > 0)
            {
                /*DrawingSession visual = new DrawingSession();
                DrawingSession drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));*/
                double dpi = decoder.DpiX;
                double resizeFactor = 1;
                

                foreach (var faceRect in faceRects)
                {
                    //await new MessageDialog("Testing Face..." + faceRect.Left + "," + faceRect.Top + "," + faceRect.Height + "," + faceRect.Width + ":" + writeableBmp.PixelHeight + "," + writeableBmp.PixelWidth).ShowAsync();

                    writeableBmp.DrawRectangle(
                        (int)(faceRect.Left * resizeFactor),
                        (int)(faceRect.Top * resizeFactor),
                        (int)((faceRect.Left + faceRect.Width) * resizeFactor),
                        (int)((faceRect.Top + faceRect.Height) * resizeFactor),
                        Colors.Red);
                }

                /*drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);*/
                facephoto.Source = writeableBmp;
            }
        }

        private async void button3_OnClick(object sender, RoutedEventArgs e)
        {
            if(myFaceId.Equals(Guid.Empty))
            {
                await new MessageDialog("No Face to Compare!").ShowAsync();
                return;
            }

            StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder assets = await appInstalledFolder.GetFolderAsync("Images");
            var files = await assets.GetFilesAsync();
            if (files.Count < 1)
            {
                await new MessageDialog("No Files Available to Compare!").ShowAsync();
                return;
            }

            double maxsimilarity = 0.0;
            StorageFile maxfile = null;

            new MessageDialog("Comparing... Please Wait").ShowAsync();

            Face[] GFace3 = null;
            foreach (var file2 in files)
            {
                var GFace2 = await UploadAndDetectFaces2(file2);
                Guid[] GFace2Ids = GFace2.Select(face => face.FaceId).ToArray();
                if (GFace2Ids.Length < 1)
                    continue;
                SimilarFace[] temp = await faceServiceClient.FindSimilarAsync(myFaceId, GFace2Ids);
                if(temp.Length > 0)
                {
                    foreach (var smf in temp)
                    {
                        if (smf.Confidence > maxsimilarity)
                        {
                            maxsimilarity = smf.Confidence;
                            maxfile = file2;
                            GFace3 = GFace2;
                        }
                    }
                }
            }

            if(maxsimilarity == 0.0)
            {
                await new MessageDialog("No Match Found!").ShowAsync();
                return;
            }

            IRandomAccessStream astream = await maxfile.OpenReadAsync();
            System.IO.Stream ostream = await maxfile.OpenStreamForReadAsync();
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(astream);
            WriteableBitmap writeableBmp = await BitmapFactory.New(1, 1).FromStream(ostream);
            facephoto2.Source = writeableBmp;

            // draw rectangles
            FaceRectangle[] faceRects = GFace3.Select(face => face.FaceRectangle).ToArray();

            await new MessageDialog(String.Format("Detection Finished. {0} face(s) detected", faceRects.Length)).ShowAsync();

            if (faceRects.Length > 0)
            {
                double dpi = decoder.DpiX;
                double resizeFactor = 1;


                foreach (var faceRect in faceRects)
                {
                    writeableBmp.DrawRectangle(
                        (int)(faceRect.Left * resizeFactor),
                        (int)(faceRect.Top * resizeFactor),
                        (int)((faceRect.Left + faceRect.Width) * resizeFactor),
                        (int)((faceRect.Top + faceRect.Height) * resizeFactor),
                        Colors.Red);
                }

                /*drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);*/
                facephoto2.Source = writeableBmp;
            }

            await new MessageDialog("Similarity: " + (maxsimilarity * 100).ToString() + "%").ShowAsync();
        }
    }
}
