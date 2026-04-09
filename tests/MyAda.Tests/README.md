# MyAda.Tests

This project contains unit tests for the clean/onion architecture layers in `MyAda`.

Suggested folder structure:

- `Services/`
- `Controllers/`
- `Common/`

Current examples:

- `Services/AuthService/RefreshTokenServiceTests.cs`
- `Services/ClubManagementService/CategoryServiceTests.cs`
- `Services/ClubManagementService/ClubServiceTests.cs`
- `Services/ClubManagementService/EventServiceTests.cs`
- `Services/ClubManagementService/EventRegistrationServiceTests.cs`
- `Services/ClubManagementService/JobApplicationServiceTests.cs`
- `Services/ClubManagementService/JobVacancyServiceTests.cs`
- `Services/ClubManagementService/SubeventServiceTests.cs`
- `Services/LostFoundService/CategoryServiceTests.cs`
- `Services/LostFoundService/FoundItemServiceTests.cs`
- `Services/LostFoundService/FoundItemAdminServiceTests.cs`
- `Services/LostFoundService/LostItemServiceTests.cs`
- `Services/LostFoundService/LostItemAdminServiceTests.cs`
- `Services/LocationService/BuildingServiceTests.cs`
- `Services/LocationService/LocationServiceTests.cs`
- `Controllers/LocationService/BuildingsControllerTests.cs`

Guidelines:

- Prefer testing `Application` and `Domain` logic directly.
- Only reference API projects when you want lightweight controller tests.
- Avoid testing EF Core repositories as unit tests; cover them with integration tests instead.
