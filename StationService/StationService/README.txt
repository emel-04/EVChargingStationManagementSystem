StationService - Full single-project package for Visual Studio 2022

How to run:
1. Requirements:
   - Visual Studio 2022
   - .NET 6 SDK
   - SQL Server / LocalDB

2. Open the solution:
   - Open StationService.sln or the folder in Visual Studio, then open the project file.

3. Check connection string in appsettings.json:
   - Default uses LocalDB (Server=(localdb)\\MSSQLLocalDB). Change if needed.

4. Package Manager Console:
   - Tools -> NuGet Package Manager -> Package Manager Console
   - Default project: StationService
   - If EF tools not installed: Install-Package Microsoft.EntityFrameworkCore.Tools
   - Run:
     Add-Migration InitialCreate
     Update-Database

5. Run (F5).
   - Swagger: https://localhost:<port>/swagger
   - Frontend: https://localhost:<port>/

Notes:
- Project contains Services and Repositories folders with DI setup in Program.cs.
- For production, set proper CORS and secure connection strings.
