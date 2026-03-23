using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IPackageValidationService
{
    Task<ValidationResult> ValidateAsync(string packageFolder, PackageInfo? packageInfo = null, CancellationToken cancellationToken = default);
}
