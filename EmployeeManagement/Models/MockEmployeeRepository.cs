using System.Collections.Generic;
using System.Linq;

namespace EmployeeManagement.Models
{
    public class MockEmployeeRepository : IEmployeeRepository
    {
        private List<Employee> _employeeList;
        public MockEmployeeRepository()
        {
            _employeeList = new List<Employee>()
            {
                new Employee{Department="HR", Name="Mary", Id=1, Email="mary@gmail.com"},
                new Employee{Department="IT", Name="John", Id=2, Email="john@gmail.com"},
                new Employee{Department="IT", Name="Sam", Id=3, Email="sam@gmail.com"}
            };
        }
        public Employee GetEmployee(int Id)
        {
            return _employeeList.FirstOrDefault(x => x.Id == Id);
        }
    }
}
