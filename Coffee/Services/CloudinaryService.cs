using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Coffee.DTO;

namespace Coffee.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var cloudinaryConfig = config.GetSection("Cloudinary");

            var account = new Account(
                cloudinaryConfig["CloudName"],
                cloudinaryConfig["ApiKey"],
                cloudinaryConfig["ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        // =========================
        // 📤 UPLOAD
        // =========================
        public async Task<CloudinaryUploadResult?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "img/Products"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId
            };
        }

        // =========================
        // ❌ DELETE IMAGE
        // =========================
        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return;

            var deleteParams = new DeletionParams(publicId)
            {
                Invalidate = true // 🔥 xoá cache CDN luôn
            };

            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}