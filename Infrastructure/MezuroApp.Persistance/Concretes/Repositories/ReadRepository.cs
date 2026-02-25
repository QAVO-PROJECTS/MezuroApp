using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories
{
    public class ReadRepository<T> : IReadRepository<T> where T : class, new()
    {
        private readonly MezuroAppDbContext _db;

        public ReadRepository(MezuroAppDbContext dbContext)
        {
            _db = dbContext;
        }

        protected DbSet<T> Table => _db.Set<T>();

        public IQueryable<T> Query() => Table.AsQueryable();
        public async Task<IList<T>> GetAllAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool enableTracking = false)
        {
            IQueryable<T> query = Table;

            if (!enableTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
                return await orderBy(query).ToListAsync();

            return await query.ToListAsync();
        }

        public async Task<T> GetByIdAsync(string id, bool enableTracking = false)
        {
            if (!Guid.TryParse(id, out var parsedId))
                throw new KeyNotFoundException($"Yanlış ID formatı: {id}");

            IQueryable<T> query = Table;

            if (!enableTracking)
                query = query.AsNoTracking();

            // Id property-si Guid olmalıdır
            var entity = await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == parsedId);

            if (entity == null)
                throw new KeyNotFoundException($"Entity with ID {id} not found.");

            return entity;
        }

        public async Task<T> GetAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool enableTracking = false)
        {
            IQueryable<T> query = Table;

            if (!enableTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            query = query.Where(predicate);

            if (orderBy != null)
                query = orderBy(query);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetPagedAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int page = 1,
            int pageSize = 10,
            bool enableTracking = false)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            IQueryable<T> query = Table;

            if (!enableTracking)
                query = query.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            if (include != null)
                query = include(query);

            if (orderBy != null)
                query = orderBy(query);

            query = query.Skip((page - 1) * pageSize).Take(pageSize);

            return await query.ToListAsync();
        }

        public Task<int> GetCountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = Table.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            return query.CountAsync();
        }

        public IQueryable<T> GetQueryable()
        {
            return Table.AsQueryable();
        }
    }
}