using ProjetDotNet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjetDotNet.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IFileService _fileService;

        public ProductController(IProductRepository productRepo, ICategoryRepository categoryRepo, IFileService fileService)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepo.GetProducts();
            return View(products);
        }

        public async Task<IActionResult> AddProduct()
        {
            var categorySelectList = (await _categoryRepo.GetCategories()).Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.Id.ToString(),
            });

            ProductDTO productToAdd = new() { CategoryList = categorySelectList };
            return View(productToAdd);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(ProductDTO productToAdd)
        {
            var categorySelectList = (await _categoryRepo.GetCategories()).Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.Id.ToString(),
            });
            productToAdd.CategoryList = categorySelectList;

            if (!ModelState.IsValid)
                return View(productToAdd);

            string? uploadedImage = null;
            productToAdd.Image = null;
            try
            {
                if (productToAdd.ImageFile != null)
                {
                    if (productToAdd.ImageFile.Length > 1 * 1024 * 1024)
                        throw new InvalidOperationException("Image file cannot exceed 1 MB");

                    string[] allowedExtensions = new[] { ".jpeg", ".jpg", ".png" };
                    string imageName = await _fileService.SaveFile(productToAdd.ImageFile, allowedExtensions);
                    uploadedImage = imageName;
                    productToAdd.Image = imageName;
                }

                Product product = new()
                {
                    Id = 0,
                    ProductName = productToAdd.ProductName,
                    BrandName = productToAdd.BrandName,
                    Price = productToAdd.Price,
                    CategoryId = productToAdd.CategoryId,
                    Image = productToAdd.Image
                };

                await _productRepo.AddProduct(product);
                uploadedImage = null;
                TempData["successMessage"] = "Product added successfully";
                return RedirectToAction(nameof(AddProduct));
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<ProductController>>().LogError(ex, "Operation failed");
                TempData["errorMessage"] = "The operation could not be completed. Check the data and try again.";
                if (uploadedImage is not null) CleanupUpload(uploadedImage);
                return View(productToAdd);
            }
        }

        public async Task<IActionResult> UpdateProduct(int id)
        {
            var product = await _productRepo.GetProductById(id);
            if (product == null)
            {
                TempData["errorMessage"] = $"Product with id: {id} not found";
                return RedirectToAction(nameof(Index));
            }

            var categorySelectList = (await _categoryRepo.GetCategories()).Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.Id.ToString(),
                Selected = c.Id == product.CategoryId
            });

            ProductDTO productToUpdate = new()
            {
                Id = product.Id,
                ProductName = product.ProductName,
                BrandName = product.BrandName,
                Price = product.Price,
                CategoryId = product.CategoryId,
                Image = product.Image,
                CategoryList = categorySelectList
            };

            return View(productToUpdate);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(ProductDTO productToUpdate)
        {
            var categorySelectList = (await _categoryRepo.GetCategories()).Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.Id.ToString(),
                Selected = c.Id == productToUpdate.CategoryId
            });
            productToUpdate.CategoryList = categorySelectList;

            if (!ModelState.IsValid)
                return View(productToUpdate);

            string? uploadedImage = null;
            try
            {
                var current = await _productRepo.GetProductById(productToUpdate.Id);
                if (current is null) return NotFound();
                productToUpdate.Image = current.Image;
                string? oldImage = null;
                if (productToUpdate.ImageFile != null)
                {
                    if (productToUpdate.ImageFile.Length > 1 * 1024 * 1024)
                        throw new InvalidOperationException("Image file cannot exceed 1 MB");

                    string[] allowedExtensions = new[] { ".jpeg", ".jpg", ".png" };
                    string imageName = await _fileService.SaveFile(productToUpdate.ImageFile, allowedExtensions);

                    oldImage = productToUpdate.Image;
                    uploadedImage = imageName;
                    productToUpdate.Image = imageName;
                }

                Product product = new()
                {
                    Id = productToUpdate.Id,
                    ProductName = productToUpdate.ProductName,
                    BrandName = productToUpdate.BrandName,
                    Price = productToUpdate.Price,
                    CategoryId = productToUpdate.CategoryId,
                    Image = productToUpdate.Image
                };

                await _productRepo.UpdateProduct(product);
                uploadedImage = null;

                if (!string.IsNullOrWhiteSpace(oldImage))
                    _fileService.DeleteFile(oldImage);

                TempData["successMessage"] = "Product updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = ex.Message;
                if (uploadedImage is not null) CleanupUpload(uploadedImage);
                return View(productToUpdate);
            }
        }

        private void CleanupUpload(string name)
        {
            try { _fileService.DeleteFile(name); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<ProductController>>().LogError(ex, "Uploaded image cleanup failed");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _productRepo.GetProductById(id);
                if (product == null)
                {
                    TempData["errorMessage"] = $"Product with id: {id} not found";
                }
                else
                {
                    await _productRepo.DeleteProduct(product);
                    if (!string.IsNullOrWhiteSpace(product.Image))
                        _fileService.DeleteFile(product.Image);
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
