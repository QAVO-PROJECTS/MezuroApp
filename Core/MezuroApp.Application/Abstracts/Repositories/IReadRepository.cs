using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MezuroApp.Application.Abstracts.Repositories
{
    /// <summary>
    /// Read-only generic repository interface. 
    /// IQueryable pipeline-larını üst səviyyədən ifadə edə bilmək üçün
    /// include parametrləri <see cref="Func{IQueryable, IQueryable}"/> kimi verilir.
    /// </summary>
    /// <typeparam name="T">Entity tipi</typeparam>
    public interface IReadRepository<T> where T : class, new()
    {
   
        Task<T> GetByIdAsync(string id, bool enableTracking = false);

   
        Task<IList<T>> GetAllAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool enableTracking = false);


        Task<T> GetAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool enableTracking = false);

        
        Task<int> GetCountAsync(Expression<Func<T, bool>>? predicate = null);


        Task<List<T>> GetPagedAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int page = 1,
            int pageSize = 10,
            bool enableTracking = false);
    }
}
