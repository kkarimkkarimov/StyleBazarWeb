using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Repositories.Abstract;

// Store üçün repository MÜQAVİLƏSİ (interfeys). IAsyncRepository-dən bütün
// CRUD əməliyyatlarını (oxu / əlavə / dəyiş / sil) miras alır; əlavə xüsusi metod yoxdur.
public interface IStoreRepository : IAsyncRepository<Store, int>
{
}
