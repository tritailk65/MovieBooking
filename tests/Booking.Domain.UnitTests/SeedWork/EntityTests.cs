using BookingService.Domain.SeedWork;
using MediatR;

namespace Booking.Domain.UnitTests.SeedWork;

public class EntityTests
{
    [Fact]
    public void IsTransient_WhenIdIsDefault_ShouldReturnTrue()
    {
        var entity = new TestEntity();

        Assert.True(entity.IsTransient());
    }

    [Fact]
    public void IsTransient_WhenIdHasValue_ShouldReturnFalse()
    {
        var entity = new TestEntity(10);

        Assert.False(entity.IsTransient());
    }

    [Fact]
    public void Equals_WhenBothEntitiesAreTransient_ShouldReturnFalse()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_WhenEntitiesHaveSameIdAndType_ShouldReturnTrue()
    {
        var first = new TestEntity(10);
        var second = new TestEntity(10);

        Assert.True(first.Equals(second));
        Assert.True(first == second);
        Assert.False(first != second);
    }

    [Fact]
    public void Equals_WhenEntitiesHaveDifferentIds_ShouldReturnFalse()
    {
        var first = new TestEntity(10);
        var second = new TestEntity(20);

        Assert.False(first.Equals(second));
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void AddDomainEvent_WhenEventIsAdded_ShouldExposeDomainEvent()
    {
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();

        entity.AddDomainEvent(domainEvent);

        Assert.Contains(domainEvent, entity.DomainEvents);
    }

    [Fact]
    public void RemoveDomainEvent_WhenEventExists_ShouldRemoveDomainEvent()
    {
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();

        entity.AddDomainEvent(domainEvent);
        entity.RemoveDomainEvent(domainEvent);

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_WhenEventsExist_ShouldRemoveAllDomainEvents()
    {
        var entity = new TestEntity();

        entity.AddDomainEvent(new TestDomainEvent());
        entity.AddDomainEvent(new TestDomainEvent());

        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }

    private sealed class TestEntity : Entity
    {
        public TestEntity()
        {
        }

        public TestEntity(int id)
        {
            Id = id;
        }
    }

    private sealed class TestDomainEvent : INotification
    {
    }
}