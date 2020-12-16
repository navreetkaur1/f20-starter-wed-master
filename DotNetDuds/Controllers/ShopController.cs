using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetDuds.Data;
using DotNetDuds.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace DotNetDuds.Controllers
{
    public class ShopController : Controller
    {
        // db connection
        private readonly ApplicationDbContext _context;

        // app configuration depedency - used to read API keys from appsettings or secret key store
        private IConfiguration _iconfiguration;

        // constructor that accepts a db context object
        public ShopController(ApplicationDbContext context, IConfiguration configuration)
        {
            // instantiate an instance of our db connection when this class is instantiated
            _context = context;

            // instantiate an instance of our app configuration when this class is instantiated
            _iconfiguration = configuration;
        }

        public IActionResult Index()
        {
            // use the db context and Categories DbSet to fetch a list from the db
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();

            // pass the categories data to the view for display to the shopper
            return View(categories);
        }

        // GET: /Shop/Browse/3
        public IActionResult Browse(int id)
        {
            // query the db for the products in the selected category
            var products = _context.Products.Include(p => p.Category).Where(p => p.CategoryId == id).OrderBy(p => p.Name).ToList();

            // get the Category name for display in the page heading
            ViewBag.Category = products[0].Category.Name;
            //ViewBag.Category = _context.Categories.Find(id).Name.ToString();

            // load the Browse view & pass the list of products for display
            return View(products);
        }

        // POST: /Shop/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int ProductId, int Quantity)
        {
            // look up the current product price
            var price = _context.Products.Find(ProductId).Price;

            // set the customer
            var customerId = GetCustomerId();

            // check if item already exists in customer's cart
            var cartItem = _context.Carts.SingleOrDefault(c => c.ProductId == ProductId && c.CustomerId == customerId);

            if (cartItem == null)
            {
                // create /populate a new cart object as customer doesn't already have this product in their cart
                var cart = new Cart
                {
                    ProductId = ProductId,
                    Quantity = Quantity,
                    Price = price,
                    CustomerId = customerId,
                    DateCreated = DateTime.Now
                };

                // save to the Carts table in the db
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }
            else
            {
                // increment quantity of the existing cart Item
                cartItem.Quantity += Quantity;
                _context.Carts.Update(cartItem);
                _context.SaveChanges();
            }          

            // redirect to Cart page
            return RedirectToAction("Cart");
        }

        // check session for existing Session ID.  If none exists, first create it then send it back.
        private string GetCustomerId()
        {
            // check if there is already a CustomerId session variable
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                // this is the 1st item in this user's cart; generate Guid and store in session variable
                HttpContext.Session.SetString("CustomerId", Guid.NewGuid().ToString());
            }

            return HttpContext.Session.GetString("CustomerId");
        }

        // GET: /Shop/Cart
        public IActionResult Cart()
        {
            // get items in current user's cart
            var cartItems = _context.Carts.Include(c => c.Product).Where(c => c.CustomerId == HttpContext.Session.GetString("CustomerId")).ToList();

            // calc total # of items in the cart to display in the navbar
            var itemCount = (from c in cartItems
                            select c.Quantity).Sum();

            // equivalent to the code above but slower
            //foreach (var item in cartItems)
            //{
            //    itemCount += item.Quantity;
            //}

            HttpContext.Session.SetInt32("ItemCount", itemCount);

            // display a view and pass the items for display
            return View(cartItems);
        }

        // GET: /Shop/RemoveFromCart/3
        public IActionResult RemoveFromCart(int id)
        {
            // find the item with this PK value
            var cartItem = _context.Carts.Find(id);

            // delete record from Carts table
            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }

            // redirect to updated Cart
            return RedirectToAction("Cart");
        }

        // GET: /Shop/Checkout
        [Authorize]
        public IActionResult Checkout()
        {
            // load checkout form
            return View();
        }

        // POST: /Shop/Checkout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout([Bind("Address,City,Province,PostalCode")] Models.Order order)
        {
            // binding the 4 form inputs to matching Order properties in the parameter list
            // auto-fill the other 3 properties
            order.OrderDate = DateTime.Now;
            order.CustomerId = User.Identity.Name;
            order.Total = (from c in _context.Carts
                           where c.CustomerId == HttpContext.Session.GetString("CustomerId")
                           select c.Quantity * c.Price).Sum();

            // store the order in a Session variable using SessionExtensions.cs library
            HttpContext.Session.SetObject("Order", order);

            // redirect to Payment so user can pay through Stripe Payment Gateway
            return RedirectToAction("Payment");
        }

        // GET: /Shop/Payment
        [Authorize]
        public IActionResult Payment()
        {
            // get the order from the session
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            // send the total to the view for display using the ViewBag
            ViewBag.Total = order.Total;

            // read the Stripe Publishable Key from the configuration and put in ViewBag for the payment form
            ViewBag.PublishableKey = _iconfiguration.GetSection("Stripe")["PublishableKey"];

            // load the Payment view
            return View();
        }

        // POST: /Shop/ProcessPayment
        [Authorize]
        [HttpPost]
        public IActionResult ProcessPayment()
        {
            // get the order from the session variable
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            // get the Stripe Secret Key from the configuration and pass it before we can create a new checkout session
            StripeConfiguration.ApiKey = _iconfiguration.GetSection("Stripe")["SecretKey"];

            // code will go here to create and submit Stripe payment charge
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                  "card",
                },
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                      UnitAmount = (long?)(order.Total * 100),
                      Currency = "cad",
                      ProductData = new SessionLineItemPriceDataProductDataOptions
                      {
                        Name = "DotNetDuds Purchase",
                      },
                    },
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = "https://" + Request.Host + "/Shop/SaveOrder",
                CancelUrl = "https://" + Request.Host + "/Shop/Cart"
            };

            var service = new SessionService();
            Session session = service.Create(options);
            return Json(new { id = session.Id });
        }

        // GET: /Shop/SaveOrder
        [Authorize]
        public IActionResult SaveOrder()
        {
            // get the order from the session variable
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            // save as new order to the db
            _context.Orders.Add(order);
            _context.SaveChanges();

            // save the line items as new order details records
            var cartItems = _context.Carts.Where(c => c.CustomerId == HttpContext.Session.GetString("CustomerId"));
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Cost = item.Price,
                    OrderId = order.Id
                };

                _context.OrderDetails.Add(orderDetail);           
            }
            _context.SaveChanges();
            
            // delete the items from the user's cart
            foreach (var item in cartItems)
            {
                _context.Carts.Remove(item);
            }
            _context.SaveChanges();

            // set the Session ItemCount variable (which shows in the navbar) back to zero
            HttpContext.Session.SetInt32("ItemCount", 0);

            // redirect to order confirmation page i.e. /Orders/Details/1
            return RedirectToAction("Details", "Orders", new { @id = order.Id });
        }
    }
}
