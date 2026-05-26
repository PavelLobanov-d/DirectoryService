using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.shared
{
    public class DSException : Exception
    {
        public DSException(string message) : base(message) { }

        public DSException() : base()
        {
        }

        public DSException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
