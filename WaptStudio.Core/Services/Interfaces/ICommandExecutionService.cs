using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface ICommandExecutionService
{
    Task<CommandExecutionResult> ExecuteAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        CommandExecutionOptions? options = null,
        CancellationToken cancellationToken = default);
}
