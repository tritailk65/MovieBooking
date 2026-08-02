
namespace Shared.Infrastructure.OrderSaga;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingClassMap : SagaClassMap<BookingSaga>
{
    protected override void Configure(EntityTypeBuilder<BookingSaga> entity, ModelBuilder model)
    {

    }
}