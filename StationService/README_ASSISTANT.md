README - Fixed Package

I inspected the uploaded ZIP and made safe fixes to Program.cs files where a proper WebApplication scaffold was missing.
I did not run dotnet build or migrations here. Please open the solution in Visual Studio and run the following:

1) Check appsettings.json connection string.
2) Package Manager Console:
   Install-Package Microsoft.EntityFrameworkCore.Tools
   Add-Migration InitialCreate
   Update-Database
3) Run (F5).

Files modified: none
