using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DirectoryService.Domain.Departments
{
    public record DepartmentName
    {
        public const int NAME_MIN_LENGHT = 3;
        public const int NAME_MAX_LENGHT = 150;

        public string Value { get; } = string.Empty;

        private DepartmentName(string value)
        {
            Value = value;
        }

        public static DepartmentName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length is < NAME_MIN_LENGHT or > NAME_MAX_LENGHT)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return new DepartmentName(value);
        }
    }
}
