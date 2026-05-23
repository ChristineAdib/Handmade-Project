using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Helpers.AuthHelper
{
    public sealed class JwtOptions
    {
        public const string SectionName = "JwtOptions";

        [Required] public string Key { get; init; } = string.Empty;
        [Required] public string Issuer { get; init; } = string.Empty;
        [Required] public string Audience { get; init; } = string.Empty;
        public int DurationInDays { get; init; } = 7;
    }
}
