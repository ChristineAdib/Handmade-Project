using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Seeders
{

    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = [AppRoles.Admin, AppRoles.Seller, AppRoles.Buyer];

            foreach (var role in roles)
            {
                var result =
               await roleManager.CreateAsync(
                   new IdentityRole(role));

                if (result.Succeeded)
                {
                    Console.WriteLine($"{role} created");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }
            }
        }
    }
}
