using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Repositories.Abstract;

// Order üçün repository MÜQAVİLƏSİ (interfeys). IAsyncRepository-dən bütün CRUD-u miras alır.
public interface IOrderRepository : IAsyncRepository<Order, int>
{
}
