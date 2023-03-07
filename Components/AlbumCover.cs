using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Orange_Music_Player
{
    public class AlbumCover
    {
        public string CoverPath { get; }
        public BitmapImage Cover { get; }

        public static string[] validImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };

        private AlbumCover(string p, BitmapImage c)
        {
            CoverPath = p;
            Cover = c;
        }

        public static async Task<AlbumCover> FromStorageFile(StorageFile f)
        {
            string path;
            BitmapImage cover;
            if (f.FileType == ".pdf")
            {
                path = f.Path;
                cover = await GetBitmapImageFromPdfFile(f);
                if (cover != null)
                    return new AlbumCover(path, cover);
                return null;
            }
            if (Array.IndexOf(validImageTypes, f.FileType) > -1)
            {
                path = f.Path;
                cover = await GetBitmapImageFromImageFile(f);
                if (cover != null)
                    return new AlbumCover(path, cover);
                return null;
            }
            return null;
        }

        public static async Task<AlbumCover> FromPath(string p)
        {
            var file = await StorageFile.GetFileFromPathAsync(p);
            if (file != null)
            {
                return await FromStorageFile(file);
            } else
                Debug.WriteLine("File not found: " + p);
            return null;
        }
        
        public static IEnumerable<StorageFile> GetValidCoverImagesFromFiles(IEnumerable<StorageFile> files)
        {
            return files.Where(i => validImageTypes.Contains(i.FileType));
        }

        public async static Task<AlbumCover> FromFolder(StorageFolder folder)
        {
            string path;
            BitmapImage cover;
            IReadOnlyCollection<StorageFile> allFiles = await folder.GetFilesAsync();
            StorageFile imageFile = allFiles.Where(i => Array.IndexOf(validImageTypes, i.FileType) > -1).FirstOrDefault();
            if (imageFile != null)
            {
                path = imageFile.Path;
                cover = await GetBitmapImageFromImageFile(imageFile);
                if (cover != null)
                    return new AlbumCover(path, cover);
                return null;
            }
            StorageFile pdfFile = allFiles.Where(i => i.FileType == ".pdf").FirstOrDefault();
            if (pdfFile != null)
            {
                path = pdfFile.Path;
                cover = await GetBitmapImageFromPdfFile(pdfFile);
                if (cover != null)
                    return new AlbumCover(path, cover);
                return null;
            }
            return null;
        }

        private async static Task<BitmapImage> GetBitmapImageFromImageFile(StorageFile imageFile)
        {
            var image = new BitmapImage();
            
            using (IRandomAccessStreamWithContentType stream = await imageFile.OpenReadAsync())
            {
                await image.SetSourceAsync(stream);
                return image;
            }
        }

        private async static Task<BitmapImage> GetBitmapImageFromPdfFile(StorageFile pdfFile)
        {
            PdfDocument pdf = await PdfDocument.LoadFromFileAsync(pdfFile);
            if (pdf == null)
                return null;
            var page = pdf.GetPage(0);
            var image = new BitmapImage();
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                await page.RenderToStreamAsync(stream);
                await image.SetSourceAsync(stream);
                return image;
            }
        }
    }
}
