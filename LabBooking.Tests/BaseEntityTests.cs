using LabBooking.Domain.Common;
using Xunit;

namespace LabBooking.Tests;

public class BaseEntityTests
{
    private sealed class TestEntity : BaseEntity
    {
        public TestEntity() { }
        public TestEntity(Guid id, DateTime createdAt, DateTime? updatedAt) : base(id, createdAt, updatedAt) { }
    }

    [Fact]
    public void NewEntity_Gets_UniqueId_And_CreatedAt()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        Assert.NotEqual(Guid.Empty, a.Id);
        Assert.NotEqual(a.Id, b.Id);
        Assert.True(a.CreatedAt <= DateTime.UtcNow);
        Assert.Null(a.UpdatedAt);
    }

    [Fact]
    public void RehydratedEntity_Preserves_Id_And_Timestamps()
    {
        var id = Guid.NewGuid();
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        var entity = new TestEntity(id, created, updated);

        Assert.Equal(id, entity.Id);
        Assert.Equal(created, entity.CreatedAt);
        Assert.Equal(updated, entity.UpdatedAt);
    }

    [Fact]
    public void MarkUpdated_Sets_UpdatedAt()
    {
        var entity = new TestEntity();

        Assert.Null(entity.UpdatedAt);
        entity.MarkUpdated();
        Assert.NotNull(entity.UpdatedAt);
        Assert.True(entity.UpdatedAt!.Value <= DateTime.UtcNow);
    }
}

public class PagedResultTests
{
    [Fact]
    public void PagedResult_Holds_Items_And_TotalCount()
    {
        var result = new PagedResult<int>([1, 2, 3], 100);

        Assert.Equal(new[] { 1, 2, 3 }, result.Items);
        Assert.Equal(100, result.TotalCount);
    }
}
