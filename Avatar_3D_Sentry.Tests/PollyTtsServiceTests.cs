using Avatar_3D_Sentry.Services;
using Xunit;

namespace Avatar_3D_Sentry.Tests;

public class PollyTtsServiceTests
{
    [Fact]
    public void MapVisemeToShapes_ReturnsEntriesForAllKnownVisemes()
    {
        var time = 123;

        foreach (var viseme in PollyTtsService._visemeToShapes.Keys)
        {
            var mapped = PollyTtsService.MapVisemeToShapes(viseme, time);

            Assert.NotEmpty(mapped);
            Assert.All(mapped, item =>
            {
                Assert.Equal(time, item.Tiempo);
                Assert.False(string.IsNullOrWhiteSpace(item.ShapeKey));
            });
        }
    }
}
