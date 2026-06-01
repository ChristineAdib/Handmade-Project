using HandoraDomain.Models.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.OrderEntity
{
    public class OrderShippingAddress
    {
        public OrderShippingAddress()
        {
            
        }
        public OrderShippingAddress(string firstName, string lastName, string street, string city, string country)
        {
            FirstName = firstName;
            LastName = lastName;
            Street = street;
            City = city;
            Country = country;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        public static implicit operator OrderShippingAddress(Address v)
        {
            throw new NotImplementedException();
        }
    }
}
