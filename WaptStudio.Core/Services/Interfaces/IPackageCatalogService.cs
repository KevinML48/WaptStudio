using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IPackageCatalogService
{
    Task<IReadOnlyList<PackageCatalogItem>> ScanAsync(string rootFolder, bool recursive, int semiRecursiveDepth, CancellationToken cancellationToken = default);
}