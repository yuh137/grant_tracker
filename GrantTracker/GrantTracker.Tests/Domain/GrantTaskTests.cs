using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;
using static GrantTracker.Tests.Domain.TestGrants;

namespace GrantTracker.Tests.Domain;

public class GrantTaskTests
{ 
    [Fact]
    public void Complete_RecordsTodayAsCompletionDate()
    {
        var task = NewTestTask(TestGrantOpen(), TaskPriority.Low, TestToday.AddDays(2));

        var changed = task.Complete(TestToday);

        Assert.True(changed);
        Assert.Equal(TestToday, task.CompletedOn);
        Assert.True(task.IsCompleted);
        Assert.False(task.IsPending);
    }

    [Fact]
    public void Complete_OnAlreadyCompletedTask_KeepsOriginalDate()
    {
        var task = NewTestTask(TestGrantOpen(), TaskPriority.Low, TestToday.AddDays(2));
        task.Complete(TestToday);

        var changed = task.Complete(TestToday.AddDays(7));

        Assert.False(changed);
        Assert.Equal(TestToday, task.CompletedOn);
    }

    [Theory]
    [InlineData(-1, false, true)]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(-1, true, false)]
    public void IsOverdue_PendingAndDueBeforeToday(int dueDaysFromToday, bool completed, bool expected)
    {
        var dueDate = TestToday.AddDays(dueDaysFromToday);
        var task = NewTestTask(TestGrantOpen(), TaskPriority.Low, dueDate, completedOn: completed ? dueDate : null);

        Assert.Equal(expected, task.IsOverdue(TestToday));
        Assert.Equal(expected, GrantTask.OverdueOn(TestToday).Compile()(task));
    }

    [Fact]
    public void ValidateTitle_TrimsSurroundingWhitespace()
    {
        Assert.Equal("Review report", GrantTask.ValidateTitle("  Review report \t"));
    }
}
