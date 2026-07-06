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
        // ──────────────────────────────────────────────────────────────────────
        // Layer 1 — Brand Identity
        // Every generated image must follow the HandAura visual identity.
        // ──────────────────────────────────────────────────────────────────────
        private const string BrandIdentityLayer =
            "Create a premium handmade crochet amigurumi doll that looks like an authentic luxury handcrafted product sold in an artisan marketplace. " +
            "The doll must look handmade rather than factory produced. " +
            "Visible yarn texture. Premium cotton yarn. Natural crochet stitches. " +
            "Soft fabric. Handcrafted appearance. Warm cozy feeling. " +
            "Elegant proportions. Cute but realistic.";

        // ──────────────────────────────────────────────────────────────────────
        // Layer 2 — Product Photography
        // Every image must look like a luxury catalog product.
        // ──────────────────────────────────────────────────────────────────────
        private const string ProductPhotographyLayer =
            "Professional product photography. Soft studio lighting. Warm daylight. " +
            "Natural shadows. Luxury e-commerce catalog. Minimal beige background. " +
            "DSLR quality. Centered composition. Standing full body. " +
            "Ultra realistic. Hyper detailed. 8K quality. " +
            "Professional commercial photography. Luxury Etsy style.";

        // ──────────────────────────────────────────────────────────────────────
        // Layer 4 — Crochet Details
        // Force the AI to generate authentic crochet textures.
        // ──────────────────────────────────────────────────────────────────────
        private const string CrochetDetailsLayer =
            "Visible crochet stitches. Natural yarn fibers. Hand knitted texture. " +
            "Soft cotton threads. Realistic fabric folds. Premium crochet craftsmanship. " +
            "Tiny handmade imperfections. Detailed embroidery. Luxury handmade finish.";

        // ──────────────────────────────────────────────────────────────────────
        // Photo Mode Prompt
        // Used when the user uploads a real person's photo.
        // ──────────────────────────────────────────────────────────────────────
        private const string PhotoModePrompt =
            "Create a premium handmade crochet amigurumi doll inspired by the uploaded person. " +
            "Do NOT reproduce the exact face. Instead, generate a crochet doll that captures the person's essence. " +
            "Preserve: hair style, hair color, face proportions, smile, glasses (if present), " +
            "beard (if present), clothing style, accessories, and overall personality. " +
            "Transform everything into a premium handcrafted crochet doll with soft yarn texture, " +
            "visible crochet stitches, premium amigurumi craftsmanship, warm lighting, clean studio background. " +
            "The result should feel recognizable while clearly being a handmade crochet character.";

        // ──────────────────────────────────────────────────────────────────────
        // Negative Prompt — Comprehensive list of unwanted qualities
        // ──────────────────────────────────────────────────────────────────────
        private const string NegativePromptTokens =
            "plastic, CGI, 3D render, anime, illustration, painting, toy plastic, " +
            "flat textures, low quality, low resolution, watermark, text, logo, " +
            "duplicate limbs, extra fingers, bad anatomy, distorted face, " +
            "oversaturated colors, artificial skin, shiny plastic eyes, " +
            "blurry, deformed, cropped, out of frame, disfigured, " +
            "poorly drawn, mutation, mutated, ugly, bad proportions";

        // ──────────────────────────────────────────────────────────────────────
        // Image Diversity — Variation cues to generate diverse but consistent designs
        // ──────────────────────────────────────────────────────────────────────
        private static readonly string[] VariationCues = new[]
        {
            "Slightly tilted head pose, gentle warm front lighting, soft smile expression.",
            "Three-quarter view angle, side lighting with soft fill, neutral calm expression.",
            "Straight-on front pose, overhead diffused lighting, cheerful bright expression."
        };

        public PromptBuildResult BuildPrompt(CustomConfiguration configuration)
        {
            return BuildPromptWithVariation(configuration, 0);
        }

        public PromptBuildResult BuildPromptWithVariation(CustomConfiguration configuration, int variationIndex)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.ProductType == ProductType.CrochetDoll)
            {
                return BuildCrochetDollPrompt(configuration.ConfigurationDataJson, variationIndex);
            }

            return BuildGenericPrompt(configuration.ProductType, configuration.ConfigurationDataJson, variationIndex);
        }

        private PromptBuildResult BuildCrochetDollPrompt(string json, int variationIndex)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<CrochetDollConfiguration>(json, options);

                if (config == null)
                {
                    return BuildFallbackPrompt(variationIndex);
                }

                // Photo Mode: Different prompt strategy for uploaded person photos
                if (!string.IsNullOrEmpty(config.ReferenceImageUrl))
                {
                    return BuildPhotoModePrompt(config, variationIndex);
                }

                // Manual Customization Mode: Full 4-layer prompt
                return BuildManualCustomizationPrompt(config, variationIndex);
            }
            catch (Exception)
            {
                return BuildFallbackPrompt(variationIndex);
            }
        }

        /// <summary>
        /// Photo Mode: Generates a prompt for uploaded person photo → crochet doll conversion.
        /// Preserves personality traits while transforming into handcrafted crochet.
        /// </summary>
        private PromptBuildResult BuildPhotoModePrompt(CrochetDollConfiguration config, int variationIndex)
        {
            var sb = new StringBuilder();

            // Layer 1: Brand Identity
            sb.Append(BrandIdentityLayer);
            sb.Append(' ');

            // Step 2: Custom Request photo-to-doll prompt
            sb.Append("Create one premium handmade crochet amigurumi doll inspired by the provided person photo. ");
            sb.Append("The doll must look like a high-end handcrafted product with realistic yarn texture, visible crochet stitches, and adorable proportions. ");
            sb.Append("The doll must match the person exactly in the following aspects: ");
            sb.AppendLine();

            string negativePrompt = NegativePromptTokens;

            if (!string.IsNullOrEmpty(config.AdditionalNotes) && config.AdditionalNotes.StartsWith("[PHOTO_ANALYSIS]: "))
            {
                var geminiJson = config.AdditionalNotes.Substring("[PHOTO_ANALYSIS]: ".Length);
                var formatted = FormatLockedAttributes(geminiJson);
                sb.Append(formatted);

                try
                {
                    using var doc = JsonDocument.Parse(geminiJson);
                    var root = doc.RootElement;

                    // Hijab coverage rule: If Hijab/Scarf is visible, make sure the negative prompt excludes hair!
                    if (root.TryGetProperty("hairOrHeadCoverage", out var headCov) &&
                        headCov.TryGetProperty("headCovered", out var headCovered) && headCovered.GetString() == "Yes")
                    {
                        negativePrompt += ", hair, hair showing, hairline visible";
                    }

                    // No-glasses rule: If the person is NOT wearing glasses, explicitly exclude them
                    // to prevent the AI from hallucinating glasses on the doll.
                    if (root.TryGetProperty("personIdentity", out var identity) &&
                        identity.TryGetProperty("glasses", out var glasses) && glasses.GetString() != "Yes")
                    {
                        negativePrompt += ", glasses, eyeglasses, spectacles, eyewear, frames on face";
                        sb.Append("IMPORTANT: The person is NOT wearing glasses. The doll must NOT have any glasses, eyeglasses, spectacles, or eyewear of any kind. ");
                    }
                }
                catch {}
            }
            else
            {
                sb.AppendLine("- Character Details: Inspired by reference photo.");
            }

            // Layer 2: Product Photography
            sb.Append(ProductPhotographyLayer);
            sb.Append(' ');

            // Layer 4: Crochet Details
            sb.Append(CrochetDetailsLayer);
            sb.Append(' ');

            // Visual Style Requirements (from system prompt diagram)
            sb.Append("Visual style requirements: Premium crochet amigurumi doll, soft cotton yarn, visible crochet stitches, handmade details, cute but realistic proportions, professional product photography, soft warm studio lighting, clean minimal background, 8K ultra-detailed, luxury artisan product, natural shadows, high quality, realistic yarn texture. ");

            // Variation cue for diversity
            sb.Append(GetVariationCue(variationIndex));

            // Negative prompt requirements (from system prompt diagram)
            var photoModeNegativePrompt = "real human, plastic, 3d render look, anime, cartoon, illustration, unrealistic face, wrong hijab style, hair if hair was not visible, body parts missing or extra, text, watermark, logo, blurry, low quality, changed clothing style, changed accessories, broken modesty, guessed cultural or religious attributes, " + negativePrompt;

            return new PromptBuildResult
            {
                PositivePrompt = sb.ToString(),
                NegativePrompt = photoModeNegativePrompt
            };
        }

        private string FormatLockedAttributes(string geminiJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(geminiJson);
                var root = doc.RootElement;

                var sb = new StringBuilder();

                // Person Identity
                if (root.TryGetProperty("personIdentity", out var identity))
                {
                    if (identity.TryGetProperty("gender", out var gender)) sb.AppendLine($"- Gender: {gender.GetString()}");
                    if (identity.TryGetProperty("skinTone", out var skinTone)) sb.AppendLine($"- Skin Tone: {skinTone.GetString()}");
                    if (identity.TryGetProperty("faceShape", out var faceShape)) sb.AppendLine($"- Face Shape: {faceShape.GetString()}");
                    if (identity.TryGetProperty("expression", out var expression)) sb.AppendLine($"- Expression: {expression.GetString()}");
                    if (identity.TryGetProperty("glasses", out var glasses))
                    {
                        var hasGlasses = glasses.GetString();
                        // Only mention glasses when the person IS wearing them.
                        // Mentioning "Glasses: No" paradoxically primes the AI to add glasses.
                        if (hasGlasses == "Yes")
                        {
                            sb.Append("- Glasses: Yes");
                            if (identity.TryGetProperty("glassesDetails", out var glassesDetails))
                            {
                                sb.Append($" ({glassesDetails.GetString()})");
                            }
                            sb.AppendLine();
                        }
                    }
                    if (identity.TryGetProperty("facialHair", out var facialHair))
                    {
                        var hasFacialHair = facialHair.GetString();
                        if (hasFacialHair == "Yes" && identity.TryGetProperty("facialHairDetails", out var facialHairDetails))
                        {
                            sb.AppendLine($"- Facial Hair: {facialHairDetails.GetString()}");
                        }
                    }
                }

                // Hair / Head Coverage
                if (root.TryGetProperty("hairOrHeadCoverage", out var hairOrHead))
                {
                    sb.Append("- Hair / Head Coverage: ");
                    if (hairOrHead.TryGetProperty("hairVisible", out var hairVisible) && hairVisible.GetString() == "Yes")
                    {
                        sb.Append("Hair is visible");
                        if (hairOrHead.TryGetProperty("hairStyle", out var hs)) sb.Append($", Style: {hs.GetString()}");
                        if (hairOrHead.TryGetProperty("hairLength", out var hl)) sb.Append($", Length: {hl.GetString()}");
                        if (hairOrHead.TryGetProperty("hairColor", out var hc)) sb.Append($", Color: {hc.GetString()}");
                    }
                    else
                    {
                        sb.Append("Hair is not visible");
                        if (hairOrHead.TryGetProperty("headCovered", out var headCovered) && headCovered.GetString() == "Yes")
                        {
                            sb.Append(", Head is covered");
                            if (hairOrHead.TryGetProperty("coverType", out var ct)) sb.Append($", Cover Type: {ct.GetString()}");
                            if (hairOrHead.TryGetProperty("hijabOrScarfStyle", out var hss)) sb.Append($", Hijab/Scarf Style: {hss.GetString()}");
                            if (hairOrHead.TryGetProperty("hijabOrScarfColors", out var hsc)) sb.Append($", Colors: {hsc.GetString()}");
                        }
                    }
                    sb.AppendLine();
                }

                // Clothing
                if (root.TryGetProperty("clothing", out var clothing))
                {
                    sb.Append("- Outfit: ");
                    if (clothing.TryGetProperty("topType", out var tt)) sb.Append($"{tt.GetString()}");
                    if (clothing.TryGetProperty("topColor", out var tc)) sb.Append($" ({tc.GetString()})");
                    if (clothing.TryGetProperty("bottomType", out var bt)) sb.Append($" with {bt.GetString()}");
                    if (clothing.TryGetProperty("bottomColor", out var bc)) sb.Append($" ({bc.GetString()})");
                    if (clothing.TryGetProperty("fullOutfitStyle", out var fos)) sb.Append($", Overall Style: {fos.GetString()}");
                    sb.AppendLine();
                }

                // Accessories
                if (root.TryGetProperty("accessories", out var accessories))
                {
                    sb.Append("- Accessories: ");
                    var accList = new System.Collections.Generic.List<string>();
                    if (accessories.TryGetProperty("headAccessories", out var ha) && ha.GetString() != "None")
                    {
                        // Filter out "Glasses" from headAccessories when the person doesn't wear glasses,
                        // to avoid contradicting the glasses=No signal from personIdentity.
                        var headAccVal = ha.GetString() ?? "";
                        bool personWearsGlasses = false;
                        if (root.TryGetProperty("personIdentity", out var pid) &&
                            pid.TryGetProperty("glasses", out var gl) && gl.GetString() == "Yes")
                        {
                            personWearsGlasses = true;
                        }
                        if (!personWearsGlasses)
                        {
                            // Remove any mention of glasses from headAccessories
                            headAccVal = System.Text.RegularExpressions.Regex.Replace(headAccVal, @"\b[Gg]lasses\b", "").Trim(' ', ',', '/');
                        }
                        if (!string.IsNullOrWhiteSpace(headAccVal) && headAccVal != "None")
                        {
                            accList.Add($"Headwear: {headAccVal}");
                        }
                    }
                    if (accessories.TryGetProperty("jewelry", out var j) && j.GetString() != "None") accList.Add($"Jewelry: {j.GetString()}");
                    if (accessories.TryGetProperty("bagOrPurse", out var bp) && bp.GetString() != "No") accList.Add($"Bag: {bp.GetString()}");
                    if (accessories.TryGetProperty("shoes", out var s) && s.GetString() != "None") accList.Add($"Shoes: {s.GetString()}");
                    if (accessories.TryGetProperty("otherAccessories", out var oa) && oa.GetString() != "None") accList.Add($"Other: {oa.GetString()}");

                    if (accList.Count > 0) sb.Append(string.Join(", ", accList));
                    else sb.Append("None");
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch
            {
                return "- Character Details: Inspired by reference photo.\n";
            }
        }

        /// <summary>
        /// Manual Customization Mode: Full 4-layer prompt with dynamic character details.
        /// Faithfully follows every selected detail — never invents additional accessories,
        /// changes colors, or replaces outfit choices.
        /// </summary>
        private PromptBuildResult BuildManualCustomizationPrompt(CrochetDollConfiguration config, int variationIndex)
        {
            var sb = new StringBuilder();

            // ── Layer 1: Brand Identity ──
            sb.Append(BrandIdentityLayer);
            sb.Append(' ');

            // ── Layer 2: Product Photography ──
            sb.Append(ProductPhotographyLayer);
            sb.Append(' ');

            // ── Layer 3: Character Details (Dynamic) ──
            sb.Append("The doll has the following custom character specifications that must be followed exactly: ");

            // Gender
            sb.Append($"It is a {config.Gender.ToString().ToLower()} character. ");

            // Body & Size
            sb.Append($"The doll size is approximately {config.Size} tall, with a {config.BodyType.ToString().ToLower()} body type. ");

            // Skin Tone
            if (!string.IsNullOrWhiteSpace(config.SkinTone))
            {
                sb.Append($"It has a beautiful {config.SkinTone.ToLower()} skin tone. ");
            }

            // Hair
            if (config.Hair != null)
            {
                if (config.Hair.Style == HairStyle.Bald)
                {
                    sb.Append("The doll has a bald head. ");
                }
                else
                {
                    sb.Append($"The doll has {config.Hair.Length.ToLower()} {config.Hair.Color.ToLower()} hair, styled in a {config.Hair.Style.ToString().ToLower()} fashion. ");
                }
            }

            // Face
            if (config.Face != null)
            {
                if (!string.IsNullOrWhiteSpace(config.Face.EyeShape))
                {
                    sb.Append($"The face features detailed {config.Face.EyeShape.ToLower()} eyes");
                    if (!string.IsNullOrWhiteSpace(config.Face.EyeColor))
                    {
                        sb.Append($" with a beautiful {config.Face.EyeColor.ToLower()} color");
                    }
                    sb.Append(". ");
                }

                if (!string.IsNullOrWhiteSpace(config.Face.Smile))
                {
                    sb.Append($"It has a friendly {config.Face.Smile.ToLower()} expression. ");
                }

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

            // Outfit
            if (config.Outfit != null && !string.IsNullOrWhiteSpace(config.Outfit.Description))
            {
                sb.Append($"The doll is dressed in a custom {config.Outfit.Type.ToString().ToLower()} outfit described as: {config.Outfit.Description.Trim().TrimEnd('.')}. ");
            }

            // Accessories
            if (config.Accessories != null && config.Accessories.Type != AccessoryType.None && !string.IsNullOrWhiteSpace(config.Accessories.Description))
            {
                sb.Append($"It is accessorized with a custom {config.Accessories.Type.ToString().ToLower()} described as: {config.Accessories.Description.Trim().TrimEnd('.')}. ");
            }

            // Personalization
            if (config.Personalization != null && !string.IsNullOrWhiteSpace(config.Personalization.LabelText))
            {
                sb.Append($"There is a small personalized embroidered tag nearby displaying the text '{config.Personalization.LabelText}' in a {config.Personalization.Font.ToString().ToLower()} font type. ");
            }

            // Additional Notes
            if (!string.IsNullOrWhiteSpace(config.AdditionalNotes))
            {
                sb.Append($"Special styling details: {config.AdditionalNotes.Trim().TrimEnd('.')}. ");
            }

            // ── Layer 4: Crochet Details ──
            sb.Append(CrochetDetailsLayer);
            sb.Append(' ');

            // Variation cue for diversity
            sb.Append(GetVariationCue(variationIndex));

            return new PromptBuildResult
            {
                PositivePrompt = sb.ToString(),
                NegativePrompt = NegativePromptTokens
            };
        }

        private PromptBuildResult BuildGenericPrompt(ProductType type, string json, int variationIndex)
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
                    var value = prop.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        sb.Append($"{prop.Name}: {value}, ");
                    }
                }
            }
            catch
            {
                sb.Append("Fully configured with custom options. ");
            }

            sb.Append(ProductPhotographyLayer);
            sb.Append(' ');
            sb.Append(CrochetDetailsLayer);
            sb.Append(' ');
            sb.Append(GetVariationCue(variationIndex));

            return new PromptBuildResult
            {
                PositivePrompt = sb.ToString(),
                NegativePrompt = NegativePromptTokens
            };
        }

        private PromptBuildResult BuildFallbackPrompt(int variationIndex)
        {
            var sb = new StringBuilder();
            sb.Append(BrandIdentityLayer);
            sb.Append(' ');
            sb.Append(ProductPhotographyLayer);
            sb.Append(' ');
            sb.Append(CrochetDetailsLayer);
            sb.Append(' ');
            sb.Append(GetVariationCue(variationIndex));

            return new PromptBuildResult
            {
                PositivePrompt = sb.ToString(),
                NegativePrompt = NegativePromptTokens
            };
        }

        private static string GetVariationCue(int variationIndex)
        {
            if (variationIndex < 0 || variationIndex >= VariationCues.Length)
            {
                return VariationCues[0];
            }
            return VariationCues[variationIndex];
        }
    }
}
