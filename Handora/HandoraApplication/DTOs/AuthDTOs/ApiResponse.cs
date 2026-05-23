using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AuthDTOs
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public T? Data { get; private init; }
        public IEnumerable<string>? Errors { get; private init; }

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }
}
