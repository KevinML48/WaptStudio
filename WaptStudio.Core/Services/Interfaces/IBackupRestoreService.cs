using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IBackupRestoreService
{
    Task<PackageBackupInfo> CreatePackageBackupAsync(PackageInfo packageInfo, string reason, CancellationToken cancellationToken = default);

    Task<PackageBackupInfo?> GetLatestBackupAsync(PackageInfo packageInfo, CancellationToken cancellationToken = default);

    Task<PackageRestoreResult> RestoreLatestBackupAsync(PackageInfo packageInfo, CancellationToken cancellationToken = default);
}