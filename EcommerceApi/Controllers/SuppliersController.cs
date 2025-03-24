using EcommerceApi.Models;
using EcommerceApi.Models.Repositories;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierRepository supplierRepository;

        public SuppliersController(ISupplierRepository supplierRepository)
        {
            this.supplierRepository = supplierRepository;
        }

        [HttpGet]
        public IActionResult GetSuppliers(int? page)
        {
            IQueryable<Supplier> query = supplierRepository.GetSuppliers();

            query = query.OrderByDescending(o => o.Id);


            // implement the pagination functionality
            if (page == null || page < 1)
            {
                page = 1;
            }

            int pageSize = 5;
            int totalPages = 0;

            decimal count = query.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((int)(page - 1) * pageSize)
                .Take(pageSize);


            // read the suppliers
            var suppliers = query.ToList();


            var response = new
            {
                Suppliers = suppliers,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetSupplier(int id)
        {
            var user = await supplierRepository.GetSupplierById(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet("Count")]
        public ActionResult GetSuppliersCount()
        {
            try
            {
                decimal count = supplierRepository.GetSuppliers().Count();

                var response = new
                {
                    Count = count,
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error retrieving suppliers count from the database");
            }

        }

        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier(SupplierDto supplierDto)
        {
            try
            {
                // check if the email address is already supplier or not
                var s = await supplierRepository.GetSupplierByEmail(supplierDto.Email);

                if (s != null)
                {
                    ModelState.AddModelError("Email", "This Email address is already used");
                    return BadRequest(ModelState);
                }


                // create new account
                Supplier supplier = new Supplier()
                {
                    Name = supplierDto.Name,
                    Email = supplierDto.Email,
                    Phone = supplierDto.Phone ?? "",
                    Address = supplierDto.Address,
                    CreatedAt = DateTime.Now
                };

                var createdSupplier = await supplierRepository.CreateSupplier(supplier);


                return Ok(createdSupplier);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error creating supplier in the database");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Supplier>> UpdateSupplier(int id, SupplierDto supplierDto)
        {
            try
            {
                var supplier = await supplierRepository.GetSupplierById(id);

                if (supplier == null)
                {
                    return NotFound();
                }

                // check if the email address is already supplier or not
                var s = await supplierRepository.GetSupplierByEmail(supplierDto.Email);

                if (s != null && s.Id != id)
                {
                    ModelState.AddModelError("Email", "This Email address is already used");
                    return BadRequest(ModelState);
                }

                // update the supplier in the database
                supplier.Name = supplierDto.Name;
                supplier.Email = supplierDto.Email;
                supplier.Phone = supplierDto.Phone;
                supplier.Address = supplierDto.Address;

                var updatedSupplier = await supplierRepository.UpdateSupplier(supplier);

                return Ok(updatedSupplier);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error updating supplier in the database");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSupplier(int id)
        {
            try
            {
                var supplier = await supplierRepository.GetSupplierById(id);

                if (supplier == null)
                {
                    return NotFound();
                }

                // delete the supplier from the database
                await supplierRepository.DeleteSupplier(supplier);

                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                "Error deleting supplier from the database");
            }
        }
    }
}
