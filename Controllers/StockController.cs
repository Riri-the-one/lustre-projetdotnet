using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjetDotNet.Repositories;

namespace ProjetDotNet.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class StockController : Controller
    {
        private readonly IStockRepository _stockRepo;

        public StockController(IStockRepository stockRepo)
        {
            _stockRepo = stockRepo;
        }

        // List all stocks, optional search by product name
        public async Task<IActionResult> Index(string sTerm = "")
        {
            var stocks = await _stockRepo.GetStocks(sTerm);
            return View(stocks);
        }

        // Show stock management page for a specific product
        public async Task<IActionResult> ManageStock(int productId)
        {
            var existingStock = await _stockRepo.GetStockByProductId(productId);
            var stock = new StockDTO
            {
                OriginalQuantity = existingStock?.Quantity ?? 0,
                ProductId = productId,
                Quantity = existingStock != null ? existingStock.Quantity : 0
            };
            return View(stock);
        }

        [HttpPost]
        public async Task<IActionResult> ManageStock(StockDTO stock)
        {
            if (!ModelState.IsValid)
                return View(stock);

            try
            {
                await _stockRepo.ManageStock(stock);
                TempData["successMessage"] = "Stock updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<StockController>>().LogError(ex, "Stock update failed");
                TempData["errorMessage"] = "Stock could not be updated. Refresh and try again.";
                return View(stock);
            }
        }
    }
}
