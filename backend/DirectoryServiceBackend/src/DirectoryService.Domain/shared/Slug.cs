using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DirectoryService.Domain.shared
{
    public sealed partial record Slug
    {

        public const int IDENTIFIER_MIN_LENGHT = 1;
        public const int IDENTIFIER_MAX_LENGHT = 150;

        public string Value { get; }

        private Slug(string value)
        {
            Value = value;
        }

        public static Slug Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!_slugRegex.IsMatch(value))
            {
                throw new ArgumentException("Значение не соответствует шаблону", nameof(value));
            }

            if (value.Length < IDENTIFIER_MIN_LENGHT || value.Length > IDENTIFIER_MAX_LENGHT)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return new Slug(value);
        }

        [GeneratedRegex("^[a-zA-Z]+$", RegexOptions.Compiled)]
        private static partial Regex _slugRegex { get; }
    }
}
