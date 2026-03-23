using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IPackageUpdateService
{
    Task<PackageUpdateResult> ReplaceInstallerAsync(
        PackageInfo packageInfo,
        string newInstallerFilePath,
        CancellationToken cancellationToken = default);
}
