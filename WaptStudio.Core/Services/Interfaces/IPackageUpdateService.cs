using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IPackageUpdateService
{
    Task<PackageSynchronizationPlan> PreviewReplacementAsync(
        PackageInfo packageInfo,
        string newInstallerFilePath,
        PackageVersionSelection? versionSelection = null,
        CancellationToken cancellationToken = default);

    Task<PackageUpdateResult> ReplaceInstallerAsync(
        PackageInfo packageInfo,
        string newInstallerFilePath,
        PackageVersionSelection? versionSelection = null,
        CancellationToken cancellationToken = default);
}
