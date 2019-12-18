using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers
{
     
    public class HomeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;

        public HomeController(IEmployeeRepository employeeRepository)
        {
            this._employeeRepository = employeeRepository;
        }
        public string Index()
        {
              return _employeeRepository.GetEmployee(1).Name;// this.Json(new { Id=1, name="Jeelani"});
        }


        //this method doesn't follow content negotiation
        public ViewResult Details()
        {
            Employee model = _employeeRepository.GetEmployee(1);
             
            ViewData["PageTitle"] = "Employees";
            return View(model);
            //return Json(model);
        }

        public ObjectResult Details1()
        {
            Employee model = _employeeRepository.GetEmployee(1);
            
            return new ObjectResult(model);
        }
    }
}