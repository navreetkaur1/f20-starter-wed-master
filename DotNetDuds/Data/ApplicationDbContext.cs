using System;
using System.Collections.Generic;
using System.Text;
using DotNetDuds.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DotNetDuds.Data
{
    // this is our auto-generated database connection class 
    public class ApplicationDbContext : IdentityDbContext
    {
        // reference the data model classes - in-memory versions of our tables that support CRUD
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        // now add DbSets for Order and OrderDetail so we can run a migration to create these tables and then use these DbSets for CRUD
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // override the OnModelCreating method - fixes a bug in the Identity Framework
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
