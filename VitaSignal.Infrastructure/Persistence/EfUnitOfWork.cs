using VitaSignal.Application.Common;

namespace VitaSignal.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly VitaSignalDbContext _dbContext;

    public EfUnitOfWork(VitaSignalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
