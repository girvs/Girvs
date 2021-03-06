﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.Domain.Extensions;
using Girvs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Power.BasicManagement.Domain.Models;
using Power.BasicManagement.Domain.Repositories;

namespace Power.BasicManagement.Infrastructure.Repositories
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public Task<Role> GetRoleByIdIncludeUsersAsync(Guid roleId)
        {
            return DbSet.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == roleId);
        }

        public override async Task<List<Role>> GetAllAsync(params string[] fields)
        {
            if (fields.Any())
            {
                //临时方法，待改进,不科学的方法
                return await Task.Run(() =>
                    DbSet.SelectProperties(fields).Where(o => !o.IsInitData).ToList());
            }
            else
            {
                return await DbSet.Where(o => !o.IsInitData).ToListAsync();
            }
        }

        public override Task<Role> GetByIdAsync(Guid id)
        {
            return DbSet.Include(r => r.UserRoles).FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}