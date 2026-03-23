using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IPackageClassificationService
{
    PackageCategory Classify(PackageInfo packageInfo, string? setupPyContent = null);

    string DetectMaturity(string packageFolder, string? packageId = null);
}