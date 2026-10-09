using System.Windows;
using Xunit;

namespace BiMaDock.Tests;

public class CategoryDockFlowAnimationTests
{
    [Fact]
    public void GetItemDelay_FirstItemStartsImmediately()
    {
        Assert.Equal(0, CategoryDockFlowAnimation.GetItemDelay(0, 5, 300));
    }

    [Fact]
    public void GetItemDelay_SingleItemHasNoDelay()
    {
        Assert.Equal(0, CategoryDockFlowAnimation.GetItemDelay(0, 1, 300));
    }

    [Fact]
    public void GetItemDelay_FewItemsUseMaximumStagger()
    {
        // 300 ms: Element dauert 180 ms, 120 ms stehen für den Versatz zur Verfügung; begrenzt auf 40 ms
        Assert.Equal(40, CategoryDockFlowAnimation.GetItemDelay(1, 3, 300), 6);
        Assert.Equal(80, CategoryDockFlowAnimation.GetItemDelay(2, 3, 300), 6);
    }

    [Theory]
    [InlineData(2, 150)]
    [InlineData(10, 300)]
    [InlineData(100, 300)]
    [InlineData(500, 800)]
    public void GetItemDelay_LastItemFinishesWithinTotalDuration(int count, double total)
    {
        double end = CategoryDockFlowAnimation.GetItemDelay(count - 1, count, total)
            + CategoryDockFlowAnimation.GetItemDuration(total);
        Assert.True(end <= total + 1e-9, $"Ende {end} ms überschreitet {total} ms");
    }

    [Fact]
    public void GetItemDelay_ManyItemsShrinkStagger()
    {
        double fewStep = CategoryDockFlowAnimation.GetItemDelay(1, 3, 300);
        double manyStep = CategoryDockFlowAnimation.GetItemDelay(1, 50, 300);
        Assert.True(manyStep < fewStep);
    }

    [Fact]
    public void GetItemDelay_IsMonotonic()
    {
        double previous = -1;
        for (int index = 0; index < 20; index++)
        {
            double delay = CategoryDockFlowAnimation.GetItemDelay(index, 20, 300);
            Assert.True(delay >= previous);
            previous = delay;
        }
    }

    [Fact]
    public void GetItemDuration_IsShareOfTotal()
    {
        Assert.Equal(180, CategoryDockFlowAnimation.GetItemDuration(300), 6);
        Assert.Equal(0, CategoryDockFlowAnimation.GetItemDuration(-5));
    }

    [Fact]
    public void GetStartOffset_MovesItemCenterOntoOrigin()
    {
        var origin = new Point(120, -40);
        var itemCenter = new Point(300, 30);

        Vector offset = CategoryDockFlowAnimation.GetStartOffset(origin, itemCenter);

        Assert.Equal(origin, itemCenter + offset);
        Assert.Equal(new Vector(-180, -70), offset);
    }
}
