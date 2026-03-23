using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services.Interfaces;

public interface IHistoryService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task AddEntryAsync(HistoryEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryEntry>> GetRecentEntriesAsync(int take = 100, CancellationToken cancellationToken = default);

    Task<HistoryEntry?> GetEntryByIdAsync(long id, CancellationToken cancellationToken = default);
}
