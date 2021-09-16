using System;
using System.Runtime.CompilerServices;

namespace Driver
{
    public static class Log
    {
        public static void Info(string message, [CallerMemberName] string? caller = null) => InternalLog(message, "INFO", caller);
        public static void Warn(string message, [CallerMemberName] string? caller = null) => InternalLog(message, "WARN", caller);
        public static void Error(string message, [CallerMemberName] string? caller = null) => InternalLog(message, "ERROR", caller);

        static void InternalLog(string message, string? level, string? caller) => 
            Console.WriteLine($"{DateTime.UtcNow:O} {level} ({caller}): {message}");
    }
}