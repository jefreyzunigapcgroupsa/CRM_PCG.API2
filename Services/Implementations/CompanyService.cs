﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomerService.API.Dtos.RequestDtos;
using CustomerService.API.Dtos.ResponseDtos;
using CustomerService.API.Models;
using CustomerService.API.Repositories.Interfaces;
using CustomerService.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.API.Services.Implementations
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _uow;

        public CompanyService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<IEnumerable<CompanyResponseDto>> GetAllAsync(CancellationToken cancellation = default)
        {
            var companies = await _uow.Companies.GetAll()
                                .ToListAsync(cancellation);

            var dtos = new List<CompanyResponseDto>(companies.Count);
            foreach (var c in companies)
            {
                dtos.Add(new CompanyResponseDto
                {
                    CompanyId = c.CompanyId,
                    Name = c.Name,
                    Address = c.Address,
                    CreatedAt = c.CreatedAt
                });
            }
            return dtos;
        }

        public async Task<CompanyResponseDto?> GetByIdAsync(int id, CancellationToken cancellation = default)
        {
            if (id <= 0) throw new ArgumentException("Invalid company ID.", nameof(id));

            var c = await _uow.Companies.GetByIdAsync(id, cancellation);
            if (c == null) return null;

            return new CompanyResponseDto
            {
                CompanyId = c.CompanyId,
                Name = c.Name,
                Address = c.Address,
                CreatedAt = c.CreatedAt
            };
        }

        public async Task<CompanyResponseDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name is required.", nameof(request.Name));

            if (await _uow.Companies.ExistsAsync(x => x.Name == request.Name, cancellation))
                throw new InvalidOperationException("A company with the same name already exists.");

            var entity = new Company
            {
                Name = request.Name.Trim(),
                Address = request.Address?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Companies.AddAsync(entity, cancellation);
            await _uow.SaveChangesAsync(cancellation);

            return new CompanyResponseDto
            {
                CompanyId = entity.CompanyId,
                Name = entity.Name,
                Address = entity.Address,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task UpdateAsync(UpdateCompanyRequest request, CancellationToken cancellation = default)
        {
            if (request.CompanyId <= 0)
                throw new ArgumentException("Invalid company ID.", nameof(request.CompanyId));

            var entity = await _uow.Companies.GetByIdAsync(request.CompanyId, cancellation)
                         ?? throw new KeyNotFoundException("Company not found.");

            if (!string.Equals(entity.Name, request.Name, StringComparison.OrdinalIgnoreCase)
                && await _uow.Companies.ExistsAsync(x => x.Name == request.Name, cancellation))
            {
                throw new InvalidOperationException("Another company with the same name already exists.");
            }

            entity.Name = request.Name.Trim();
            entity.Address = request.Address?.Trim();
            _uow.Companies.Update(entity);
            await _uow.SaveChangesAsync(cancellation);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellation = default)
        {
            if (id <= 0) throw new ArgumentException("Invalid company ID.", nameof(id));

            var entity = await _uow.Companies.GetByIdAsync(id, cancellation)
                         ?? throw new KeyNotFoundException("Company not found.");

            _uow.Companies.Remove(entity);
            await _uow.SaveChangesAsync(cancellation);
        }
    }
}