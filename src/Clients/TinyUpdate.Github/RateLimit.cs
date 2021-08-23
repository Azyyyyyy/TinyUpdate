using System;

namespace TinyUpdate.Github
{
    public readonly struct RateLimit
    {
        public RateLimit(bool isRateLimited, DateTime? resetTime = null)
        {
            ResetTime = resetTime;
            IsRateLimited = isRateLimited;
        }

        /// <summary>
        /// If we have been rate limited
        /// </summary>
        public bool IsRateLimited { get; }

        /// <summary>
        /// When we can talk to the server again
        /// </summary>
        public DateTime? ResetTime { get; }
    }
}