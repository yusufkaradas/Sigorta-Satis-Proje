using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Kasko.DataAccess.Repositories.Concrete
{
    public class QuoteRepository : GenericRepository<Quote>, IQuoteRepository
    {
        private readonly KaskoContext _dbContext;

        public QuoteRepository(KaskoContext context)
            : base(context)
        {
            _dbContext = context;
        }


        public async Task<Quote?> GetByIdIncludingDetailsAsync(Guid id)
        {
            return await _dbContext.Quotes
                .Include(x => x.Customer)
                .Include(x => x.Vehicle)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<bool> QuoteNumberExistsAsync(
            string quoteNumber,
            Guid? excludeQuoteId = null)
        {
            return await _dbContext.Quotes
                .AnyAsync(x =>
                    x.QuoteNumber == quoteNumber &&
                    (!excludeQuoteId.HasValue ||
                     x.Id != excludeQuoteId.Value));
        }

    }
}