using System;
using System.Collections.Generic;
using HandoraDomain.Consts;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public record HairConfiguration(
        HairStyle Style,
        string Color,
        string Length
    );

    public record FaceConfiguration(
        string EyeShape,
        string EyeColor,
        string Smile,
        bool Freckles,
        bool Blush
    );

    public record OutfitConfiguration(
        OutfitType Type,
        string Description
    );

    public record AccessoryConfiguration(
        AccessoryType Type,
        string Description
    );

    public record PersonalizationConfiguration(
        string LabelText,
        FontType Font
    );

    public record CrochetDollConfiguration(
        Gender Gender,
        string Size,
        BodyType BodyType,
        string SkinTone,
        HairConfiguration Hair,
        FaceConfiguration Face,
        OutfitConfiguration Outfit,
        AccessoryConfiguration Accessories,
        PersonalizationConfiguration Personalization,
        string? ReferenceImageUrl,
        string? AdditionalNotes
    );
}
