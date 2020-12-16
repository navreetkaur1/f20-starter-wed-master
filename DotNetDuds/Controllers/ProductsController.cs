using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetDuds.Data;
using DotNetDuds.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace DotNetDuds.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            // add OrderBy to sort the product query result by A-Z by Name
            var applicationDbContext = _context.Products.Include(p => p.Category).OrderBy(p => p.Name);
            return View("Index", await applicationDbContext.ToListAsync());
        }

        [AllowAnonymous]
        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                //return NotFound();
                return View("404");  // or return View("Error");
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                //return NotFound();
                return View("404"); // or return View("Error");
            }

            return View("Details", product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories.OrderBy(c => c.Name), "Id", "Name");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,Description,CategoryId")] Product product, IFormFile Image)
        {
            if (ModelState.IsValid)
            {
                // process file upload if any before saving so we can also save a unique image name with the product
                if (Image != null)
                {
                    // get temp location of the uploaded image
                    var tempPath = Path.GetTempFileName();

                    // create a unique file name using a Globally Unique Id mypic.jpg => abc123-mypic.jpg
                    var uniqueName = Guid.NewGuid() + "-" + Image.FileName;

                    // set destination file path & name dynamically so it works on any server
                    var uploadPath = System.IO.Directory.GetCurrentDirectory() + "\\wwwroot\\img\\product-uploads\\" + uniqueName;

                    // use a stream to copy the file to the destination folder w/a unique name
                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    await Image.CopyToAsync(stream);

                    // set the product Image property equal the new unique image file name
                    product.Image = uniqueName;
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,CategoryId")] Product product, IFormFile Image, string CurrentImage)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // process file upload if any before saving so we can also save a unique image name with the product
                    if (Image != null)
                    {
                        // get temp location of the uploaded image
                        var tempPath = Path.GetTempFileName();

                        // create a unique file name using a Globally Unique Id mypic.jpg => abc123-mypic.jpg
                        var uniqueName = Guid.NewGuid() + "-" + Image.FileName;

                        // set destination file path & name dynamically so it works on any server
                        var uploadPath = System.IO.Directory.GetCurrentDirectory() + "\\wwwroot\\img\\product-uploads\\" + uniqueName;

                        // use a stream to copy the file to the destination folder w/a unique name
                        using (var stream = new FileStream(uploadPath, FileMode.Create))
                            await Image.CopyToAsync(stream);

                        // set the product Image property equal the new unique image file name
                        product.Image = uniqueName;
                    }
                    else
                    {
                        product.Image = CurrentImage;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
