﻿using API.Models;
using AspAPI.Report;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace AspAPI.Controllers
{
    public class DepartmentController : Controller
    {
        readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("https://localhost:44366/api/")
        };

        // GET: Department
        public ActionResult Index()
        {
            return View(LoadDepartment());
        }

        public JsonResult LoadDepartment()
        {
            IEnumerable<Department> datadept = null;
            var responseTask = client.GetAsync("Department");
            responseTask.Wait();
            var result = responseTask.Result;
            if (result.IsSuccessStatusCode)
            {
                var readTask = result.Content.ReadAsAsync<IList<Department>>();
                readTask.Wait();
                datadept = readTask.Result;
            }
            else
            {
                datadept = Enumerable.Empty<Department>();
                ModelState.AddModelError(string.Empty, "Sorry Server Error, Try Again");
            }

            return new JsonResult { Data = datadept, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public JsonResult InsertorUpdate(Department department)
        {
            var myContent = JsonConvert.SerializeObject(department);
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            if (department.Id == 0)
            {
                var result = client.PostAsync("Department", byteContent).Result;
                return new JsonResult { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet }; //Return if Success
            }
            else
            {
                var result = client.PutAsync("Department/" + department.Id, byteContent).Result;
                return new JsonResult { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        public async Task<JsonResult> GetById(int Id)
        {
            HttpResponseMessage response = await client.GetAsync("Department");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsAsync<IList<Department>>();
                var dept = data.FirstOrDefault(t => t.Id == Id);
                var json = JsonConvert.SerializeObject(dept, Formatting.None, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
                return new JsonResult { Data = json, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            return Json("Internal Server Error"); // return if Error
        }

        public JsonResult Delete(int Id)
        {
            var Result = client.DeleteAsync("Department/" + Id).Result;
            return new JsonResult { Data = Result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        //Export
        public ActionResult ExportToPDF(Department department)
        {
            DepartmentReport deptreport = new DepartmentReport();
            byte[] abytes = deptreport.PrepareReport(GetDepartment());
            return File(abytes, "application/pdf");
        }

        public List<Department> GetDepartment()
        {
            IEnumerable<Department> datadept = null;
            var responseTask = client.GetAsync("Department");
            responseTask.Wait();
            var result = responseTask.Result;
            if (result.IsSuccessStatusCode)
            {
                var readTask = result.Content.ReadAsAsync<IList<Department>>();
                readTask.Wait();
                datadept = readTask.Result;
            }
            else
            {
                datadept = Enumerable.Empty<Department>();
                ModelState.AddModelError(string.Empty, "Sorry Server Error, Try Again");
            }

            return datadept.ToList();
        }

        public ActionResult ExportToExcel()
        {
            var comlumHeaders = new string[]
            {
                "Id",
                "Name Department",
                "Tanggal Dibuat",
                "Tanggal Diubah"
            };

            byte[] result;

            using (var package = new ExcelPackage())
            {
                // add a new worksheet to the empty workbook

                var worksheet = package.Workbook.Worksheets.Add("Department List"); //Worksheet name
                using (var cells = worksheet.Cells[1, 1, 1, 4]) //(1,1) (1,5)
                {
                    cells.Style.Font.Bold = true;
                }

                //First add the headers
                for (var i = 0; i < comlumHeaders.Count(); i++)
                {
                    worksheet.Cells[1, i + 1].Value = comlumHeaders[i];
                }

                //Add values
                var j = 2;
                foreach (var dept in GetDepartment())
                {
                    worksheet.Cells["A" + j].Value = dept.Id;
                    worksheet.Cells["B" + j].Value = dept.Name;
                    worksheet.Cells["C" + j].Value = dept.CreateDate.ToString();
                    worksheet.Cells["D" + j].Value = dept.UpdateDate.ToString();
                    j++;
                }
                result = package.GetAsByteArray();
            }

            return File(result, "application/ms-excel", $"DepartmentList.xlsx");
        }

        public ActionResult ExportToCSV()
        {
            var comlumHeaders = new string[]
            {
                "Id",
                "Name Department",
                "Tanggal Dibuat",
                "Tanggal Diubah"
            };

            var deptRecords = (from dept in GetDepartment()
                                   select new object[]
                                   {
                                            dept.Id,
                                            dept.Name,
                                            dept.CreateDate.ToString(),
                                            dept.UpdateDate.ToString()
                                   }).ToList();

            // Build the file content
            var deptcsv = new StringBuilder();
            deptRecords.ForEach(line =>
            {
                deptcsv.AppendLine(string.Join(",", line));
            });

            byte[] buffer = Encoding.ASCII.GetBytes($"{string.Join(",", comlumHeaders)}\r\n{deptcsv.ToString()}");
            return File(buffer, "text/csv", $"DepartmentList.csv");
        }
    }
}