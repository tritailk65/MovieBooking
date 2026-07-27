namespace BookingService.Infrastructure;

/// <summary>
/// Add migrations using the following command inside the 'Ordering.Infrastructure' project directory:
/// dotnet ef migrations add --startup-project ../Booking.API --context BookingContext InitialCreate
/// </summary>
public class BookingContext : DbContext, IUnitOfWork
{
    public DbSet<Booking> Bookings {get; set;}
    public DbSet<BookingItem> BookingItems {get; set;}
    public DbSet<Buyer> Buyers {get; set;}
    public DbSet<CardType> CardTypes {get; set;}
    public DbSet<PaymentMethod> PaymentMethods {get; set;}
    public DbSet<BookingStatus> BookingStatuses {get; set;}

    private readonly IMediator _mediator;
    private IDbContextTransaction _currentTransaction;

    public BookingContext(DbContextOptions<BookingContext> options) : base(options) { }

    public IDbContextTransaction GetCurrentTransaction() => _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction != null;

    public BookingContext(DbContextOptions<BookingContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        System.Diagnostics.Debug.WriteLine("BookingContext::ctor ->" + this.GetHashCode());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");
        modelBuilder.ApplyConfiguration(new BuyerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentMethodEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BookingEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BookingStatusEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CardEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ClientRequestEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BookingItemEntityTypeConfiguration());
        modelBuilder.UseIntegrationEventLogs();
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch Domain Events collection. 
        // Choices:
        // A) Right BEFORE committing data (EF SaveChanges) into the DB will make a single transaction including  
        // side effects from the domain event handlers which are using the same DbContext with "InstancePerLifetimeScope" or "scoped" lifetime
        // B) Right AFTER committing data (EF SaveChanges) into the DB will make multiple transactions. 
        // You will need to handle eventual consistency and compensatory actions in case of failures in any of the Handlers. 
        await _mediator.DispatchDomainEventsAsync(this);

        // After executing this line all the changes (from the Command Handler and Domain Event Handlers) 
        // performed through the DbContext will be committed
        _ = await base.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null) return null;

        _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction != _currentTransaction) throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current");

        try
        {
            await SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (HasActiveTransaction)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public void RollbackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            if (HasActiveTransaction)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
}
