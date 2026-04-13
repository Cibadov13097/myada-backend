using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Infrastructure;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        const int seedUserId = 1;

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

        if (!await context.SupportStaffStatuses.AnyAsync())
        {
            var staffStatuses = new List<SupportStaffStatus>
            {
                new() { MemberId = 1001, Status = StaffAvailabilityStatus.Online },
                new() { MemberId = 1002, Status = StaffAvailabilityStatus.OnBreak },
                new() { MemberId = 2001, Status = StaffAvailabilityStatus.Online },
                new() { MemberId = 2002, Status = StaffAvailabilityStatus.Offline }
            };

            staffStatuses.ForEach(x => x.SetCreated(seedUserId));

            await context.SupportStaffStatuses.AddRangeAsync(staffStatuses);
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
                    Description = "Wi-Fi is not working in the library study area.",
                    IsBuilding = true,
                    BuildingId = 1,
                    IsRoom = false,
                    RoomId = null,
                    AreaDetails = "2nd floor library",
                    Status = RequestStatus.New,
                    IsUrgent = true,
                    ImgUrl = null
                },
                new()
                {
                    CategoryId = acCategory.Id,
                    Description = "Air conditioner in room B203 is not cooling.",
                    IsBuilding = true,
                    BuildingId = 2,
                    IsRoom = true,
                    RoomId = 203,
                    AreaDetails = "B block, room 203",
                    Status = RequestStatus.Assigned,
                    StaffId = 2001,
                    AssignedBy = 1,
                    AssignedAt = DateTime.UtcNow,
                    IsUrgent = false,
                    DispatcherInstructions = "Please inspect before noon."
                }
            };

            requests.ForEach(x => x.SetCreated(seedUserId));

            await context.Requests.AddRangeAsync(requests);
            await context.SaveChangesAsync();

            if (!await context.Attachments.AnyAsync())
            {
                var savedRequest = await context.Requests
                    .FirstAsync(x => x.Description.Contains("Wi-Fi is not working"));

                var attachments = new List<Attachment>
                {
                    new()
                    {
                        RequestId = savedRequest.Id,
                        Url = "https://example.com/uploads/sample-wifi-issue.jpg"
                    }
                };

                attachments.ForEach(x => x.SetCreated(seedUserId));

                await context.Attachments.AddRangeAsync(attachments);
                await context.SaveChangesAsync();
            }
        }
    }
}