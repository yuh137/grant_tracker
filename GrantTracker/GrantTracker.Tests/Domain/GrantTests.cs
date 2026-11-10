using GrantTracker.Domain;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Tests.Domain;

public class GrantTests
{
    [Fact]
    public void AddTask_OnClosedGrant()
    {
        var grant = TestGrants.TestGrantClose();

        var ex = Assert.Throws<BusinessRuleException>(() =>
            grant.AddTask("Verify bank details", TaskPriority.Low, TestGrants.TestToday.AddDays(1), TestGrants.TestNow));

        Assert.Equal(BusinessRuleErrorCode.GrantClosed, ex.ErrorCode);
        Assert.Empty(grant.Tasks);
    }

    [Fact]
    public void AddTask_OnOpenGrant_AddsTaskAndBumpsVersion()
    {
        var grant = TestGrants.TestGrantOpen();

        var task = grant.AddTask("  Verify bank details  ", TaskPriority.High, TestGrants.TestToday.AddDays(5), TestGrants.TestNow);

        Assert.Single(grant.Tasks, task);
        Assert.Equal(grant.Id, task.GrantId);
        Assert.Equal("Verify bank details", task.Title);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(TestGrants.TestToday.AddDays(5), task.DueDate);
        Assert.Equal(TestGrants.TestNow, task.CreatedOn);
        Assert.Null(task.CompletedOn);
        Assert.Equal(1, grant.Version);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void AddTask_DueDateRelativeToToday_RejectsPastDates(int daysFromToday, bool accepted)
    {
        var grant = TestGrants.TestGrantOpen();
        var dueDate = TestGrants.TestToday.AddDays(daysFromToday);

        if (accepted)
        {
            var task = grant.AddTask("Task", TaskPriority.Low, dueDate, TestGrants.TestNow);
            Assert.Equal(dueDate, task.DueDate);
        }
        else
        {
            var ex = Assert.Throws<BusinessRuleException>(() => grant.AddTask("Task", TaskPriority.Low, dueDate, TestGrants.TestNow));
            Assert.Equal(BusinessRuleErrorCode.InvalidDueDate, ex.ErrorCode);
        }
    }

    [Fact]
    public void AddTask_DueBeforeApplicationDate_Throws()
    {
        var grant = TestGrants.TestGrantOpen(applicationDate: TestGrants.TestToday.AddDays(10));

        var ex = Assert.Throws<BusinessRuleException>(() => grant.AddTask("Task", TaskPriority.Low, TestGrants.TestToday.AddDays(5), TestGrants.TestNow));

        Assert.Equal(BusinessRuleErrorCode.InvalidDueDate, ex.ErrorCode);
        Assert.Contains("application date", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTask_MissingTitle_Throws(string? title)
    {
        var grant = TestGrants.TestGrantOpen();

        var ex = Assert.Throws<BusinessRuleException>(() => grant.AddTask(title, TaskPriority.Low, TestGrants.TestToday, TestGrants.TestNow));

        Assert.Equal(BusinessRuleErrorCode.InvalidTitle, ex.ErrorCode);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void AddTask_TitleLength_AllowsAtMost100Characters(int length, bool accepted)
    {
        var grant = TestGrants.TestGrantOpen();
        var title = new string('x', length);

        if (accepted)
        {
            Assert.Equal(title, grant.AddTask(title, TaskPriority.Low, TestGrants.TestToday, TestGrants.TestNow).Title);
        }
        else
        {
            var ex = Assert.Throws<BusinessRuleException>(() => grant.AddTask(title, TaskPriority.Low, TestGrants.TestToday, TestGrants.TestNow));
            Assert.Equal(BusinessRuleErrorCode.InvalidTitle, ex.ErrorCode);
        }
    }

    [Fact]
    public void Close_WithPendingHighTask_Throws()
    {
        var grant = TestGrants.TestGrantOpen();
        TestGrants.NewTestTask(grant, TaskPriority.High, TestGrants.TestToday.AddDays(3));

        var ex = Assert.Throws<BusinessRuleException>(() => grant.Close());

        Assert.Equal(BusinessRuleErrorCode.PendingHighPriorityTask, ex.ErrorCode);
        Assert.Equal(GrantStatus.Open, grant.Status);
    }

    [Fact]
    public void Close_WithCompletedHighAndPendingMediumTasks_Closes()
    {
        var grant = TestGrants.TestGrantOpen();
        TestGrants.NewTestTask(grant, TaskPriority.High, TestGrants.TestToday.AddDays(-10), completedOn: TestGrants.TestToday.AddDays(-10));
        TestGrants.NewTestTask(grant, TaskPriority.Medium, TestGrants.TestToday.AddDays(3));

        var changed = grant.Close();

        Assert.True(changed);
        Assert.Equal(GrantStatus.Closed, grant.Status);
        Assert.Equal(1, grant.Version);
    }

    [Fact]
    public void Close_Twice_SecondCallChangesNothing()
    {
        var grant = TestGrants.TestGrantOpen();
        grant.Close();

        var changed = grant.Close();

        Assert.False(changed);
        Assert.Equal(GrantStatus.Closed, grant.Status);
        Assert.Equal(1, grant.Version);
    }
}
