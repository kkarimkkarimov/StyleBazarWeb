using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// NewsletterSubscriber repository-sinin GERÇƏKLƏŞMƏSİ. EfRepositoryBase bütün CRUD məntiqini verir;
// burada yalnız konstruktor DbContext-i bazaya ötürür.
public class NewsletterSubscriberRepository : EfRepositoryBase<NewsletterSubscriber, int, ApplicationDbContext>, INewsletterSubscriberRepository
{
    public NewsletterSubscriberRepository(ApplicationDbContext context) : base(context)
    {
    }
}
