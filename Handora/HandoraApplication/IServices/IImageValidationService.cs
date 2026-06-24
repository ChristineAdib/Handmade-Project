using Microsoft.AspNetCore.Http;

namespace HandoraApplication.IServices
{
    public interface IImageValidationService
    {
        (bool IsValid, string ErrorMessage) ValidateImage(IFormFile file);
    }
}
