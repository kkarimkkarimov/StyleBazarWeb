namespace Core.Application.Repositories;

public interface IEntity
{
}

public interface IEntity<TId> : IEntity
{
    TId Id { get; set; }
}
