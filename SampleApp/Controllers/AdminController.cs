using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rotativa.AspNetCore;
using SampleApp.Models;
using SampleApp.Repository;
using SampleApp.Services;
using System.Data;
using System.Text;
using System.Text.Json;

namespace SampleApp.Controllers
{
    //[Authorize(Roles ="Admin")]
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IUserReg _userreg;
        //private readonly int _pageSize;
        private readonly IConfiguration _configuration;
        public AdminController(IUserReg userreg, IConfiguration configuration)
        {
            _userreg = userreg;
            _configuration = configuration;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(string searchName, string searchEmail, int page = 1)
        {
            var pageSize = Convert.ToInt32(_configuration["appSettings:Pagesize"]);
          
            var totalUsers = await _userreg.TotalUsers(searchName, searchEmail);
            List<Registration> users = (List<Registration>)await _userreg.ListQuery(searchName, searchEmail, page, pageSize);
            //var totalUsers = await _userreg.TotalUsers();
            // var users = await _userreg.GetPaginatedUsers(page, pageSize);
            //var users = await _userreg.GetAll(); // Fetch all users
            var viewModel = new PaginatedList<Registration>(users.ToList(), totalUsers, page, pageSize);
            return View(viewModel);
        }


        [HttpGet("ViewUser/{id}")]
        public async Task<IActionResult> ViewUser(string id)
        {

            Console.WriteLine("Encrypted value " + id);
            string decryptedId = RSAEncryption.Decrypt(id);

            Console.WriteLine("Decrypted value " + decryptedId);
            int userId = int.Parse(decryptedId);
            Console.WriteLine("GET View called");
            var user = await _userreg.GetById(userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

            [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            Console.WriteLine("GET Edit called");
            Console.WriteLine("Encrypted value " + id);
            string decryptedId = RSAEncryption.Decrypt(id);

            Console.WriteLine("Decrypted value " + decryptedId);
            int userId = int.Parse(decryptedId);
            var user = await _userreg.GetById(userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Registration reg)
        {
            Console.WriteLine(" Post me called");
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"Key: {key} Error: {error.ErrorMessage}");
                    }
                }
                return View(reg);
            }
            if (id != reg.Id)
            {
                return BadRequest();
            }

            await _userreg.Update(reg);
            return RedirectToAction("list", "Admin");
        }

      
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            Console.WriteLine("Encrypted value " + id);
            string decryptedId = RSAEncryption.Decrypt(id);

            Console.WriteLine("Decrypted value " + decryptedId);
            int userId = int.Parse(decryptedId);
            var user = await _userreg.GetById(userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: /Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userreg.Delete(id);
            return RedirectToAction("list", "Admin");
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var users = await _userreg.GetAll();
            if (users == null)
            {
                return NotFound("No events found.");
            }
            // ✅ Create an Excel Workbook
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Users List");

                // ✅ Add Header Row
                worksheet.Cell(1, 1).Value = "Name";
                worksheet.Cell(1, 2).Value = "Email";
                worksheet.Cell(1, 3).Value = "Gender";
                worksheet.Cell(1, 4).Value = "Status";

                // ✅ Apply Header Styling
                var headerRange = worksheet.Range("A1:D1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // ✅ Insert Data
                int row = 2;
                foreach (var ev in users)
                {
                    worksheet.Cell(row, 1).Value = ev.Name;
                    worksheet.Cell(row, 2).Value = ev.Email;
                    worksheet.Cell(row, 3).Value = ev.Gender;
                    worksheet.Cell(row, 4).Value = ev.Status;
                    row++;
                }

                // ✅ Auto-Fit Columns
                worksheet.Columns().AdjustToContents();

                // ✅ Save to MemoryStream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    // ✅ Copy stream data to a new stream to prevent disposal issues
                    var outputStream = new MemoryStream(stream.ToArray());
                    return File(outputStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UsersList.xlsx");
                }
            }   
        }

        public async Task<IActionResult> GeneratePdf()
        {
            // Get the event details
            var users = await _userreg.GetAll();
           
            return new ViewAsPdf("PdfReport", users)
            {
                FileName = "UserReport.pdf"
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetUserChartData()
        {
            var jsonData = await _userreg.GetFilteredUserStatusAsync();
            return Content(jsonData, "application/json");
        }

        [HttpGet]
        public IActionResult StatusChart()
        {
            return View();
        }

    }
}
