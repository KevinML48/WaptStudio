using System;
using System.Threading;
using System.Threading.Tasks;

namespace WaptStudio.Core.Services.Interfaces;

public interface ILogService
{
    Task LogInfoAsync(string message, CancellationToken cancellationToken = default);

    Task LogWarningAsync(string message, CancellationToken cancellationToken = default);

    Task LogErrorAsync(string message, Exception? exception = null, CancellationToken cancellationToken = default);
}
