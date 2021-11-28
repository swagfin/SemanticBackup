using System;

namespace SemanticBackup.API.Services
{
    public interface IServerSharedRuntime
    {
        TimeZoneInfo CurrentTimeZone { get; }
        DateTime GetServerTime { get; }
    }
}