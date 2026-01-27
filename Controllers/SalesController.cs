using Microsoft.AspNetCore.Mvc;
using PMS.Web.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PMS.Web.Controllers
{
    public class SalesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SalesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveModule = "Sales";
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://hrm.btkdeals.com/queries.php");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<SalesApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                var data = apiResponse?.Data ?? new List<SalesQuery>();
                
                // Grouping for dashboard
                var typeGroups = data
                    .GroupBy(q => q.QueryType ?? "Other")
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var statusGroups = data
                    .GroupBy(q => q.Status ?? "Pending")
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // Group by date (CreatedAt)
                var dateGroups = data
                    .Where(q => !string.IsNullOrEmpty(q.CreatedAt))
                    .GroupBy(q => DateTime.TryParse(q.CreatedAt, out var dt) ? dt.ToString("yyyy-MM-dd") : "Unknown")
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Label)
                    .TakeLast(7) // Last 7 days with activity
                    .ToList();

                ViewBag.TypeLabels = JsonSerializer.Serialize(typeGroups.Select(x => x.Label));
                ViewBag.TypeCounts = JsonSerializer.Serialize(typeGroups.Select(x => x.Count));
                ViewBag.StatusLabels = JsonSerializer.Serialize(statusGroups.Select(x => x.Label));
                ViewBag.StatusCounts = JsonSerializer.Serialize(statusGroups.Select(x => x.Count));
                ViewBag.DateLabels = JsonSerializer.Serialize(dateGroups.Select(x => x.Label));
                ViewBag.DateCounts = JsonSerializer.Serialize(dateGroups.Select(x => x.Count));

                return View(data);
            }
            
            return View(new List<SalesQuery>());
        }

        public async Task<IActionResult> QueriesReport()
        {
            ViewBag.ActiveModule = "Sales";
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://hrm.btkdeals.com/queries.php");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<SalesApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return View(apiResponse?.Data ?? new List<SalesQuery>());
            }
            
            return View(new List<SalesQuery>());
        }

        [HttpGet]
        public async Task<IActionResult> AllSales()
        {
            return RedirectToAction(nameof(QueriesReport));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.ActiveModule = "Sales";
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://hrm.btkdeals.com/queries.php");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<SalesApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var query = apiResponse?.Data?.Find(q => q.Id == id);
                if (query == null) return NotFound();

                return View(query);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, SalesQuery query)
        {
            ViewBag.ActiveModule = "Sales";
            var client = _httpClientFactory.CreateClient();
            
            // Following Steps.txt params: id, query_type, title, description, email, mobile_no, address, status, responder_remarks, comments
            var values = new Dictionary<string, string>
            {
                { "id", id.ToString() },
                { "query_type", query.QueryType ?? "" },
                { "title", query.Title ?? "" },
                { "description", query.Description ?? "" },
                { "email", query.Email ?? "" },
                { "mobile_no", query.MobileNo ?? "" },
                { "address", query.Address ?? "" },
                { "status", query.Status ?? "" },
                { "responder_remarks", query.ResponderRemarks ?? "" },
                { "comments", query.Comments ?? "" }
            };

            var content = new FormUrlEncodedContent(values);
            // Steps.txt says POST /updatequery.php?id={queryId} or id in body. We do both to be sure.
            var response = await client.PostAsync($"https://hrm.btkdeals.com/updatequery.php?id={id}", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                try 
                {
                    var result = JsonSerializer.Deserialize<SalesUpdateResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result?.Status?.ToLower() == "ok")
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    
                    ModelState.AddModelError("", result?.Message ?? "Update failed according to API status.");
                }
                catch
                {
                    // Fallback if API returns 200 but not valid JSON
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error: {response.StatusCode}. {errorContent}");
            }

            return View(query);
        }
    }
}
