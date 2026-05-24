using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace HandoraApplication.Helpers
{
    public class ImageHelper
    {
        private readonly string _webRootPath;

        public ImageHelper(string webRootPath)
        {
            _webRootPath = webRootPath;
        }

        public async Task<string> SaveImageAsync(IFormFile image)
        {
            string uploadsFolder = Path.Combine(_webRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/uploads/{uniqueFileName}";
        }

        public Task DeleteImages(List<string> imagesPaths)
        {
            foreach (var imagesPath in imagesPaths)
                DeleteImage(imagesPath);
            return Task.CompletedTask;
        }

        public Task DeleteImage(string imagePath)
        {
            var relativePath = imagePath[1..].Replace("/", "\\");
            var fullPath = Path.Combine(_webRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}
