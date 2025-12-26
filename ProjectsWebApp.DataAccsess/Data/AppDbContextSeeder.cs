using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using ProjectsWebApp.SeedData;


namespace ProjectsWebApp.DataAccsess.Data
{
    public static class AppDbContextSeeder
    {
        public static async Task SeedAsync(this ApplicationDbContext db, IWebHostEnvironment env)
        {
            Console.WriteLine("SeedAsync started...");

            var jsonPath = Path.Combine(env.ContentRootPath, "SeedData", "MakerSpaceProjects.json");
            var doc = await File.ReadAllTextAsync(jsonPath);

            var root = JsonSerializer.Deserialize<Root>(doc)!;

            // (Option 1) Clear all existing MakerSpaceProjects first
            db.MakerSpaceProjects.RemoveRange(db.MakerSpaceProjects);
            await db.SaveChangesAsync();

            // Then insert fresh ones
            var projects = root.Data.Select(item => new MakerSpaceProject
            {
                Id = item.Id,
                Title = item.Title,
                Tags = item.Tags,
                Top = item.Top,
                DisplayOrder = item.Verlauf,
                ProjectUrl = item.ProjectUrl,
                Description = $"(Auto-generated description for {item.Title})"
            });

            db.MakerSpaceProjects.AddRange(projects);
            await db.SaveChangesAsync();

            Console.WriteLine("Seeding completed ✅");
        }


    }
}
