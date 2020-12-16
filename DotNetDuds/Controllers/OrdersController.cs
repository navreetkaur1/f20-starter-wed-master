using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetDuds.Data;
using DotNetDuds.Models;
using Microsoft.AspNetCore.Authorization;

namespace DotNetDuds.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                return View(await _context.Orders.OrderByDescending(o => o.Id).ToListAsync());
            }
            else
            {
                return View(await _context.Orders.Where(o => o.CustomerId == User.Identity.Name).OrderByDescending(o => o.OrderDate).ToListAsync());
            }
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.Include(o => o.OrderDetails).ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            // if current customer does not own this order
            if (!User.IsInRole("Administrator"))
            {
                if (order.CustomerId != User.Identity.Name)
                {
                    return NotFound();
                }
            } 

            if (order == null)
            {
                return NotFound();
            }

            return View("Details", order);
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
