using WaptStudio.Core.Models;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PublicationPreparationResultTests
{
    [Fact]
    public void CanPrepareDirectUpload_RequiresRealWaptFileAndDirectUploadAvailability()
    {
        var blockedByUpload = new PublicationPreparationResult
        {
            PackageReady = true,
            HasRealWaptFile = true,
            DirectUploadAvailable = false
        };

        var available = new PublicationPreparationResult
        {
            PackageReady = true,
            HasRealWaptFile = true,
            DirectUploadAvailable = true
        };

        Assert.False(blockedByUpload.CanPrepareDirectUpload);
        Assert.True(available.CanPrepareDirectUpload);
    }
}