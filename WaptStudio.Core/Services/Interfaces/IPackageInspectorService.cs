using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IPackageInspectorService
{
    Task<PackageInfo> AnalyzePackageAsync(string packageFolder, CancellationToken cancellationToken = default);
}
