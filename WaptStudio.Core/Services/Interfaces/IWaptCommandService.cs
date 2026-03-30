using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IWaptCommandService
{
    Task<CommandExecutionResult> CheckWaptAvailabilityAsync(CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> ValidatePackageWithWaptAsync(string packageFolder, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> SignPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, string? waptFilePath = null, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> AuditPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default);

    Task<CommandExecutionResult> UninstallPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default);
}
