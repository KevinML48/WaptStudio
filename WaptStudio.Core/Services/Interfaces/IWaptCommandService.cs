using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IWaptCommandService
{
    Task<CommandExecutionResult> CheckWaptAvailabilityAsync(CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> ValidatePackageWithWaptAsync(string packageFolder, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> SignPackageAsync(string packageFolder, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, CancellationToken cancellationToken = default);
}
