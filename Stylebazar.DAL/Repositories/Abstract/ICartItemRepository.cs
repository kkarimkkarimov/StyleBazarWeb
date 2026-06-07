using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Repositories.Abstract;

// CartItem üçün repository MÜQAVİLƏSİ (interfeys). IAsyncRepository-dən bütün CRUD-u miras alır.
public interface ICartItemRepository : IAsyncRepository<CartItem, int>
{
}
