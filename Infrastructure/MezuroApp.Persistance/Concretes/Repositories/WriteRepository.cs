using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories
{
    public class WriteRepository<T> : IWriteRepository<T> where T : class, new()
    {
        private readonly MezuroAppDbContext _MezuroAppDbContext;

        public WriteRepository(MezuroAppDbContext MezuroAppDbContext)
        {
            _MezuroAppDbContext = MezuroAppDbContext;
        }

        private DbSet<T> Table { get => _MezuroAppDbContext.Set<T>(); }
        public async Task AddAsync(T entity)
        {
            await Table.AddAsync(entity);
        }

        public async Task HardDeleteAsync(T entity)
        {
            await Task.Run(() => Table.Remove(entity));

        }
        public DbContext GetDbContext()
        {
            return _MezuroAppDbContext;
        }
        public async Task SoftDeleteAsync(T entity)
        {
            // Stub nesneyi attach et
            Table.Attach(entity);

            // Sadece IsDeleted alanını güncelle
            var entry = Table.Entry(entity);
            entry.Property("IsDeleted").CurrentValue = true;
            entry.Property("IsDeleted").IsModified = true;

            await Task.CompletedTask;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            await Task.Run(() => Table.Update(entity));
            return entity;
        }

        public async Task<int> CommitAsync()
        {
            return await _MezuroAppDbContext.SaveChangesAsync();
        }
    }
}
