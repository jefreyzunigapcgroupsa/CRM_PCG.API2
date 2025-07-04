﻿using CustomerService.API.Data.Context;
using CustomerService.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;

namespace CustomerService.API.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly CustomerSupportContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(CustomerSupportContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        public virtual IQueryable<T> GetAll() =>
            _dbSet.AsNoTracking();

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellation = default)
        {
            if (id <= 0) throw new ArgumentException("El id debe ser mayor que cero", nameof(id));
            return await _dbSet.FindAsync(new object[] { id }, cancellation);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return await _dbSet.AsNoTracking().AnyAsync(predicate, cancellation);
        }

        public virtual async Task AddAsync(T entity, CancellationToken cancellation = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _dbSet.AddAsync(entity, cancellation);
        }

        public void Update(T entity, CancellationToken cancellation = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Update(entity);
        }

        public void Remove(T entity, CancellationToken cancellation = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Remove(entity);
        }
    }
}