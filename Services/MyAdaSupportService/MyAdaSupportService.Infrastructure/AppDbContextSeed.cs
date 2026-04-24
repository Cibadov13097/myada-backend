using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Infrastructure;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        var seedUserId = Guid.Empty;
        var seededStaffId = Guid.Parse("00000000-0000-0000-0000-000000002001");
        var seededDispatcherId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Module = SupportArea.IT, Name = "Internet / Wi-Fi" },
                new() { Module = SupportArea.IT, Name = "Projector / Smart Board" },
                new() { Module = SupportArea.IT, Name = "Computer / Software Issue" },
                new() { Module = SupportArea.IT, Name = "Printer Issue" },

                new() { Module = SupportArea.FM, Name = "Electricity" },
                new() { Module = SupportArea.FM, Name = "Air Conditioner" },
                new() { Module = SupportArea.FM, Name = "Plumbing" },
                new() { Module = SupportArea.FM, Name = "Furniture Damage" }
            };

            categories.ForEach(x => x.SetCreated(seedUserId));

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        if (!await context.Requests.AnyAsync())
        {
            var wifiCategory = await context.Categories.FirstAsync(x => x.Name == "Internet / Wi-Fi");
            var acCategory = await context.Categories.FirstAsync(x => x.Name == "Air Conditioner");

            var requests = new List<Request>
            {
                new()
                {
                    CategoryId = wifiCategory.Id,
                    Description =
                        "Eduroam drops every few minutes in the ADA Library quiet reading zone (second floor, near the window bays).",
                    IsBuilding = true,
                    BuildingId = 1,
                    IsRoom = false,
                    RoomId = null,
                    AreaDetails = "ADA Library — 2nd floor, east wing",
                    Status = RequestStatus.New,
                    IsUrgent = true,
                    ImgUrl = null
                },
                new()
                {
                    CategoryId = acCategory.Id,
                    Description =
                        "HVAC in Kathleen Velasco Math & Science Building room MSB-305 is blowing warm air during afternoon labs.",
                    IsBuilding = true,
                    BuildingId = 2,
                    IsRoom = true,
                    RoomId = 305,
                    AreaDetails = "MSB-305 (third floor, corridor B)",
                    Status = RequestStatus.Assigned,
                    StaffId = seededStaffId,
                    AssignedBy = seededDispatcherId,
                    AssignedAt = DateTime.UtcNow,
                    IsUrgent = false,
                    DispatcherInstructions = "Please inspect before 12:00 Baku time; lab session at 14:00."
                }
            };

            requests.ForEach(x => x.SetCreated(seedUserId));

            await context.Requests.AddRangeAsync(requests);
            await context.SaveChangesAsync();

            if (!await context.Attachments.AnyAsync())
            {
                var savedRequest = await context.Requests
                    .FirstAsync(x => x.Description.Contains("Eduroam drops"));

                var attachments = new List<Attachment>
                {
                    new()
                    {
                        RequestId = savedRequest.Id,
                        Url = "https://myada.ada.edu.az/storage/support/demo/library-wifi-speedtest.png"
                    }
                };

                attachments.ForEach(x => x.SetCreated(seedUserId));

                await context.Attachments.AddRangeAsync(attachments);
                await context.SaveChangesAsync();
            }
        }
    }
}
