using Xunit;

namespace BiMaDock.Tests;

public class CategoryDockPositionerTests
{
    [Fact]
    public void CalculateLeftMargin_CentersUnderCategoryButton()
    {
        // Hauptdock 800 breit, Kategorie-Element bei 400, Kategoriebar 200 breit
        Assert.Equal(300, CategoryDockPositioner.CalculateLeftMargin(400, 800, 200));
    }

    [Fact]
    public void CalculateLeftMargin_StaysInsideLeftEdgeOfMainDock()
    {
        Assert.Equal(0, CategoryDockPositioner.CalculateLeftMargin(30, 800, 200));
    }

    [Fact]
    public void CalculateLeftMargin_StaysInsideRightEdgeOfMainDock()
    {
        Assert.Equal(600, CategoryDockPositioner.CalculateLeftMargin(780, 800, 200));
    }

    [Fact]
    public void CalculateLeftMargin_ButtonScrolledOutOfViewIsClamped()
    {
        Assert.Equal(600, CategoryDockPositioner.CalculateLeftMargin(5000, 800, 200));
        Assert.Equal(0, CategoryDockPositioner.CalculateLeftMargin(-300, 800, 200));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(400)]
    [InlineData(750)]
    public void CalculateLeftMargin_WiderThanMainDockIsCenteredUnderMainDock(double buttonCenter)
    {
        // Der Container ist dann so breit wie die Kategoriebar; Rand 0 bedeutet mittig unter dem Hauptdock.
        Assert.Equal(0, CategoryDockPositioner.CalculateLeftMargin(buttonCenter, 800, 1000));
    }

    [Fact]
    public void CalculateLeftMargin_SameWidthAsMainDockIsFlush()
    {
        Assert.Equal(0, CategoryDockPositioner.CalculateLeftMargin(100, 800, 800));
    }
}
