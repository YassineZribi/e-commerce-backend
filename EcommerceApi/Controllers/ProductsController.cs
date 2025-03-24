using EcommerceApi.Models;
using EcommerceApi.Models.Repositories;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository productRepository;
        private readonly IWebHostEnvironment env;

        private readonly List<string> listCategories = new List<string>()
        {
            "Phones", "Computers", "Accessories", "Printers", "Cameras", "Other"
        };

        public ProductsController(IProductRepository productRepository , IWebHostEnvironment env)
        {
            this.productRepository = productRepository;
            this.env = env;
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            return Ok(listCategories);
        }


        [HttpGet]
        public async Task<ActionResult<IQueryable<Product>>> GetProducts(string? search, string? category, int? minPrice, int? maxPrice, string? sort, string? order, int? page)
        {
            try
            {
                IQueryable<Product> query = productRepository.GetProducts();

                // search functionality
                if (search != null)
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
                }

                if (category != null)
                {
                    query = query.Where(p => p.Category == category);
                }

                if (minPrice != null)
                {
                    query = query.Where(p => p.Price >= minPrice);
                }

                if (maxPrice != null)
                {
                    query = query.Where(p => p.Price <= maxPrice);
                }

                // sort functionality
                if (sort == null) sort = "id";
                if (order == null || order != "asc") order = "desc";

                if (sort.ToLower() == "name")
                {
                    if (order == "asc")
                    {
                        query = query.OrderBy(p => p.Name);
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.Name);
                    }
                }
                else if (sort.ToLower() == "brand")
                {
                    if (order == "asc")
                    {
                        query = query.OrderBy(p => p.Brand);
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.Brand);
                    }
                }
                else if (sort.ToLower() == "category")
                {
                    if (order == "asc")
                    {
                        query = query.OrderBy(p => p.Category);
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.Category);
                    }
                }
                else if (sort.ToLower() == "price")
                {
                    if (order == "asc")
                    {
                        query = query.OrderBy(p => p.Price);
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.Price);
                    }
                }
                else if (sort.ToLower() == "date")
                {
                    if (order == "asc")
                    {
                        query = query.OrderBy(p => p.CreatedAt);
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.CreatedAt);
                    }
                }
                else
                {
                    if (order == "asc")
                    {
                        query = query.OrderBy(p => p.Id);
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.Id);
                    }
                }


                // pagination functionality
                if (page == null || page < 1) page = 1;

                int pageSize = 6;
                int totalPages = 0;

                decimal count = query.Count();
                totalPages = (int)Math.Ceiling(count / pageSize);

                query = query.Skip((int)(page - 1) * pageSize).Take(pageSize);


                var products = await query.ToListAsync();

                var response = new
                {
                    Products = products,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    Page = page
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error retrieving products from the database");
            }

        }

        [HttpGet("Recent")]
        public async Task<ActionResult<IQueryable<Product>>> GetRecentProducts()
        {
            try
            {
                IQueryable<Product> query = productRepository.GetProducts();

                query = query.OrderByDescending(p => p.CreatedAt).Take(5);


                var recentProducts = await query.ToListAsync();

                var response = new
                {
                    RecentProducts = recentProducts,
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error retrieving products from the database");
            }

        }

        [Authorize(Roles = "admin")]
        [HttpGet("CountByCategories")]
        public ActionResult GetProductsCountByCategory()
        {
            try
            {
                IQueryable<Product> query = productRepository.GetProducts();
                var response = listCategories.ToDictionary(key => key, key => productRepository.GetProducts(key).Count());

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error retrieving products count by categories from the database");
            }

        }

        [Authorize(Roles = "admin")]
        [HttpGet("Count")]
        public ActionResult GetProductsCount()
        {
            try
            {
                decimal count = productRepository.GetProducts().Count();

                var response = new
                {
                    Count = count,
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error retrieving products count from the database");
            }

        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            try
            {
                var result = await productRepository.GetProduct(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error retrieving product from the database");
            }

        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromForm] ProductDto productDto)
        {
            try
            {
                if (!listCategories.Contains(productDto.Category))
                {
                    ModelState.AddModelError("Category", "Please select a valid category");
                    return BadRequest(ModelState);
                }

                if (productDto.ImageFile == null)
                {
                    ModelState.AddModelError("ImageFile", "The Image File is required");
                    return BadRequest(ModelState);
                }

                // save the image on the server
                string imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

                string imagesFolder = env.WebRootPath + "/images/products/";

                using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
                {
                    productDto.ImageFile.CopyTo(stream);
                }


                // save product in the database
                Product product = new Product()
                {
                    Name = productDto.Name,
                    Brand = productDto.Brand,
                    Category = productDto.Category,
                    Price = productDto.Price,
                    Description = productDto.Description ?? "",
                    ImageFileName = imageFileName,
                    CreatedAt = DateTime.Now
                };

                var createdProduct = await productRepository.CreateProduct(product);

                return Ok(createdProduct);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error creating product to the database");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, [FromForm] ProductDto productDto)
        {
            try
            {
                if (!listCategories.Contains(productDto.Category))
                {
                    ModelState.AddModelError("Category", "Please select a valid category");
                    return BadRequest(ModelState);
                }

                var product = await productRepository.GetProduct(id);

                if (product == null)
                {
                    return NotFound();
                }

                string imageFileName = product.ImageFileName;
                if (productDto.ImageFile != null)
                {
                    // save the image on the server
                    imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

                    string imagesFolder = env.WebRootPath + "/images/products/";
                    using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
                    {
                        productDto.ImageFile.CopyTo(stream);
                    }


                    // delete the old image
                    System.IO.File.Delete(imagesFolder + product.ImageFileName);
                }


                // update the product in the database
                product.Name = productDto.Name;
                product.Brand = productDto.Brand;
                product.Category = productDto.Category;
                product.Price = productDto.Price;
                product.Description = productDto.Description ?? "";
                product.ImageFileName = imageFileName;

                var updatedProduct = await productRepository.UpdateProduct(product);

                return Ok(updatedProduct);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error updating product to the database");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<Product>> DeleteProduct(int id)
        {
            var product = await productRepository.GetProduct(id);

            if (product == null)
            {
                return NotFound();
            }

            // delete the image on the server
            string imagesFolder = env.WebRootPath + "/images/products/";
            System.IO.File.Delete(imagesFolder + product.ImageFileName);


            // delete the product from the database
            await productRepository.DeleteProduct(product);

            return Ok();
        }
    }
}
