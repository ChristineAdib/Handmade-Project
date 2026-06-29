using System;

namespace HandoraApplication.AI.Exceptions
{
    public class AIException : Exception
    {
        public AIException(string message) : base(message) { }
        public AIException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AIProviderUnavailableException : AIException
    {
        public AIProviderUnavailableException(string message) : base(message) { }
        public AIProviderUnavailableException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AIQuotaExceededException : AIException
    {
        public AIQuotaExceededException(string message) : base(message) { }
        public AIQuotaExceededException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AITimeoutException : AIException
    {
        public AITimeoutException(string message) : base(message) { }
        public AITimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AIInvalidPromptException : AIException
    {
        public AIInvalidPromptException(string message) : base(message) { }
        public AIInvalidPromptException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AIInvalidImageException : AIException
    {
        public AIInvalidImageException(string message) : base(message) { }
        public AIInvalidImageException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AINetworkException : AIException
    {
        public AINetworkException(string message) : base(message) { }
        public AINetworkException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AIRateLimitException : AIException
    {
        public AIRateLimitException(string message) : base(message) { }
        public AIRateLimitException(string message, Exception innerException) : base(message, innerException) { }
    }
}
