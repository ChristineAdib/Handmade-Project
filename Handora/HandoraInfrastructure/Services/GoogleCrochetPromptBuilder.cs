using System;
using System.Text;
using System.Text.Json;
using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraDomain.Consts;
using HandoraDomain.Models.CustomStudioEntities;

namespace HandoraInfrastructure.Services
{
    public class GoogleCrochetPromptBuilder : IAIPromptBuilder
    {
        private const string PositiveStyleTokens = "Crochet texture, Handmade yarn, Premium craftsmanship, Natural lighting, Studio photography, Cute proportions, Highly detailed stitches, Soft shadows, Photorealistic crochet doll";
        private const string NegativeStyleTokens = "Plastic, Toy, Pixar, Anime, Cartoon, CGI, Low quality";

        public PromptBuildResult BuildPrompt(CustomConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.ProductType == ProductType.CrochetDoll)
            {
                return BuildCrochetDollPrompt(configuration.ConfigurationDataJson);
            }

            return BuildGenericPrompt(configuration.ProductType, configuration.ConfigurationDataJson);
        }

        private PromptBuildResult BuildCrochetDollPrompt(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<CrochetDollConfiguration>(json, options);

                if (config == null)
                {
                    return new PromptBuildResult
                    {
                        PositivePrompt = $"A beautiful handmade amigurumi crochet doll. {PositiveStyleTokens}",
                        NegativePrompt = NegativeStyleTokens
                    };
                }

                var sb = new StringBuilder();
                sb.Append("A premium, high-quality, professional studio photo of a handmade amigurumi crochet doll. ");
                sb.Append("The doll has the following detailed custom features: ");

                sb.Append($"It is styled as a {config.Gender.ToString().ToLower()} character. ");
                sb.Append($"The doll's size is approximately {config.Size} tall, with a {config.BodyType.ToString().ToLower()} body structure. ");
                sb.Append($"It has a beautiful {config.SkinTone.ToLower()} skin tone. ");

                if (config.Hair != null && config.Hair.Style != HairStyle.Bald)
                {
                    sb.Append($"The doll has {config.Hair.Length.ToLower()} {config.Hair.Color.ToLower()} hair, styled in a {config.Hair.Style.ToString().ToLower()} fashion. ");
                }
                else
                {
                    sb.Append("The doll has a bald head. ");
                }

                if (config.Face != null)
                {
                    sb.Append($"The face features detailed {config.Face.EyeShape.ToLower()} eyes with a beautiful {config.Face.EyeColor.ToLower()} color. ");
                    sb.Append($"It has a friendly {config.Face.Smile.ToLower()} expression. ");
                    sb.Append("It has beautifully stitched, subtle eyebrows that match the facial expression. ");

                    if (config.Face.Freckles)
                    {
                        sb.Append("Subtle cute stitched freckles are visible on the cheeks. ");
                    }
                    if (config.Face.Blush)
                    {
                        sb.Append("Soft pink blush details are applied to the cheeks. ");
                    }
                }

                if (config.Outfit != null && !string.IsNullOrWhiteSpace(config.Outfit.Description))
                {
                    sb.Append($"The doll is dressed in a custom {config.Outfit.Type.ToString().ToLower()} outfit described as: {config.Outfit.Description.Trim().TrimEnd('.')}. ");
                }

                if (config.Accessories != null && config.Accessories.Type != AccessoryType.None && !string.IsNullOrWhiteSpace(config.Accessories.Description))
                {
                    sb.Append($"It is accessorized with a custom {config.Accessories.Type.ToString().ToLower()} described as: {config.Accessories.Description.Trim().TrimEnd('.')}. ");
                }

                if (config.Personalization != null && !string.IsNullOrWhiteSpace(config.Personalization.LabelText))
                {
                    sb.Append($"There is a small personalized embroidered tag nearby displaying the text '{config.Personalization.LabelText}' in a {config.Personalization.Font.ToString().ToLower()} font type. ");
                }

                if (!string.IsNullOrWhiteSpace(config.AdditionalNotes))
                {
                    sb.Append($"Special styling details: {config.AdditionalNotes.Trim().TrimEnd('.')}. ");
                }

                sb.Append($"Crafted entirely from soft high-quality yarn, with tight, neat stitches. The doll is standing upright against a clean, soft-focus neutral studio background. Photographed with professional lighting. {PositiveStyleTokens}");

                return new PromptBuildResult
                {
                    PositivePrompt = sb.ToString(),
                    NegativePrompt = NegativeStyleTokens
                };
            }
            catch (Exception)
            {
                return new PromptBuildResult
                {
                    PositivePrompt = $"A beautiful handmade amigurumi crochet doll. {PositiveStyleTokens}",
                    NegativePrompt = NegativeStyleTokens
                };
            }
        }

        private PromptBuildResult BuildGenericPrompt(ProductType type, string json)
        {
            var sb = new StringBuilder();
            sb.Append($"A premium, high-quality, professional studio photo of a custom handmade {type.ToString().ToLower()}. ");

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                sb.Append("Features: ");
                foreach (var prop in root.EnumerateObject())
                {
                    sb.Append($"{prop.Name}: {prop.Value.ToString()}, ");
                }
            }
            catch
            {
                sb.Append("Fully configured with custom options. ");
            }

            sb.Append($"Photographed with soft professional studio lighting against a neutral background. {PositiveStyleTokens}");

            return new PromptBuildResult
            {
                PositivePrompt = sb.ToString(),
                NegativePrompt = NegativeStyleTokens
            };
        }
    }
}
