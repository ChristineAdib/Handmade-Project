using System;
using System.Text.RegularExpressions;

namespace HandoraApplication.Helpers;

public static class ChatValidationHelper
{
    // Egyptian mobile phone number regex pattern:
    // Matches local formats: 010xxxxxxxx, 011xxxxxxxx, 012xxxxxxxx, 015xxxxxxxx
    // Matches international formats: +2010xxxxxxxx, 002010xxxxxxxx (with or without separators like spaces, dashes, dots)
    private static readonly Regex EgyptianPhoneRegex = new Regex(
        @"(?:\+?20|0020)?\s*0?1[0125](?:\s*[.\- ]?\s*\d){8}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // URL and Link regex pattern:
    // Matches http://, https://, www.
    // Matches any domain ending with specified TLDs (.com, .net, .org, .io, .co, .app, .dev, .me, .shop, .store, .eg)
    // Matches social media patterns: facebook, instagram, tiktok, twitter, youtube, telegram, whatsapp, wa.me, t.me
    private static readonly Regex LinkRegex = new Regex(
        @"\b(?:https?://|www\.)\S+|\b[a-zA-Z0-9.-]+\.(?:com|net|org|io|co|app|dev|me|shop|store|eg)\b(?:\/\S*)?|\b(?:facebook|instagram|tiktok|twitter|youtube|telegram|whatsapp|wa\.me|t\.me)\b(?:\/\S*)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool ContainsPhoneNumber(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return EgyptianPhoneRegex.IsMatch(content);
    }

    public static bool ContainsLinks(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return LinkRegex.IsMatch(content);
    }

    public static bool IsSelfMessaging(string? user1Id, string? user2Id)
    {
        if (string.IsNullOrWhiteSpace(user1Id) || string.IsNullOrWhiteSpace(user2Id))
            return false;

        return user1Id.Equals(user2Id, StringComparison.OrdinalIgnoreCase);
    }
}
