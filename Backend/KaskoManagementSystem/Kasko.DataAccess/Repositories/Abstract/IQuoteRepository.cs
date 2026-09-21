using Kasko.Entities.Concrete;
namespace Kasko.DataAccess.Repositories.Abstract
{
    public interface IQuoteRepository : IGenericRepository<Quote>
    {
        Task<Quote?> GetByIdIncludingDetailsAsync(Guid id);

        Task<bool> QuoteNumberExistsAsync(String quoteNumber, Guid? excludeQuoteId = null);
    }
}
