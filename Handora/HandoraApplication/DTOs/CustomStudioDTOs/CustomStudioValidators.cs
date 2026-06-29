using System;
using System.Text.Json;
using FluentValidation;
using HandoraDomain.Consts;
using HandoraDomain.Models.CustomStudioEntities;

namespace HandoraApplication.DTOs.CustomStudioDTOs
{
    public class CreateCustomRequestCommandValidator : AbstractValidator<CreateCustomRequestCommand>
    {
        public CreateCustomRequestCommandValidator()
        {
            RuleFor(x => x.ProductType)
                .IsInEnum().WithMessage("A valid Product Type must be specified.");

            RuleFor(x => x.TargetBudget)
                .GreaterThan(0).WithMessage("Target budget must be greater than 0.")
                .When(x => x.TargetBudget.HasValue);

            RuleFor(x => x.DeadlineDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Deadline date must be in the future.")
                .When(x => x.DeadlineDate.HasValue);
        }
    }

    public class SaveConfigurationCommandValidator : AbstractValidator<SaveConfigurationCommand>
    {
        public SaveConfigurationCommandValidator()
        {
            RuleFor(x => x.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");

            RuleFor(x => x.ConfigurationDataJson)
                .NotEmpty().WithMessage("Configuration data JSON cannot be empty.");

            // Custom conditional validation for CrochetDoll
            RuleFor(x => x)
                .Custom((cmd, context) =>
                {
                    if (cmd.ProductType == ProductType.CrochetDoll)
                    {
                        try
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var config = JsonSerializer.Deserialize<CrochetDollConfiguration>(cmd.ConfigurationDataJson, options);
                            
                            if (config == null)
                            {
                                context.AddFailure("Invalid JSON structure for Crochet Doll configuration.");
                                return;
                            }

                            // Validate strongly-typed values
                            if (config.Gender == Gender.Unspecified)
                            {
                                context.AddFailure("Gender selection is required.");
                            }
                            if (string.IsNullOrWhiteSpace(config.Size))
                            {
                                context.AddFailure("Size selection is required.");
                            }
                            if (config.BodyType == 0)
                            {
                                context.AddFailure("Body Type selection is required.");
                            }
                            if (string.IsNullOrWhiteSpace(config.SkinTone))
                            {
                                context.AddFailure("Skin Tone selection is required.");
                            }

                            // Validate Hair
                            if (config.Hair == null)
                            {
                                context.AddFailure("Hair styling configuration is required.");
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(config.Hair.Color))
                                    context.AddFailure("Hair Color is required.");
                                if (string.IsNullOrWhiteSpace(config.Hair.Length))
                                    context.AddFailure("Hair Length is required.");
                            }

                            // Validate Eyes
                            if (config.Face == null)
                            {
                                context.AddFailure("Face styling configuration is required.");
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(config.Face.EyeShape))
                                    context.AddFailure("Eye Shape selection is required.");
                                if (string.IsNullOrWhiteSpace(config.Face.EyeColor))
                                    context.AddFailure("Eye Color selection is required.");
                                if (string.IsNullOrWhiteSpace(config.Face.Smile))
                                    context.AddFailure("Smile selection is required.");
                            }

                            // Validate Personalization
                            if (config.Personalization != null && !string.IsNullOrWhiteSpace(config.Personalization.LabelText))
                            {
                                if (config.Personalization.LabelText.Length > 50)
                                {
                                    context.AddFailure("Personalization label text cannot exceed 50 characters.");
                                }
                            }

                            // Validate Notes
                            if (config.AdditionalNotes != null && config.AdditionalNotes.Length > 500)
                            {
                                context.AddFailure("Additional notes cannot exceed 500 characters.");
                            }

                            // Validate Reference Image Url
                            if (!string.IsNullOrWhiteSpace(config.ReferenceImageUrl))
                            {
                                if (!Uri.TryCreate(config.ReferenceImageUrl, UriKind.Absolute, out _))
                                {
                                    context.AddFailure("Reference image must be a valid absolute URL.");
                                }
                            }
                        }
                        catch (Exception)
                        {
                            context.AddFailure("Failed to deserialize configuration data JSON for Crochet Doll.");
                        }
                    }
                });
        }
    }

    public class CreateSellerOfferCommandValidator : AbstractValidator<CreateSellerOfferCommand>
    {
        public CreateSellerOfferCommandValidator()
        {
            RuleFor(x => x.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");

            RuleFor(x => x.ShopId)
                .NotEmpty().WithMessage("Seller Shop ID is required.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Offer price must be greater than 0.");

            RuleFor(x => x.DeliveryTimeDays)
                .GreaterThan(0).WithMessage("Delivery estimation must be at least 1 day.");

            RuleFor(x => x.RevisionsAllowed)
                .GreaterThanOrEqualTo(0).WithMessage("Revisions allowed cannot be negative.");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Offer notes cannot exceed 500 characters.");

            RuleFor(x => x.Attachments)
                .Custom((list, context) =>
                {
                    if (list != null)
                    {
                        foreach (var url in list)
                        {
                            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                            {
                                context.AddFailure($"Attachment metadata contains an invalid URL: {url}");
                            }
                        }
                    }
                });
        }
    }
}
