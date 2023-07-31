namespace Girvs.BusinessBasis.Entities;

public interface Entity
{
}

public interface Entity<TKey> : Entity
{
    TKey Id { get; }
}