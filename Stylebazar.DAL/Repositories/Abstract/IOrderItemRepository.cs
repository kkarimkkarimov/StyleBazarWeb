using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Repositories.Abstract;

// OrderItem üçün repository MÜQAVİLƏSİ (interfeys). IAsyncRepository-dən bütün CRUD-u miras alır.
public interface IOrderItemRepository : IAsyncRepository<OrderItem, int>
{
}
